using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Util;
using Xunit.Abstractions;

namespace Temporal.TestUtil
{
    internal sealed class TemporalLiteExeTestServerController : ITemporalTestServerController, IDisposable
    {
        public enum ExeBinarySource
        {
            Unspecified = 0,
            PrecompiledFromToolsRepo = 1,
            ReleaseBinTemporalLiteRepo = 2
        }

        private static class Config
        {
            public const ExeBinarySource ExeBinarySource
                                = TemporalLiteExeTestServerController.ExeBinarySource.ReleaseBinTemporalLiteRepo;
        }

        private readonly ITestOutputHelper _cout;
        private readonly bool _redirectServerOutToCout;

        private ProcessManager _temporalLiteProc = null;

        public TemporalLiteExeTestServerController(ITestOutputHelper cout, bool redirectServerOutToCout)
        {
            Validate.NotNull(cout);
            _cout = cout;
            _redirectServerOutToCout = redirectServerOutToCout;
        }

        public async Task StartAsync()
        {
#pragma warning disable CS0162 // Unreachable code detected: Using const bools for settings
            if (Config.ExeBinarySource == ExeBinarySource.PrecompiledFromToolsRepo)
            {
                EnsureRunningWindows();
            }
#pragma warning restore CS0162 // Unreachable code detected

            const int TemporalServicePort = 7233;
            if (IsPortInUse(TemporalServicePort))
            {
                CoutWriteLine();
                CoutWriteLine($"WARNING!   Something is already listening on local port {TemporalServicePort}."
                            + $" We will not be able to start TemporalLite."
                            + Environment.NewLine
                            + CoutPrefix("WARNING!   However, this is most likely some kind of Temporal server,"
                                       + " so we will not abort based on this.")
                            + Environment.NewLine
                            + CoutPrefix("WARNING!   Take notice of this, as it may affect any test in an unpredictable manner.")
                            + Environment.NewLine
                            + CoutPrefix("WARNING!   You may not be running with a test-dedicated TemporalLite instance!"));
                CoutWriteLine();

                return;
            }

            string temporalLiteExePath = GetTemporalLiteExePath();
            if (!File.Exists(temporalLiteExePath))
            {
                await InstallTemporalLiteAsync(temporalLiteExePath);
            }

            Start(temporalLiteExePath);
        }

#pragma warning disable CS0162 // Unreachable code detected: Using const bools for settings
        private Task InstallTemporalLiteAsync(string temporalLiteExePath)
        {
            switch (Config.ExeBinarySource)
            {
                case ExeBinarySource.PrecompiledFromToolsRepo:
                    InstallTemporalLite_PrecompiledFromToolsRepo(temporalLiteExePath);
                    return Task.CompletedTask;

                case ExeBinarySource.ReleaseBinTemporalLiteRepo:
                    return InstallTemporalLite_ReleaseBinTemporalLiteRepo(temporalLiteExePath);


                case ExeBinarySource.Unspecified:
                default:
                    throw new Exception($"Unexpected value of {nameof(Config)}.{nameof(Config.ExeBinarySource)}: {Config.ExeBinarySource}.");
            }
        }
#pragma warning restore CS0162 // Unreachable code detected

