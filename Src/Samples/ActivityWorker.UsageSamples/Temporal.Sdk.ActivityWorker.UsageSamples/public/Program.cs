using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Activities.Worker;
using Temporal.Util;
using Temporal.Common.Payloads;

using SerializedPayloads = Temporal.Api.Common.V1.Payloads;

namespace Temporal.Sdk.ActivityWorker.UsageSamples
{
    public class Program
    {
        public static void Main(string[] _)
        {
            Console.WriteLine($"RuntimeEnvironmentInfo: \n{RuntimeEnvironmentInfo.SingletonInstance}");

            Console.WriteLine($"\n{typeof(Program).FullName} has finished.\n");
        }

        private static class Sample01
        {
            public static void SayHello()
            {
                Console.WriteLine("Hello World!");
            }

            public static async Task ExecuteActivityHostAsync()
            {
                // Create a new worker:
                using TemporalActivityWorker worker = new();

                // Add an activity:
                worker.HostActivity("Say-Hello", (_) => SayHello());

                // Start the worker (the returned task is completed when the worker has started):
                await worker.StartAsync();

                Console.WriteLine($"{nameof(Sample01)}: Worker started. Press enter to terminate.");
                Console.ReadLine();

                // Terminate the worker (the returned task is completed when the worker has finished shutting down):
                await worker.TerminateAsync();
            }
        }

        internal class NameData
        {
            public string Name { get; }
            public NameData(string name) { Name = name; }
            public override string ToString() { return Name ?? ""; }
        }

        private static class Sample02
        {
            public static void SayHello()
            {
                Console.WriteLine("Hello!");
            }

            public static void SayHelloWithMetadata(IWorkflowActivityContext activityCtx)
            {
                string reqWfId = activityCtx.RequestingWorkflow.WorkflowId;
                string reqWfRunId = activityCtx.RequestingWorkflow.RunId;

                Console.WriteLine($"Hello! (Requested by Wf:[Id='{reqWfId}'; RunId='{reqWfRunId}'])");
            }

            public static void SayHelloName(NameData name)
            {
                Console.WriteLine($"Hello, {name}!");
            }

            public static void SayHelloNameWithMetadata(NameData name, IWorkflowActivityContext activityCtx)
            {
                string reqWfId = activityCtx.RequestingWorkflow.WorkflowId;
                string reqWfRunId = activityCtx.RequestingWorkflow.RunId;

                Console.WriteLine($"Hello, {name}! (Requested by Wf:[Id='{reqWfId}'; RunId='{reqWfRunId}'])");
            }

            public static string EnterWord()
            {
                Console.Write($"Enter a word >");
                return Console.ReadLine();
            }

            public static string EnterWordWithMetadata(IWorkflowActivityContext activityCtx)
            {
                Console.Write($"Enter a word (WfType:'{activityCtx.RequestingWorkflow.TypeName}') >");
                return Console.ReadLine();
            }

            public static string EnterWordWithName(NameData name)
            {
                Console.Write($"Hi, {name}, enter a word >");
                return Console.ReadLine();
            }

            public static string EnterWordWithNameAndMetadata(NameData name, IWorkflowActivityContext activityCtx)
            {
                Console.Write($"Hi, {name}, enter a word (WfType:'{activityCtx.RequestingWorkflow.TypeName}') >");
                return Console.ReadLine();
            }

            public static async Task ExecuteActivityHostAsync()
            {
                using TemporalActivityWorker worker = new();

                worker.HostActivity("Say-Hello", SayHello)
                      .HostActivity("Say-Hello2", SayHelloWithMetadata)

                      .HostActivity<NameData>("Say-Name", SayHelloName)
                      .HostActivity<NameData>("Say-Name2", SayHelloNameWithMetadata)

                      .HostActivity("Enter-Word", EnterWord)
                      .HostActivity("Enter-Word2", EnterWordWithMetadata)

                      .HostActivity<NameData, string>("Enter-Word-Name", EnterWordWithName)
                      .HostActivity<NameData, string>("Enter-Word-Name2", EnterWordWithNameAndMetadata);

                await worker.StartAsync();

                Console.WriteLine($"{nameof(Sample01)}: Worker started. Press enter to terminate.");
                Console.ReadLine();

                await worker.TerminateAsync();
            }

