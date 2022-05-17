using System;
using System.Threading.Tasks;

namespace Temporal.TestUtil
{
    internal class JavaBasedTemporalTestServerController : ITemporalTestServerController
    {
        public Task StartAsync()
        {
            throw new NotImplementedException($"{nameof(JavaBasedTemporalTestServerController)} is not implemented.");
        }

        public Task ShutdownAsync()
        {
            throw new NotImplementedException($"{nameof(JavaBasedTemporalTestServerController)} is not implemented.");
        }
    }
}
