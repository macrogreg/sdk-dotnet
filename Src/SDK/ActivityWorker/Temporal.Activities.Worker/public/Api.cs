using System;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Common;
using Temporal.Serialization;
using Temporal.Util;
using SerializedPayloads = Temporal.Api.Common.V1.Payloads;

// <summary>
// This file contains design-phase APIs.
// We will refactor and implement after the Activity WOrker design is complete.
// </summary>
namespace Temporal.Activities.Worker
{
    public static class Api
    {
    }

    public class TemporalActivityWorker : IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task StartAsync()
        {
            throw new NotImplementedException();
        }

        public Task TerminateAsync()
        {
            throw new NotImplementedException();
        }

        public TemporalActivityWorker HostActivity(IActivityImplementationFactory activityFactory)
        {
            throw new NotImplementedException("@ToDo");
            //return this;
        }

        #region HostActivity for a specific `activity`-instance that is reused actoss invocations

        /// <summary>
        /// Uses the specified <c>activity</c> for all activity invocations. I.e., same instance is shared across invocations.
        /// </summary>        
        public TemporalActivityWorker HostActivity(IActivityImplementation activity)
        {
            return HostActivity(BasicActivityImplementationFactory.GetDefaultActivityTypeNameForImplementationType(activity.GetType()),
                                activity);
        }