            private static class Sample03
            {
                public static async Task WriteHelloAsync()
                {
                    using StreamWriter writer = new("DemoFile.txt");
                    await writer.WriteLineAsync("Hello!");
                }

                public static async Task WriteHelloWithCancelAsync(CancellationToken cancelToken)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    using StreamWriter writer = new("DemoFile.txt");
                    await writer.WriteLineAsync($"Hello!");
                }

                public static async Task WriteHelloWithMetadataAsync(IWorkflowActivityContext activityCtx)
                {
                    activityCtx.CancelToken.ThrowIfCancellationRequested();

                    string reqWfId = activityCtx.RequestingWorkflow.WorkflowId;
                    string reqWfRunId = activityCtx.RequestingWorkflow.RunId;

                    using StreamWriter writer = new("DemoFile.txt");
                    await writer.WriteLineAsync($"Hello! (Requested by Wf:[Id='{reqWfId}'; RunId='{reqWfRunId}'])");
                }

                public static async Task WriteHelloNameAsync(NameData name)
                {
                    using StreamWriter writer = new("DemoFile.txt");
                    await writer.WriteLineAsync($"Hello {name}!");
                }

                public static async Task WriteHelloNameWithCancelAsync(NameData name, CancellationToken cancelToken)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    using StreamWriter writer = new("DemoFile.txt");
                    await writer.WriteLineAsync($"Hello {name}!");
                }

                public static async Task WriteHelloNameWithMetadataAsync(NameData name, IWorkflowActivityContext activityCtx)
                {
                    activityCtx.CancelToken.ThrowIfCancellationRequested();

                    string reqWfId = activityCtx.RequestingWorkflow.WorkflowId;
                    string reqWfRunId = activityCtx.RequestingWorkflow.RunId;

                    using StreamWriter writer = new("DemoFile.txt");
                    await writer.WriteLineAsync($"Hello {name}! (Requested by Wf:[Id='{reqWfId}'; RunId='{reqWfRunId}'])");
                }

                public static async Task<string> ReadWordAsync()
                {
                    using StreamReader reader = new("DemoFile.txt");
                    return await reader.ReadLineAsync();
                }

                public static async Task<string> ReadWordWithCancelAsync(CancellationToken cancelToken)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    using StreamReader reader = new("DemoFile.txt");
                    return await reader.ReadLineAsync();
                }

                public static async Task<string> ReadWordWithMetadataAsync(IWorkflowActivityContext activityCtx)
                {
                    activityCtx.CancelToken.ThrowIfCancellationRequested();

                    using StreamReader reader = new("DemoFile.txt");
                    string line = await reader.ReadLineAsync();

                    return $"{line} (read on behalf of:'{activityCtx.RequestingWorkflow.TypeName}')";
                }

                public static async Task<string> ReadWordWithNameAsync(NameData name)
                {
                    using StreamReader reader = new("DemoFile.txt");
                    string line = await reader.ReadLineAsync();

                    return $"{line} (read for '{name}')";
                }

                public static async Task<string> ReadWordWithNameAndCancelAsync(NameData name, CancellationToken cancelToken)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    using StreamReader reader = new("DemoFile.txt");
                    string line = await reader.ReadLineAsync();

