using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Temporal.Util;
using Xunit.Abstractions;

namespace Temporal.TestUtil
{
    internal sealed class ProcessManager : IDisposable
    {
        private readonly Process _process;
        private readonly bool _redirectToCout;
        private readonly ITestOutputHelper _cout;
        private readonly string _coutProcNameMoniker;

        private ManualResetEventSlim _errorSignal = null;
        private ManualResetEventSlim _outputSignal = null;

        private volatile Action<string> _onOutputInspector = null;

        public record WaitForInitOptions(string InitCompletedMsg, int TimeoutMillis);

        public static ProcessManager Start(string exePath,
                                           string args,
                                           WaitForInitOptions waitForInitOptions,
                                           bool redirectToCout,
                                           string coutProcNameMoniker,
                                           ITestOutputHelper cout)
        {
            Validate.NotNull(exePath);

            if (waitForInitOptions != null)
            {
                Validate.NotNullOrWhitespace(waitForInitOptions.InitCompletedMsg);
            }

            int startMillis = Environment.TickCount;
            Process proc = new();

            proc.StartInfo.FileName = exePath;

            if (!String.IsNullOrWhiteSpace(args))
            {
                proc.StartInfo.Arguments = args;
            }

            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;

            cout?.WriteLine($"Starting proc."
                          + $" (RedirectToCout={redirectToCout};"
                          + $" File=\"{proc.StartInfo.FileName}\";"
                          + $" Args={Format.QuoteOrNull(proc.StartInfo.Arguments)})");

            ProcessManager procMan = new(proc, redirectToCout, coutProcNameMoniker, cout);

            ManualResetEventSlim initSignal = null;
            if (waitForInitOptions != null)
            {
                initSignal = new();
                procMan._onOutputInspector = (s) =>
                {
                    if (s != null && s.IndexOf(waitForInitOptions.InitCompletedMsg) >= 0)
                    {
                        procMan._onOutputInspector = null;
                        initSignal.Set();
                    }
                };
            }

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            cout?.WriteLine($"[ProcMan] Proc `{proc.Id}` started (TS: {Format.AsReadablePreciseLocal(DateTimeOffset.Now)})."
                          + $" Startup took {(Environment.TickCount - startMillis)} msec so far.");

            if (initSignal != null)
            {
                cout?.WriteLine($"[ProcMan] Waiting until init-completed-marker is encountered in the proc output"
                              + $" (Timeout={waitForInitOptions.TimeoutMillis}ms; Marker=\"{waitForInitOptions.InitCompletedMsg}\").");

                bool isInitSignalSet = initSignal.Wait(waitForInitOptions.TimeoutMillis);
                //bool isInitSignalSet = initSignal.Wait(Timeout.Infinite);  // for debug
                if (isInitSignalSet)
                {
                    cout?.WriteLine($"[ProcMan] InitCompletedMsg was encountered.");
                }
                else
                {
                    cout?.WriteLine($"[ProcMan] InitCompletedMsg was NOT encountered, but timeout was reached.");

                    throw new TimeoutException($"ProcessManager started the target process, but the process did not initialize within the timeout."
                                             + $" File=\"{proc.StartInfo.FileName}\";"
                                             + $" Args={Format.QuoteOrNull(proc.StartInfo.Arguments)};"
                                             + $" Timeout={waitForInitOptions.TimeoutMillis}ms;"
                                             + $" InitCompletedMsg=\"{waitForInitOptions.InitCompletedMsg}\".");
                }
            }

            int elapsedMillis = Environment.TickCount - startMillis;
            cout?.WriteLine($"[ProcMan] Startup & initialization of proc `{proc.Id}` took {elapsedMillis} msec.");

            return procMan;
        }

