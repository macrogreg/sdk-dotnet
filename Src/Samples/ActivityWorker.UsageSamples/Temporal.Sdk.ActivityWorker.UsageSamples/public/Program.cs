using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Temporal.Activities.Worker;
using Temporal.Util;

using Temporal.Serialization;
using IUnnamedPayloadContainer = Temporal.Common.Payloads.PayloadContainers.IUnnamed;
using INamedPayloadContainer = Temporal.Common.Payloads2.PayloadContainers.INamed;  // For Demo only. `...Payloads2...` will be just `...Payloads...` later.

using SerializedPayloads = Temporal.Api.Common.V1.Payloads;
using System.Collections.Generic;

namespace Temporal.Sdk.ActivityWorker.UsageSamples
{
    public class Program
    {
        public static void Main(string[] _)
        {
            Console.WriteLine($"RuntimeEnvironmentInfo: \n{RuntimeEnvironmentInfo.SingletonInstance}");

            Console.WriteLine($"\n{typeof(Program).FullName} has finished.\n");
        }

        /// <summary>
        /// Used in samples to pass data around.
        /// </summary>
        internal class NameData
        {
            public string Name { get; }
            public NameData(string name) { Name = name; }
            public override string ToString() { return Name ?? ""; }
        }

        /// <summary>
        /// Minimal code to host an activity worker.
        /// </summary>
        private static class Sample01_MinimalActivityHost
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

                Console.WriteLine($"{nameof(Sample01_MinimalActivityHost)}: Worker started. Press enter to terminate.");
                Console.ReadLine();

