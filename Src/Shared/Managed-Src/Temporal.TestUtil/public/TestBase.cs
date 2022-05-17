using System;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Util;
using Xunit;
using Xunit.Abstractions;

namespace Temporal.TestUtil
{
    public class TestBase : IAsyncLifetime, IDisposable
    {
        private readonly ITestOutputHelper _cout;
        private volatile int _isDisposed = 0;
        private string _coutWriteLineMoniker = null;

        /// <summary>Prevents subclasses from not passing the required parameters by making the default ctor private.</summary>
        private TestBase()
        {
        }

        protected TestBase(ITestOutputHelper cout)
        {
            Validate.NotNull(cout);
            _cout = cout;

            CoutWriteLine($"{this.GetType().Name}: {RuntimeEnvironmentInfo.SingletonInstance}");
        }

        public virtual ITestOutputHelper Cout
        {
            get { return _cout; }
        }

        public string CoutWriteLineMoniker
        {
            get { return _coutWriteLineMoniker; }
            set { _coutWriteLineMoniker = value; }
        }

        public virtual void CoutWriteLine(string text = null)
        {
            if (text == null)
            {
                _cout.WriteLine(String.Empty);
            }
            else
            {
                string coutWriteLineMoniker = _coutWriteLineMoniker;
                if (coutWriteLineMoniker == null)
                {
                    _cout.WriteLine(text);
                }
                else
                {
                    _cout.WriteLine('[' + coutWriteLineMoniker + ']' + text);
                }
            }
        }

        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task DisposeAsync()
        {
            if (0 == Interlocked.Exchange(ref _isDisposed, 1))
            {
                Dispose(isDisposingSync: false, isDisposingAsync: true, isFinalizing: false);
            }

            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool isDisposingSync, bool isDisposingAsync, bool isFinalizing)
        {
            if (isFinalizing)
            {
                Cout.WriteLine($"{nameof(Dispose)}(..) method was called from the finalizer."
                             + $" This might indicate a problem with the test setup."
                             + $" Test class: \"{this.GetType().FullName}\".");
            }

            int c = 0;
            c += isDisposingSync ? 1 : 0;
            c += isDisposingAsync ? 1 : 0;
            c += isFinalizing ? 1 : 0;

            if (c != 1)
            {
                Cout.WriteLine($"During {nameof(Dispose)}(..) exactly one of the invoker flags must be True. However, {c} such flags are True:"
                             + $" isDisposingSync={isDisposingSync}; isDisposingAsync={isDisposingAsync}; isFinalizing={isFinalizing}."
                             + $" This might indicate a problem with the test setup."
                             + $" Test class: \"{this.GetType().FullName}\".");
            }

            _isDisposed = 1;
        }

        ~TestBase()
        {
            if (0 == Interlocked.Exchange(ref _isDisposed, 1))
            {
                Dispose(isDisposingSync: false, isDisposingAsync: false, isFinalizing: true);
            }
        }

        public void Dispose()
        {
            if (0 == Interlocked.Exchange(ref _isDisposed, 1))
            {
                Dispose(isDisposingSync: true, isDisposingAsync: false, isFinalizing: false);
                GC.SuppressFinalize(this);
            }
        }
    }
}