                    return $"{line} (read for '{name}')";
                }

                public static async Task<string> ReadWordWithNameAndMetadataAsync(NameData name, IWorkflowActivityContext activityCtx)
                {
                    activityCtx.CancelToken.ThrowIfCancellationRequested();

                    using StreamReader reader = new("DemoFile.txt");
                    string line = await reader.ReadLineAsync();

                    return $"{line} (read for '{name}' on behalf of:'{activityCtx.RequestingWorkflow.TypeName}')";
                }

                public static async Task ExecuteActivityHostAsync()
                {
                    using TemporalActivityWorker worker = new();

                    worker.HostActivity("Write-Hello", WriteHelloAsync)
                          .HostActivity("Write-Hello2", (ctx) => WriteHelloWithCancelAsync(ctx.CancelToken))
                          .HostActivity("Write-Hello3", WriteHelloWithMetadataAsync)

                          .HostActivity<NameData>("Write-Name", WriteHelloNameAsync)
                          .HostActivity<NameData>("Write-Name2", (nm, ctx) => WriteHelloNameWithCancelAsync(nm, ctx.CancelToken))
                          .HostActivity<NameData>("Write-Name3", WriteHelloNameWithMetadataAsync)

                          .HostActivity("Read-Word", ReadWordAsync)
                          .HostActivity("Read-Word2", (ctx) => ReadWordWithCancelAsync(ctx.CancelToken))
                          .HostActivity("Read-Word3", ReadWordWithMetadataAsync)

                          .HostActivity<NameData, string>("Read-Word-Name", ReadWordWithNameAsync)
                          .HostActivity<NameData, string>("Read-Word-Name2", (nm, ctx) => ReadWordWithNameAndCancelAsync(nm, ctx.CancelToken))
                          .HostActivity<NameData, string>("Read-Word-Name3", ReadWordWithNameAndMetadataAsync);

                    await worker.StartAsync();

                    Console.WriteLine($"{nameof(Sample01)}: Worker started. Press enter to terminate.");
                    Console.ReadLine();

                    await worker.TerminateAsync();
                }
            }
        }

        private static class Sample04
        {
            public class SayHelloActivity : IActivityImplementation
            {
                private static readonly Task<SerializedPayloads> s_completedTask = Task.FromResult<SerializedPayloads>(null);

                public Task<SerializedPayloads> ExecuteAsync(IWorkflowActivityContext _)
                {
                    Console.WriteLine("Hello World!");
                    return s_completedTask;
                }
            }

            public static async void ConfigureActivityHost()
            {
                // The SAME activity instance will be used for every invocation:

                TemporalActivityWorker worker1 = new();

                SayHelloActivity activity = new();
                worker1.HostActivity(activity);                     // Activity-type-name will be "SayHello" (extracted from typeof(SayHelloActivity))

                worker1.HostActivity("SayHi", activity);            // Activity-type-name wil be "SayHi" (explicitly specified)


                // A NEW activity instance will be used for every invocation:

                TemporalActivityWorker worker2 = new();

                worker2.HostActivity<SayHelloActivity>();           // Activity-type-name will be "SayHello" (extracted from typeof(SayHelloActivity))

                worker2.HostActivity<SayHelloActivity>("SayHi");    // Activity-type-name wil be "SayHi" (explicitly specified)

                await Task.WhenAll(worker1.StartAsync(),
                                   worker2.StartAsync());

                Console.WriteLine($"{nameof(Sample01)}: Workers started. Press enter to terminate.");
                Console.ReadLine();

                await Task.WhenAll(worker1.TerminateAsync(),
                                   worker2.TerminateAsync());
            }
        }

        private static class Sample05
        {
            public static Task<double> ComputeStuffAsync(int x, int y, int z)
            {
                double d = Math.Sqrt(x * x + y * y + z * z);
                return Task.FromResult(d);
            }

            public static async void ConfigureActivityHost()
            {

                TemporalActivityWorker worker = new();


                worker.HostActivity<PayloadContainers.IUnnamed, double>("ComputeStuff",
                                                                        (inp) => ComputeStuffAsync(inp.GetValue<int>(0),
                                                                                                   inp.GetValue<int>(1),
                                                                                                   inp.GetValue<int>(2)));

                await worker.StartAsync();

                Console.WriteLine($"{nameof(Sample01)}: Workers started. Press enter to terminate.");
                Console.ReadLine();

                await worker.TerminateAsync();
            }
        }
    }
}