                // Terminate the worker (the returned task is completed when the worker has finished shutting down):
                await worker.TerminateAsync();
            }
        }

        /// <summary>
        /// Any existing of new method can become an activity. Just pass it to the worker.
        /// There are signature restrictions. Later samples show how to create wrappers/adapters to overcome those.
        /// This example uses static activity methods.
        /// </summary>
        private static class Sample02_HostSimpleSyncActivities
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

                Console.WriteLine($"{nameof(Sample02_HostSimpleSyncActivities)}: Worker started. Press enter to terminate.");
                Console.ReadLine();

                await worker.TerminateAsync();
            }

            /// <summary>
            /// Async activities are just as simple as sync activities.
            /// This sample shows how to use adapters if there is an existing method that uses
            /// a CancellationToken directly and is not aware of IWorkflowActivityContext.
            /// </summary>
            private static class Sample03_HostSimpleAsyncActivities
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

                    Console.WriteLine($"{nameof(Sample03_HostSimpleAsyncActivities)}: Worker started. Press enter to terminate.");
                    Console.ReadLine();

                    await worker.TerminateAsync();
                }
            }
        }

        /// <summary>
        /// Previous examples show static methods representing activities.
        /// An activity may also be implemented as a method on a class instance, such that the instance is shared
        /// across activity invocations. In such scenarios, make sure that activity invocations may happen concurrently,
        /// and synchronize as appropriate.
        /// </summary>
        private static class Sample04_NonStaticActivityMethods
        {
            public sealed class CustomLogger : IDisposable
            {
                private readonly SemaphoreSlim _lock = new(initialCount: 1);
                private readonly StreamWriter _writer;

                public CustomLogger(string fileName)
                {
                    _writer = new StreamWriter(fileName);
                }

                public void Dispose()
                {

                    DisposeAsync().GetAwaiter().GetResult();
                }

                public async Task DisposeAsync()
                {
                    await _lock.WaitAsync();

                    try
                    {
                        _writer.Dispose();
                    }
                    finally
                    {
                        _lock.Release();
                    }
                }

                public async Task WriteLogLineAsync(string logLevel, string logMessage)
                {
                    await _lock.WaitAsync();

                    try
                    {
                        await _writer.WriteLineAsync($"[{logLevel}] {logMessage}");
                    }
                    finally
                    {
                        _lock.Release();
                    }
                }

                public Task WriteErrorLineAsync(string logMessage)
                {
                    return WriteLogLineAsync("ERR", logMessage);
                }

                public Task WriteInfoLineAsync(string logMessage)
                {
                    return WriteLogLineAsync("INF", logMessage);
                }
            }

            public static async void ConfigureActivityHost()
            {
                CustomLogger logger = new("DemoLog.txt");

                TemporalActivityWorker worker = new();

                worker.HostActivity<string>("Log-Error", logger.WriteErrorLineAsync)
                      .HostActivity<string>("Log-Info", logger.WriteInfoLineAsync);

                await worker.StartAsync();

                Console.WriteLine($"{nameof(Sample04_NonStaticActivityMethods)}: Worker started. Press enter to terminate.");
                Console.ReadLine();

                await worker.TerminateAsync();
            }
        }

        /// <summary>
        /// Previous example shows how an instance method can implements an activity while sharing the underlying
        /// class instance across activity invocations. This example shows how a new instance can be created for each
        /// activity invocation.        
        /// </summary>
        private static class Sample05_NewClassInstanceForEachActivityInvocation
        {
            public sealed class CustomLogger2 : IDisposable
            {
                private readonly StreamWriter _writer;

                public CustomLogger2(string fileName)
                {
                    _writer = new StreamWriter(fileName);
                }

                public void Dispose()
                {
                    _writer.Dispose();
                }

                public Task WriteLogLineAsync(string logLevel, string logMessage)
                {
                    return _writer.WriteLineAsync($"[{logLevel}] {logMessage}");
                }

                public Task WriteErrorLineAsync(string logMessage)
                {
                    return WriteLogLineAsync("ERR", logMessage);
                }

                public Task WriteInfoLineAsync(string logMessage)
                {
                    return WriteLogLineAsync("INF", logMessage);
                }
            }

            private static int s_logFileIndex = 0;

            public static async void ConfigureActivityHost()
            {
                TemporalActivityWorker worker = new();

                worker.HostActivity<string>("Log-Error", (msg) => (new CustomLogger2($"DemoLog-{Interlocked.Increment(ref s_logFileIndex)}.txt")).WriteErrorLineAsync(msg))
                      .HostActivity<string>("Log-Info", (msg) => (new CustomLogger2($"DemoLog-{Interlocked.Increment(ref s_logFileIndex)}.txt")).WriteInfoLineAsync(msg));

                await worker.StartAsync();

                Console.WriteLine($"{nameof(Sample05_NewClassInstanceForEachActivityInvocation)}: Worker started. Press enter to terminate.");
                Console.ReadLine();

                await worker.TerminateAsync();
            }
        }

        /// <summary>
        /// Previous examples use a single business logic argument.
        /// This example demonstrates an adapter for multiple arguments.
        /// </summary>
        private static class Sample06_MultipleArguments
        {
            public static Task<double> ComputeStuffAsync(int x, int y, int z)
            {
                double d = Math.Sqrt(x * x + y * y + z * z);
                return Task.FromResult(d);
            }

            public static async void ConfigureActivityHost()
            {

                TemporalActivityWorker worker = new();

                // .NET Workflows will be encouraged to use named arguments.
                // The transport mechanism is a single payload carrying a json object with named properties.

                worker.HostActivity<INamedPayloadContainer, double>("ComputeStuff",
                                                                    (inp) => ComputeStuffAsync(inp.GetValue<int>("x"),
                                                                                               inp.GetValue<int>("y"),
                                                                                               inp.GetValue<int>("z")));

                // If the workflow is using multiple payload entries to send multiple arguments, an "unnamed" container
                // can be used to receive them. Howeever, the "named" approch (above) is preferred.

                worker.HostActivity<IUnnamedPayloadContainer, double>("ComputeStuff2",
                                                                        (inp) => ComputeStuffAsync(inp.GetValue<int>(0),
                                                                                                   inp.GetValue<int>(1),
                                                                                                   inp.GetValue<int>(2)));

                await worker.StartAsync();

                Console.WriteLine($"{nameof(Sample06_MultipleArguments)}: Worker started. Press enter to terminate.");
                Console.ReadLine();

                await worker.TerminateAsync();
            }
        }

        /// <summary>
        /// This sample shows the underlying mechanics of activity hosting. 
        /// The worker really uses a factory that create a new instance of a class implementing
        /// <see cref="IActivityImplementation"/> for each activity invocation.
        /// 
        /// This is for advanced users only. How shoudl we document this fact?
        /// </summary>
        private static class Sample07_UnderlyingMechanicsA
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

                worker1.HostActivity("SayHi", activity);            // Activity-type-name will be "SayHi" (explicitly specified)


                // A NEW activity instance will be used for every invocation:

                TemporalActivityWorker worker2 = new();

                worker2.HostActivity<SayHelloActivity>();           // Activity-type-name will be "SayHello" (extracted from typeof(SayHelloActivity))

                worker2.HostActivity<SayHelloActivity>("SayHi");    // Activity-type-name wil be "SayHi" (explicitly specified)

                await Task.WhenAll(worker1.StartAsync(),
                                   worker2.StartAsync());

                Console.WriteLine($"{nameof(Sample07_UnderlyingMechanicsA)}: Workers started. Press enter to terminate.");
                Console.ReadLine();

                await Task.WhenAll(worker1.TerminateAsync(),
                                   worker2.TerminateAsync());
            }
        }

        /// <summary>
        /// This sample shows the underlying mechanics of serialization and accessign the underlying payload.        
        /// 
        /// This is for advanced users only. How shoudl we document this fact?
        /// </summary>
        private static class Sample08_UnderlyingMechanicsB
        {
            public class CalculateDistanceActivity : IActivityImplementation
            {
                public Task<SerializedPayloads> ExecuteAsync(IWorkflowActivityContext activityCtx)
                {
                    SerializedPayloads serializedInput = activityCtx.Input;
                    INamedPayloadContainer input = activityCtx.PayloadConverter.Deserialize<INamedPayloadContainer>(serializedInput);

                    double dist = CalculateDistance(input.GetValue<double>("X1"), input.GetValue<double>("Y1"),
                                                    input.GetValue<double>("X2"), input.GetValue<double>("Y2"));

                    SerializedPayloads serializedOutputAccumulator = new();
                    activityCtx.PayloadConverter.Serialize<double>(dist, serializedOutputAccumulator);

                    return Task.FromResult(serializedOutputAccumulator);
                }

                public double CalculateDistance(double x1, double y1, double x2, double y2)
                {
                    double dX = (x2 - x1);
                    double dY = (y2 - y1);
                    return Math.Sqrt(dX * dX + dY * dY);
                }
            }

            public static async void ConfigureActivityHost()
            {
                TemporalActivityWorker worker = new();

                worker.HostActivity<CalculateDistanceActivity>();

                await worker.StartAsync();

                Console.WriteLine($"{nameof(Sample08_UnderlyingMechanicsB)}: Worker started. Press enter to terminate.");
                Console.ReadLine();

                await worker.TerminateAsync();
            }
        }

        /// <summary>
        /// For the heartbeat API we choose a name that EXPLICITLY communicates that the eharbeat may or may not be delivbered immediately.
        /// This is consistent with the "request cancellation" pattern.
        /// </summary>
        private static class Sample09_Heartbeat
        {
            internal static bool IsPrime(int n, List<int> prevPrimes)
            {
                int sqrtN = (int) Math.Sqrt(n);

                int c = prevPrimes.Count;
                for (int i = 1; i < c; i++)
                {
                    int m = prevPrimes[i];
                    if (m > sqrtN)
                    {
                        break;
                    }

                    if (n % m == 0)
                    {
                        return false;
                    }
                }

                return true;
            }

            public static IList<int> CalculatePrimes(int max, IWorkflowActivityContext activityCtx)
            {
                List<int> primes = new();

                for (int n = 1; n <= max; n++)
                {
                    activityCtx.CancelToken.ThrowIfCancellationRequested();  // Check for cancellation

                    if (IsPrime(n, primes))
                    {
                        primes.Add(n);
                    }

                    activityCtx.RequestHeartbeatRecording(n);  // Record heartbeat
                }

                return primes;
            }

            public static async void ConfigureActivityHost()
            {
                TemporalActivityWorker worker = new();

                worker.HostActivity<int, IList<int>>("CalculatePrimeNumbers", CalculatePrimes);

                await worker.StartAsync();

                Console.WriteLine($"{nameof(Sample09_Heartbeat)}: Worker started. Press enter to terminate.");
                Console.ReadLine();

                await worker.TerminateAsync();
            }
        }

        /// <summary>
        /// This example shows how to execute an activity on the thread pool, and how to use another heartbeat overload.
        /// </summary>
        private static class Sample10_ParallelThreading
        {
            public static IList<int> CalculatePrimes(int max, IWorkflowActivityContext activityCtx)
            {
                List<int> primes = new();

                for (int n = 1; n <= max; n++)
                {
                    if (activityCtx.CancelToken.IsCancellationRequested)
                    {
                        // Cancellation is cooperative. Activity may not react to it. 
                        // In this sample, we DO react to it, but for the server the activity will appear completed,
                        // becasue we did not throw the OperationCanceledException like in the previous examples.

                        break;
                    }

                    if (Sample09_Heartbeat.IsPrime(n, primes))
                    {
                        primes.Add(n);
                    }

                    // In previous examples we passed arguments to `RequestHeartbeatRecording`.
                    // However, that is optional:
                    activityCtx.RequestHeartbeatRecording();
                }

                return primes;
            }

            public static async void ConfigureActivityHost()
            {
                TemporalActivityWorker worker = new();

                // Invoke the action on the thread pool (the action is complete when the task compeltes):
                worker.HostActivity<int, IList<int>>("CalculatePrimeNumbers", async (n, ctx) => await Task.Run(() => CalculatePrimes(n, ctx)));

                await worker.StartAsync();

                Console.WriteLine($"{nameof(Sample10_ParallelThreading)}: Worker started. Press enter to terminate.");
                Console.ReadLine();

                await worker.TerminateAsync();
            }
        }

        /// <summary>
        /// The `IWorkflowActivityContext` is an explicit. This has some advantages:
        ///   * Unit testing for activities is simpler (the context can be easily mocked).
        ///   * Supporting activity-functionality is an explicit API contract
        ///     (i.e. a library method that can heartbeat can demonstrate it by having IWorkflowActivityContext in the signature).
        ///   * If an application / implemetation wishes to promote the activity context to be implicitly available, it can choose
        ///     whatever mechanism it wants. The Temporal SDK does not want to be prescriptive on that regard.
        /// We demonstrate 2 approaches to making the activity context implicitly available: (a) Instance field and (b) Async local.
        /// This sample demonstrates the first of the two.
        /// </summary>
        private static class Sample11_WorkflowActivityContextA
        {
            internal class DoSomethingActivity
            {
                private readonly IWorkflowActivityContext _activityCtx;

                public DoSomethingActivity(IWorkflowActivityContext activityCtx)
                {
                    _activityCtx = activityCtx;
                }

                public async Task ExecuteAsync(string dataFile)
                {
                    object data = await ReadInputAsync(dataFile);
                    ProcessData(data);
                    await WriteOutputAsync(data, dataFile);
                }

                private async Task<object> ReadInputAsync(string dataFile)
                {
                    // Note the usage of `_activityCtx` in the method body:

                    List<byte> data = new();
                    byte[] buff = new byte[1024 * 20];
                    using FileStream fs = File.OpenRead(dataFile);

                    int readBytes = await fs.ReadAsync(buff, 0, buff.Length, _activityCtx.CancelToken);
                    while (readBytes > 0)
                    {
                        for (int i = 0; i < readBytes; i++)
                        {
                            data.Add(buff[i]);
                        }

                        _activityCtx.RequestHeartbeatRecording();
                        readBytes = await fs.ReadAsync(buff, 0, buff.Length, _activityCtx.CancelToken);
                    }

                    return data;
                }

                [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Sample")]
                private void ProcessData(object data)
                {
                    // . . .
                }

                [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Sample")]
                private Task WriteOutputAsync(object data, string dataFile)
                {
                    // . . .
                    return null;
                }
            }

            public static async void ConfigureActivityHost()
            {
                TemporalActivityWorker worker = new();

                // Invoke the action on the thread pool (the action is complete when the task compeltes):
                worker.HostActivity<string>("Do-Something", (datFl, ctx) => (new DoSomethingActivity(ctx)).ExecuteAsync(datFl));

                await worker.StartAsync();

                Console.WriteLine($"{nameof(Sample11_WorkflowActivityContextA)}: Worker started. Press enter to terminate.");
                Console.ReadLine();

                await worker.TerminateAsync();
            }
        }

        /// <summary>
        /// We demonstrate 2 approaches to making the activity context (`IWorkflowActivityContext`) implicitly available:
        /// (a) Instance field and (b) Async local.
        /// The previous example demonstrated the first.
        /// This sample demonstrates the second.
        /// The business logic shown here matched the previous example.
        /// </summary>
        private static class Sample12_WorkflowActivityContextB
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Sample")]
            internal static class WorkflowActivityContexts
            {
                private static class CtxContainers
                {
                    internal static readonly AsyncLocal<IWorkflowActivityContext> s_doSomething = new();
                    // More actvities...
                }

                public static IWorkflowActivityContext DoSomething
                {
                    get { return CtxContainers.s_doSomething.Value; }
                    set { CtxContainers.s_doSomething.Value = value; }
                }

                // More actvities...
            }

            public static async Task DoSomethingAsync(string dataFile, IWorkflowActivityContext activityCtx)
            {
                WorkflowActivityContexts.DoSomething = activityCtx;

                try
                {
                    object data = await ReadInputAsync(dataFile);
                    ProcessData(data);
                    await WriteOutputAsync(data, dataFile);
                }
                finally
                {
                    WorkflowActivityContexts.DoSomething = null;
                }
            }

            private static async Task<object> ReadInputAsync(string dataFile)
            {
                // Note the usage of `WorkflowActivityContexts.DoSomething` in the method body:

                List<byte> data = new();
                byte[] buff = new byte[1024 * 20];
                using FileStream fs = File.OpenRead(dataFile);

                int readBytes = await fs.ReadAsync(buff, 0, buff.Length, WorkflowActivityContexts.DoSomething.CancelToken);
                while (readBytes > 0)
                {
                    for (int i = 0; i < readBytes; i++)
                    {
                        data.Add(buff[i]);
                    }

                    WorkflowActivityContexts.DoSomething.RequestHeartbeatRecording();
                    readBytes = await fs.ReadAsync(buff, 0, buff.Length, WorkflowActivityContexts.DoSomething.CancelToken);
                }

                return data;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Sample")]
            private static void ProcessData(object data)
            {
                // . . .
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Sample")]
            private static Task WriteOutputAsync(object data, string dataFile)
            {
                // . . .
                return null;
            }

            public static async void ConfigureActivityHost()
            {
                TemporalActivityWorker worker = new();

                // Invoke the action on the thread pool (the action is complete when the task compeltes):
                worker.HostActivity<string>("Do-Something", DoSomethingAsync);

                await worker.StartAsync();

                Console.WriteLine($"{nameof(Sample12_WorkflowActivityContextB)}: Worker started. Press enter to terminate.");
                Console.ReadLine();

                await worker.TerminateAsync();
            }
        }
    }
}