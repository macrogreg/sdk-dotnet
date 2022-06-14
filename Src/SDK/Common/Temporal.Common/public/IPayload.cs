using System.Threading.Tasks;

namespace Temporal.Common
{
    public interface IPayload
    {
        public sealed class Void : IPayload
        {
            public static readonly Void Instance = new();
            public static readonly Task<IPayload.Void> CompletedTask = Task.FromResult(Instance);
        }
    }
}
