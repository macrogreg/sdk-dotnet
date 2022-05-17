using System.Threading.Tasks;

namespace Temporal.TestUtil
{
    internal interface ITemporalTestServerController
    {
        Task StartAsync();
        Task ShutdownAsync();
    }
}