        /// <summary>
        /// Uses the specified <c>activity</c> for all activity invocations. I.e., same instance is shared across invocations.
        /// </summary>        
        public TemporalActivityWorker HostActivity(string activityTypeName, IActivityImplementation activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create(activityTypeName, activity));
        }

        #endregion HostActivity for a specific `activity`-instance that is reused actoss invocations

        #region HostActivity for <TActImpl> where TActImpl : IActivityImplementation, new()

        public TemporalActivityWorker HostActivity<TActImpl>()
                where TActImpl : IActivityImplementation, new()
        {
            return HostActivity<TActImpl>(BasicActivityImplementationFactory.GetDefaultActivityTypeNameForImplementationType(typeof(TActImpl)));
        }

        public TemporalActivityWorker HostActivity<TActImpl>(string activityTypeName)
                where TActImpl : IActivityImplementation, new()
        {
            return HostActivity(new BasicActivityImplementationFactory(activityTypeName, activityCreator: () => new TActImpl()));
        }

        #endregion HostActivity for <TActImpl> where TActImpl : IActivityImplementation, new()

        #region HostActivity takes `Func<.., Task<TResult>>` with 0 inputs

        public TemporalActivityWorker HostActivity<TResult>(string activityTypeName, Func<Task<TResult>> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<IPayload.Void, TResult>(
                    activityTypeName,
                    (_, __) => activity()));
        }

        public TemporalActivityWorker HostActivity<TArg, TResult>(string activityTypeName,
                                                                  Func<IWorkflowActivityContext, Task<TResult>> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<TArg, TResult>(
                    activityTypeName,
                    (_, ctx) => activity(ctx)));
        }

        #endregion HostActivity takes `Func<.., Task<TResult>>` with 0 inputs

        #region HostActivity takes `Func<.., Task<TResult>>` with 1 input

        public TemporalActivityWorker HostActivity<TArg, TResult>(string activityTypeName, Func<TArg, Task<TResult>> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<TArg, TResult>(
                    activityTypeName,
                    (inp, _) => activity(inp)));
        }

        public TemporalActivityWorker HostActivity<TArg, TResult>(string activityTypeName,
                                                                  Func<TArg, IWorkflowActivityContext, Task<TResult>> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<TArg, TResult>(
                    activityTypeName,
                    (inp, ctx) => activity(inp, ctx)));
        }

        #endregion HostActivity takes `Func<.., Task<TResult>>` with 1 input

        #region HostActivity takes async `Func<.., Task>` with 0 inputs (Task is NOT Task<TResult>)

        public TemporalActivityWorker HostActivity(string activityTypeName, Func<Task> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<IPayload.Void, IPayload.Void>(
                    activityTypeName,
                    async (_, __) =>
                    {
                        await activity();
                        return IPayload.Void.Instance;
                    }));
        }

        public TemporalActivityWorker HostActivity(string activityTypeName, Func<IWorkflowActivityContext, Task> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<IPayload.Void, IPayload.Void>(
                    activityTypeName,
                    async (_, ctx) =>
                    {
                        await activity(ctx);
                        return IPayload.Void.Instance;
                    }));
        }

        #endregion HostActivity takes async `Func<.., Task>` with 0 inputs (Task is NOT Task<TResult>)

        #region HostActivity takes async `Func<.., Task>` with 1 input (Task is NOT Task<TResult>)

        public TemporalActivityWorker HostActivity<TArg>(string activityTypeName, Func<TArg, Task> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<TArg, IPayload.Void>(
                    activityTypeName,
                    async (inp, _) =>
                    {
                        await activity(inp);
                        return IPayload.Void.Instance;
                    }));
        }

        public TemporalActivityWorker HostActivity<TArg>(string activityTypeName, Func<TArg, IWorkflowActivityContext, Task> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<TArg, IPayload.Void>(
                    activityTypeName,
                    async (inp, ctx) =>
                    {
                        await activity(inp, ctx);
                        return IPayload.Void.Instance;
                    }));
        }

        #endregion HostActivity takes async `Func<.., Task>` with 1 input (Task is NOT Task<TResult>)

        #region HostActivity takes `Func<.., TResult>` with 0 inputs (`TResult` must not be Task)

        public TemporalActivityWorker HostActivity<TResult>(string activityTypeName, Func<TResult> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<IPayload.Void, TResult>(
                    activityTypeName,
                    (_, __) =>
                    {
                        TResult r = activity();
                        return Task.FromResult(r);
                    }));
        }

        public TemporalActivityWorker HostActivity<TResult>(string activityTypeName, Func<IWorkflowActivityContext, TResult> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<IPayload.Void, TResult>(
                    activityTypeName,
                    (_, ctx) =>
                    {
                        TResult r = activity(ctx);
                        return Task.FromResult(r);
                    }));
        }

        #endregion HostActivity takes `Func<.., TResult>` with 0 inputs (`TResult` must not be Task)

        #region HostActivity takes `Func<.., TResult>` with 1 input (`TResult` must not be Task)

        public TemporalActivityWorker HostActivity<TArg, TResult>(string activityTypeName, Func<TArg, TResult> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<TArg, TResult>(
                    activityTypeName,
                    (inp, _) =>
                    {
                        TResult r = activity(inp);
                        return Task.FromResult(r);
                    }));
        }

        public TemporalActivityWorker HostActivity<TArg, TResult>(string activityTypeName,
                                                                  Func<TArg, IWorkflowActivityContext, TResult> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<TArg, TResult>(
                    activityTypeName,
                    (inp, ctx) =>
                    {
                        TResult r = activity(inp, ctx);
                        return Task.FromResult(r);
                    }));
        }

        #endregion HostActivity takes `Func<.., TResult>` with 1 input (`TResult` must not be Task)

        #region HostActivity takes `Action<..>` with 0 inputs

        public TemporalActivityWorker HostActivity(string activityTypeName, Action activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<IPayload.Void, IPayload.Void>(
                    activityTypeName,
                    (_, __) =>
                    {
                        activity();
                        return IPayload.Void.CompletedTask;
                    }));
        }

        public TemporalActivityWorker HostActivity(string activityTypeName, Action<IWorkflowActivityContext> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<IPayload.Void, IPayload.Void>(
                    activityTypeName,
                    (_, ctx) =>
                    {
                        activity(ctx);
                        return IPayload.Void.CompletedTask;
                    }));
        }

        #endregion HostActivity takes `Action<..>` with 0 inputs

        #region HostActivity takes `Action<..>` with 1 input

        public TemporalActivityWorker HostActivity<TArg>(string activityTypeName, Action<TArg> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<TArg, IPayload.Void>(
                    activityTypeName,
                    (inp, _) =>
                    {
                        activity(inp);
                        return IPayload.Void.CompletedTask;
                    }));
        }

        public TemporalActivityWorker HostActivity<TArg>(string activityTypeName, Action<TArg, IWorkflowActivityContext> activity)
        {
            return HostActivity(BasicActivityImplementationFactory.Create<TArg, IPayload.Void>(
                    activityTypeName,
                    (inp, ctx) =>
                    {
                        activity(inp, ctx);
                        return IPayload.Void.CompletedTask;
                    }));
        }

        #endregion HostActivity takes `Action<..>` with 1 input

    }

    public class TemporalActivityWorkerConfiguration
    {

    }

    public record RequestingWorkflowInfo(string WorkflowId,
                                         string RunId,
                                         string TypeName);

    public interface IWorkflowActivityContext
    {
        SerializedPayloads Input { get; }
        IPayloadConverter PayloadConverter { get; }
        CancellationToken CancelToken { get; }

        RequestingWorkflowInfo RequestingWorkflow { get; }
    }

    public interface IActivityImplementation
    {
        Task<SerializedPayloads> ExecuteAsync(IWorkflowActivityContext activityCtx);
    }

    internal class ActivityAdapter<TArg, TResult> : IActivityImplementation
    {
        private readonly Func<TArg, IWorkflowActivityContext, Task<TResult>> _activity;

        internal ActivityAdapter(Func<TArg, IWorkflowActivityContext, Task<TResult>> activity)
        {
            _activity = activity;
        }

        public async Task<SerializedPayloads> ExecuteAsync(IWorkflowActivityContext activityCtx)
        {
            SerializedPayloads serializedInput = activityCtx.Input;
            TArg input = activityCtx.PayloadConverter.Deserialize<TArg>(serializedInput);

            TResult output = await _activity(input, activityCtx);

            SerializedPayloads serializedOutputAccumulator = new();
            activityCtx.PayloadConverter.Serialize<TResult>(output, serializedOutputAccumulator);

            return serializedOutputAccumulator;
        }
    }

    public interface IActivityImplementationFactory
    {
        string ActivityTypeName { get; }

        public IActivityImplementation CreateActivity();
    }

    internal class BasicActivityImplementationFactory : IActivityImplementationFactory
    {
        #region Static APIs

        public static string GetDefaultActivityTypeNameForImplementationType(Type activityImplementationType)
        {
            const string ActivityMonikerSuffix = "Activity";

            Validate.NotNull(activityImplementationType);

            string implTypeName = activityImplementationType.Name;
            string activityTypeName = implTypeName.EndsWith(ActivityMonikerSuffix, StringComparison.OrdinalIgnoreCase)
                    ? implTypeName.Substring(implTypeName.Length - ActivityMonikerSuffix.Length)
                    : implTypeName;

            return activityTypeName;
        }

        public static BasicActivityImplementationFactory Create(string activityTypeName, IActivityImplementation activity)
        {
            return new BasicActivityImplementationFactory(activityTypeName, () => activity);
        }

        public static BasicActivityImplementationFactory Create<TArg, TResult>(string activityTypeName,
                                                                               Func<TArg, IWorkflowActivityContext, Task<TResult>> activity)
        {
            return new BasicActivityImplementationFactory(activityTypeName,
                                                          () => new ActivityAdapter<TArg, TResult>(activity));
        }

        #endregion Static APIs

        private readonly string _activityTypeName;
        private readonly Func<IActivityImplementation> _activityCreator;

        public BasicActivityImplementationFactory(string activityTypeName,
                                                  Func<IActivityImplementation> activityCreator)
        {
            _activityTypeName = activityTypeName;
            _activityCreator = activityCreator;
        }


        public virtual string ActivityTypeName
        {
            get { return _activityTypeName; }
        }

        public IActivityImplementation CreateActivity()
        {
            return _activityCreator();
        }
    }
}
