using System.Threading;
using System.Threading.Tasks;

using Xunit.Abstractions;

namespace Temporal.TestUtil
{
    public class IntegrationTestBase : TestBase
    {
        private const bool RedirectServerOutToTstoutDefault = false;

        private readonly bool _redirectServerOutToTstout;
        private ITemporalTestServerController _testServer = null;

        private readonly TestCaseContextMonikers _testCaseContextMonikers;

        public IntegrationTestBase(ITestOutputHelper tstout)
            : this(tstout, RedirectServerOutToTstoutDefault)
        {
        }

        public IntegrationTestBase(ITestOutputHelper tstout, bool redirectServerOutToTstout)
            : base(tstout)
        {
            _redirectServerOutToTstout = redirectServerOutToTstout;
            _testCaseContextMonikers = new TestCaseContextMonikers(System.DateTimeOffset.Now);
        }

        internal virtual ITemporalTestServerController TestServer
        {
            get { return _testServer; }
        }

        internal virtual TestCaseContextMonikers TestCaseContextMonikers
        {
            get { return _testCaseContextMonikers; }
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // In the future, when we will have other test servers (docker base, remote, ...), we will use some sort of
            // configuration mechanism to decide on the implementation chosen.

            _testServer = new TemporalLiteExeTestServerController(Tstout, _redirectServerOutToTstout);
            await _testServer.StartAsync();
        }

        public override async Task DisposeAsync()
        {
            ITemporalTestServerController testServer = Interlocked.Exchange(ref _testServer, null);
            if (testServer != null)
            {
                await testServer.ShutdownAsync();
            }

            await base.DisposeAsync();
        }
    }
}