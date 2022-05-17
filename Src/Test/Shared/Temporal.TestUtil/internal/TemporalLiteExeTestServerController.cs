using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Util;
using Xunit.Abstractions;

namespace Temporal.TestUtil
{
    internal sealed class TemporalLiteExeTestServerController : ITemporalTestServerController, IDisposable
    {
        private readonly ITestOutputHelper _cout;
        private readonly bool _redirectServerOutToCout;

        private ProcessManager _temporalLiteProc = null;

        public TemporalLiteExeTestServerController(ITestOutputHelper cout, bool redirectServerOutToCout)
        {
            Validate.NotNull(cout);
            _cout = cout;
            _redirectServerOutToCout = redirectServerOutToCout;
        }

        public Task StartAsync()
        {
            EnsureRunningWindows();

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

                return Task.CompletedTask;
            }

            string temporalLiteExePath = GetTemporalLiteExePath();
            if (!File.Exists(temporalLiteExePath))
            {
                InstallTemporalLite(temporalLiteExePath);
            }

            Start(temporalLiteExePath);
            return Task.CompletedTask;
        }

        private void InstallTemporalLite(string temporalLiteExePath)
        {
            const string BuildToolsRepoRootDirName = "temporal-dotnet-buildtools";
            const string TemporalLiteZipDirName = "TemporalLite";
            const string TemporalLiteZipFileName = "temporalite-win-1.16.2.exe.zip";

            const string TemporalLiteExeZipEntryName = "temporalite-1.16.2.exe";

            CoutWriteLine();
            CoutWriteLine($"TemporalLite executable not found at \"{temporalLiteExePath}\".");
            CoutWriteLine("Trying to install...");
            CoutWriteLine();

            string environmentRootDirPath = TestEnvironment.GetEnvironmentRootDirPath();
            CoutWriteLine($"Root of the Test's Environment: \"{environmentRootDirPath}\".");

            CoutWriteLine($"Checking for Build Tools Repo under \"{BuildToolsRepoRootDirName}\"...");
            string buildToolsRepoRootPath = Path.Combine(environmentRootDirPath, BuildToolsRepoRootDirName);
            if (File.Exists(buildToolsRepoRootPath))
            {
                throw new Exception($"Build Tools Repo directory does not exist (\"{buildToolsRepoRootPath}\")."
                                  + $" Did you clone `macrogreg/temporal-dotnet-buildtools`?");
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
                CoutWriteLine($"Destination dir exists (${temporalLiteDirPath}).");
            }
            else
            {
                CoutWriteLine($"Destination dir does not exist (${temporalLiteDirPath}). Creating...");
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
                throw new PlatformNotSupportedException($"{nameof(TemporalLiteExeTestServerController)} currently only supports Windows.");
            }
        }

        private static string GetTemporalLiteExePath()
        {
            const string TemporalLiteExeDirName = "TemporalLite";
            const string TemporalLiteExeFileName = "temporalite-1.16.2.exe";

            string binaryRootDirPath = TestEnvironment.GetBinaryRootDirPath();
            return Path.Combine(binaryRootDirPath, TemporalLiteExeDirName, TemporalLiteExeFileName);
        }

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