        private ProcessManager(Process process, bool redirectToCout, string coutProcNameMoniker, ITestOutputHelper cout)
        {
            Validate.NotNull(process);

            _process = process;
            _redirectToCout = redirectToCout;

            if (redirectToCout)
            {
                Validate.NotNull(coutProcNameMoniker);
                Validate.NotNull(cout);
            }

            _coutProcNameMoniker = coutProcNameMoniker;
            _cout = cout;

            _process.OutputDataReceived += OnOutputDataReceived;
            _process.ErrorDataReceived += OnErrorDataReceived;
        }

        public Process Process
        {
            get { return _process; }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (_redirectToCout)
            {
                _cout.WriteLine($"[{_coutProcNameMoniker}:ERR] {e.Data}");
            }

            ManualResetEventSlim signal = _errorSignal;
            if (signal != null)
            {
                signal.Set();
            }
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (_redirectToCout)
            {
                _cout.WriteLine($"[{_coutProcNameMoniker}:STD] {e.Data}");
            }

            Action<string> onOutputInspector = _onOutputInspector;
            if (onOutputInspector != null)
            {
                onOutputInspector(e.Data);
            }

            ManualResetEventSlim signal = _outputSignal;
            if (signal != null)
            {
                signal.Set();
            }
        }

        public void SendCtrlC()
        {
            bool isSucc = GenerateConsoleCtrlEvent(CtrlEvent.CtrlC, _process.Id);
            _process.StandardInput.Flush();

            _process.CloseMainWindow();
            _process.StandardInput.Write("\x3");
            _process.StandardInput.Flush();
            _process.StandardInput.Close();

            _cout?.WriteLine($"[ProcMan] Sent Ctrl-C to proc `{_process.Id}` (isSucc={isSucc}).");
        }

        public bool SendCtrlCAndWaitForExit(int timeout = Timeout.Infinite)
        {
            SendCtrlC();

            int startMillis = (timeout == Timeout.Infinite) ? 0 : Environment.TickCount;

            _process.WaitForExit(timeout);

            if (timeout != Timeout.Infinite)
            {
                int elapsedMillis = Environment.TickCount - startMillis;
                timeout = Math.Max(1, timeout - elapsedMillis);
            }

            DrainOutput(timeout);
            return _process.HasExited;
        }

        public bool KillAndWaitForExit(int timeout = Timeout.Infinite)
        {
            try
            {
                _process.Kill();
            }
            catch (InvalidOperationException)
            {
            }

            int startMillis = (timeout == Timeout.Infinite) ? 0 : Environment.TickCount;

            _process.WaitForExit(timeout);

            if (timeout != Timeout.Infinite)
            {
                int elapsedMillis = Environment.TickCount - startMillis;
                timeout = Math.Max(1, timeout - elapsedMillis);
            }

            DrainOutput(timeout);
            return _process.HasExited;
        }

        public bool DrainOutput(int timeout = Timeout.Infinite)
        {
            _errorSignal = new ManualResetEventSlim();
            _outputSignal = new ManualResetEventSlim();

            try
            {
                return _outputSignal.Wait(timeout) && _errorSignal.Wait(timeout);
            }
            catch (ArgumentOutOfRangeException aorEx)
            {
                throw new ArgumentOutOfRangeException($"{nameof(timeout)}={timeout}", aorEx);
            }
        }

        public void Dispose()
        {
            _process.OutputDataReceived -= OnOutputDataReceived;
            _process.ErrorDataReceived -= OnErrorDataReceived;

            ManualResetEventSlim errorMutex = Interlocked.Exchange(ref _errorSignal, null);
            if (errorMutex != null)
            {
                errorMutex.Dispose();
            }

            ManualResetEventSlim outputMutex = Interlocked.Exchange(ref _outputSignal, null);
            if (outputMutex != null)
            {
                outputMutex.Dispose();
            }

            _process.Dispose();
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GenerateConsoleCtrlEvent(CtrlEvent dwCtrlEvent, int dwProcessGroupId);

        private enum CtrlEvent
        {
            CtrlC = 0,
            CtrlBreak = 1
        }
    }
}