        private async Task InstallTemporalLite_ReleaseBinTemporalLiteRepo(string temporalLiteExePath)
        {
            const string DownloadedArchiveFileName = "Temporalite.Exe.Distro.zip";

            CoutWriteLine();
            CoutWriteLine($"TemporalLite executable not found at \"{temporalLiteExePath}\".");
            CoutWriteLine($"Trying to install ({nameof(Config.ExeBinarySource)}=`{Config.ExeBinarySource}`)...");
            CoutWriteLine();

            string temporalLiteDirPath = Path.GetDirectoryName(temporalLiteExePath);
            if (Directory.Exists(temporalLiteDirPath))
            {
                CoutWriteLine($"Destination dir exists ({temporalLiteDirPath}).");
            }
            else
            {
                CoutWriteLine($"Destination dir does not exist ({temporalLiteDirPath}). Creating...");
                DirectoryInfo destDir = Directory.CreateDirectory(temporalLiteDirPath);

                if (destDir.Exists)
                {
                    CoutWriteLine($"Destination dir successfully created (${temporalLiteDirPath}).");
                }
                else
                {
                    CoutWriteLine($"Cound not create destination dir (${temporalLiteDirPath}).");
                    throw new Exception($"Cound not create destination dir for TemporalLite (${temporalLiteDirPath}).");
                }
            }

            string releaseBinArchiveUrl = GetReleaseBinArchiveUrl();
            string downloadedArchiveFilePath = Path.Combine(temporalLiteDirPath, DownloadedArchiveFileName);
            CoutWriteLine($"RuntimeEnvironmentInfo: {RuntimeEnvironmentInfo.SingletonInstance}");
            CoutWriteLine($"Downloading TemporalLite from \"{releaseBinArchiveUrl}\"...");

            long dowloadedFileSize = 0;
            using (FileStream downloadOutStream = new(downloadedArchiveFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                using (HttpClient client = new())
                {
                    using (HttpResponseMessage response = await client.GetAsync(releaseBinArchiveUrl))
                    using (Stream downloadInStream = await response.Content.ReadAsStreamAsync())
                    {
                        await downloadInStream.CopyToAsync(downloadOutStream);
                        dowloadedFileSize = downloadOutStream.Length;
                    }
                }
            }

            if (dowloadedFileSize < 1024)
            {
                CoutWriteLine($"Finished downloading TemporalLite distribution archive, but only `{dowloadedFileSize}` bytes were received.");
                CoutWriteLine($"Most likely the remote file at the specified URL does not exist, or there was a network issue.");
                CoutWriteLine($"Download URL: \"{releaseBinArchiveUrl}\".");
                CoutWriteLine($"Downloaded data: \"{downloadedArchiveFilePath}\".");
                CoutWriteLine($"Giving up.");
                throw new Exception($"Cannot get TemporalLite distribution:"
                                  + $" Only `{dowloadedFileSize}` bytes downloaded from"
                                  + $" \"{releaseBinArchiveUrl}\" to \"{downloadedArchiveFilePath}\".");
            }

            CoutWriteLine($"Finished downloading TemporalLite to \"{downloadedArchiveFilePath}\". Unpacking...");

            using (FileStream unpackInFileStream = new(downloadedArchiveFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (ZipArchive archive = new(unpackInFileStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string entryTargetFilePath = Path.Combine(temporalLiteDirPath, entry.FullName);
                    CoutWriteLine($"  - \"{entryTargetFilePath}\".");
                    entry.ExtractToFile(temporalLiteExePath, overwrite: true);
                }
            }

            CoutWriteLine($"Unpacking completed.");

            if (File.Exists(temporalLiteExePath))
            {
                CoutWriteLine($"The expected TemporalLite executable is among the"
                            + $" unpacked files ({temporalLiteExePath}).");
            }
            else
            {
                CoutWriteLine($"The expected TemporalLite executable is NOT among the unpacked"
                            + $" files ({temporalLiteExePath}). Giving up.");
                throw new Exception($"TemporalLite distributable downloaded and unpacked,"
                                  + $" but the expected TemporalLite executable is NOT among"
                                  + $" the unpacked files ({temporalLiteExePath}).");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string temporalLiteExeName = Path.GetFileName(temporalLiteExePath);
                CoutWriteLine($"Setting eXecutable mode for the TemporalLite binary (\"{temporalLiteExeName}\")...");

                string escapedTemporalLiteExePath = temporalLiteExePath.Replace("\"", "\\\"");
                ProcessManager chmod = ProcessManager.Start(exePath: "/bin/bash",
                                                            args: $"-c \"chmod -v +x {escapedTemporalLiteExePath}\"",
                                                            waitForInitOptions: null,
                                                            redirectToCout: true,
                                                            coutProcNameMoniker: "bash",
                                                            _cout);
                chmod.WaitForExit(timeout: 2000);

                CoutWriteLine($"TemporalLite binary set to be eXecutable.");
            }

            CoutWriteLine();
            CoutWriteLine($"TemporalLite has been installed.");
            CoutWriteLine();
        }

        private string GetReleaseBinArchiveUrl()
        {
            const string ReleaseBinBaseUrl = @"https://github.com/macrogreg/temporalite/releases/download/v0.0.3/";
            const string ReleaseBinArchiveName_Win_x86x64 = "temporalite_0.0.3_Windows_x86_64.zip";

#if NETFRAMEWORK
            return ReleaseBinBaseUrl + ReleaseBinArchiveName_Win_x86x64;
#else
            const string ReleaseBinArchiveName_Linux_x86x64 = "temporalite_0.0.3_Linux_x86_64.zip";
            const string ReleaseBinArchiveName_Linux_Arm64 = "temporalite_0.0.3_Linux_arm64.zip";
            const string ReleaseBinArchiveName_MacOS_x86x64 = "temporalite_0.0.3_Darwin_x86_64.zip";
            const string ReleaseBinArchiveName_MacOS_Arm64 = "temporalite_0.0.3_Darwin_arm64.zip";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    && (RuntimeInformation.OSArchitecture == Architecture.X86
                        || RuntimeInformation.OSArchitecture == Architecture.X64))
            {
                return ReleaseBinBaseUrl + ReleaseBinArchiveName_Win_x86x64;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    && (RuntimeInformation.OSArchitecture == Architecture.X86
                        || RuntimeInformation.OSArchitecture == Architecture.X64))
            {
                return ReleaseBinBaseUrl + ReleaseBinArchiveName_Linux_x86x64;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    && (RuntimeInformation.OSArchitecture == Architecture.Arm64))
            {
                return ReleaseBinBaseUrl + ReleaseBinArchiveName_Linux_Arm64;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    && (RuntimeInformation.OSArchitecture == Architecture.X86
                        || RuntimeInformation.OSArchitecture == Architecture.X64))
            {
                return ReleaseBinBaseUrl + ReleaseBinArchiveName_MacOS_x86x64;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    && (RuntimeInformation.OSArchitecture == Architecture.Arm64))
            {
                return ReleaseBinBaseUrl + ReleaseBinArchiveName_MacOS_Arm64;
            }

            CoutWriteLine($"Unexpected OS/Architecture: {RuntimeInformation.OSDescription} / {RuntimeInformation.OSArchitecture}.");
            CoutWriteLine("Giving up.");
            throw new Exception($"Unexpected OS/Architecture: {RuntimeInformation.OSDescription} / {RuntimeInformation.OSArchitecture}.");
#endif
        }

        private void InstallTemporalLite_PrecompiledFromToolsRepo(string temporalLiteExePath)
        {
            const string BuildToolsRepoRootDirName = "temporal-dotnet-buildtools";
            const string TemporalLiteZipDirName = "TemporalLite";
            const string TemporalLiteZipFileName = "temporalite-win-1.16.2.exe.zip";

            const string TemporalLiteExeZipEntryName = "temporalite-1.16.2.exe";

            EnsureRunningWindows();

            CoutWriteLine();
            CoutWriteLine($"TemporalLite executable not found at \"{temporalLiteExePath}\".");
            CoutWriteLine($"Trying to install ({nameof(Config.ExeBinarySource)}=`{Config.ExeBinarySource}`)...");
            CoutWriteLine();

            string environmentRootDirPath = TestEnvironment.GetEnvironmentRootDirPath();
            CoutWriteLine($"Root of the Test's Environment: \"{environmentRootDirPath}\".");

            CoutWriteLine($"Checking for Build Tools Repo under \"{BuildToolsRepoRootDirName}\"...");
            string buildToolsRepoRootPath = Path.Combine(environmentRootDirPath, BuildToolsRepoRootDirName);
            if (!File.Exists(buildToolsRepoRootPath))
            {
                string msg = $"Build Tools Repo directory does not exist (\"{buildToolsRepoRootPath}\")."
                           + $" Did you clone `macrogreg/temporal-dotnet-buildtools`?";
                CoutWriteLine(msg);
                throw new Exception(msg);
            }

            CoutWriteLine($"Checking for TemporalLite executable archive...");

            string temporalLiteZipFilePath = Path.Combine(buildToolsRepoRootPath, TemporalLiteZipDirName, TemporalLiteZipFileName);
            if (!File.Exists(temporalLiteZipFilePath))
            {
                CoutWriteLine($"TemporalLite executable archive cannot be found (\"{temporalLiteZipFilePath}\"). Giving up.");
                throw new Exception($"Build Tools Repo directory is present, but TemporalLite executable archive cannot"
                                  + $" be found under \"{temporalLiteZipFilePath}\".");
            }

            CoutWriteLine($"Unpacking TemporalLite executable (\"{temporalLiteZipFilePath}\")...");

            string temporalLiteDirPath = Path.GetDirectoryName(temporalLiteExePath);
            if (Directory.Exists(temporalLiteDirPath))
            {
                CoutWriteLine($"Destination dir exists ({temporalLiteDirPath}).");
            }
            else
            {
                CoutWriteLine($"Destination dir does not exist ({temporalLiteDirPath}). Creating...");
                DirectoryInfo destDir = Directory.CreateDirectory(temporalLiteDirPath);

                if (destDir.Exists)
                {
                    CoutWriteLine($"Destination dir successfully created (${temporalLiteDirPath}).");
                }
                else
                {
                    CoutWriteLine($"Cound not create destination dir (${temporalLiteDirPath}).");
                    throw new Exception($"Cound not create destination dir for the TemporalLite Exe (${temporalLiteDirPath}).");
                }
            }

            using FileStream inFStr = new(temporalLiteZipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using ZipArchive archive = new(inFStr, ZipArchiveMode.Read);

            CoutWriteLine($"Archive opened. Accessing entry \"{TemporalLiteExeZipEntryName}\".");
            ZipArchiveEntry entry = archive.GetEntry(TemporalLiteExeZipEntryName);

            CoutWriteLine($"Extracting entry to \"{temporalLiteExePath}\"...");
            entry.ExtractToFile(temporalLiteExePath, overwrite: false);

            CoutWriteLine($"Extracted.");
            CoutWriteLine();
            CoutWriteLine($"TemporalLite has been installed.");
            CoutWriteLine();
        }

        private void Start(string temporalLiteExePath)
        {
            const string TemporalLiteNamespace = "default";
            const string TemporalLiteProcArgsTemplate = "start --ephemeral --namespace {0}";

            const string TemporalLiteInitCompletedMsg = "worker service started";
            const int TemporalLiteInitTimeoutMillis = 15000;

            string temporalLiteProcArgs = String.Format(TemporalLiteProcArgsTemplate, TemporalLiteNamespace);

            try
            {
                _temporalLiteProc = ProcessManager.Start(temporalLiteExePath,
                                                         temporalLiteProcArgs,
                                                         new ProcessManager.WaitForInitOptions(TemporalLiteInitCompletedMsg,
                                                                                               TemporalLiteInitTimeoutMillis),
                                                         _redirectServerOutToCout,
                                                         "TmprlLt",
                                                         _cout);

                CoutWriteLine($"TemporalLite started (ProcId={_temporalLiteProc.Process.Id}).");
            }
            catch (TimeoutException toEx)
            {
                throw new TimeoutException($"Starting/Initializing of TemporalLite timed out after {TemporalLiteInitTimeoutMillis}ms."
                                         + $" Check that TemporalLite is not already running.",
                                           toEx);
            }
        }

        public Task ShutdownAsync()
        {
            ProcessManager temporalLiteProc = Interlocked.Exchange(ref _temporalLiteProc, null);
            if (temporalLiteProc != null)
            {
                Shutdown(temporalLiteProc);
            }

            return Task.CompletedTask;
        }

        private void Shutdown(ProcessManager temporalLiteProc)
        {
            const int CtrlCTimeoutMillis = 50;
            const int KillTimeoutMillis = 100;

            int startMillis = Environment.TickCount;

            if (temporalLiteProc.Process.HasExited)
            {
                CoutWriteLine($"TemporalLite was already shut down when Shutdown was actually requested."
                            + $" Draining output (timeout={KillTimeoutMillis} msec)...");

                temporalLiteProc.DrainOutput(KillTimeoutMillis);
            }
            else
            {
                CoutWriteLine($"Shutting down TemporalLite (timeout={CtrlCTimeoutMillis} msec)...");

                if (temporalLiteProc.SendCtrlCAndWaitForExit(CtrlCTimeoutMillis))
                {
                    CoutWriteLine($"Successfully shut down TemporalLite.");
                }
                else
                {
                    CoutWriteLine($"Could not gracefully shut down TemporalLite within the timeout ({KillTimeoutMillis} msec)."
                                + $" Will kill the process.");

                    temporalLiteProc.KillAndWaitForExit(KillTimeoutMillis);
                }
            }

            int elapsedMillis = Environment.TickCount - startMillis;
            CoutWriteLine($"Shutdown took {elapsedMillis} msec.");
        }

        private static void EnsureRunningWindows()
        {
            if (!TestEnvironment.IsWindows)
            {
                throw new PlatformNotSupportedException($"With the currently specified {nameof(Config)} settings,"
                                                      + $" {nameof(TemporalLiteExeTestServerController)} currently only supports Windows.");
            }
        }

#pragma warning disable CS0162 // Unreachable code detected: Using const bools for settings
        private string GetTemporalLiteExePath()
        {
            const string TemporalLiteExeDirName = "TemporalLite";

            string temporalLiteExeFileName = null;

            if (Config.ExeBinarySource == ExeBinarySource.PrecompiledFromToolsRepo)
            {
                temporalLiteExeFileName = "temporalite-1.16.2.exe";
            }
            else if (Config.ExeBinarySource == ExeBinarySource.ReleaseBinTemporalLiteRepo)

            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    temporalLiteExeFileName = "temporalite.exe";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                        || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    temporalLiteExeFileName = "temporalite";
                }
            }

            if (temporalLiteExeFileName == null)
            {
                string errMsg = $"Could not decide on TemporalLite exe file name."
                              + $" {nameof(Config)}.{nameof(Config.ExeBinarySource)}: {Config.ExeBinarySource}."
                              + $" OSDescription: {RuntimeInformation.OSDescription}.";

                CoutWriteLine(errMsg);
                CoutWriteLine("Giving up.");
                throw new Exception(errMsg);
            }

            string binaryRootDirPath = TestEnvironment.GetBinaryRootDirPath();
            return Path.Combine(binaryRootDirPath, TemporalLiteExeDirName, temporalLiteExeFileName);
        }
#pragma warning restore CS0162 // Unreachable code detected

        public void Dispose()
        {
            ShutdownAsync().GetAwaiter().GetResult();
        }

        private static string CoutPrefix(string text)
        {
            return (text == null) ? text : ("[TmprlLt Ctrl] " + text);
        }

        private void CoutWriteLine(string text = null)
        {
            if (text == null)
            {
                _cout.WriteLine(String.Empty);
            }
            else
            {
                _cout.WriteLine(CoutPrefix(text));
            }
        }

        private static bool IsPortInUse(int port)

        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    return true;

                }
            }

            return false;
        }
    }
}
