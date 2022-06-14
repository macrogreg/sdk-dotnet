using System;
using System.Threading.Tasks;
using Temporal.Util;
using Temporal.WorkflowClient;
using Temporal.Common;
using System.IO;
using Google.Protobuf;
using Temporal.Api.WorkflowService.V1;
using Temporal.Api.TaskQueue.V1;
using System.Collections.Generic;

namespace Temporal.Demos.AdHocScenarios
{
    internal class AdHocClientInvocations
    {
        public void Run()
        {
            Console.WriteLine();

            RunAsync().GetAwaiter().GetResult();

            Console.WriteLine();
            Console.WriteLine("Done. Press Enter.");
            Console.ReadLine();
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Creating a client...");
            ITemporalClient client = await TemporalClient.ConnectAsync(TemporalClientConfiguration.ForLocalHost());

            string demoWfId = "Payloads Demo Workflow / " + Format.AsReadablePreciseLocal(DateTimeOffset.Now);

            Console.WriteLine("Starting a workflow...");
            IWorkflowHandle workflow = await client.StartWorkflowAsync(demoWfId,
                                                                      "DemoWorkflowTypeName",
                                                                      "DemoTaskQueue");
            Console.WriteLine("Started. Info:");
            Console.WriteLine($"    Namespace:       {workflow.Namespace}");
            Console.WriteLine($"    WorkflowId:      {workflow.WorkflowId}");
            Console.WriteLine($"    IsBound:         {workflow.IsBound}");
            Console.WriteLine($"    WorkflowChainId: {workflow.WorkflowChainId}");

            Console.WriteLine("Sending signals with different payloads...");

            await workflow.SignalAsync("Signal-Void", Payload.Void);
            await workflow.SignalAsync<object>("Signal-Null-Object", signalArg: null);
            await workflow.SignalAsync<int[]>("Signal-Null-Array", signalArg: null);
            await workflow.SignalAsync<Stream>("Signal-Null-Stream", signalArg: null);
            await workflow.SignalAsync<AdHocClientInvocations>("Signal-Null-SomeClass", signalArg: null);

            await workflow.SignalAsync("Signal-Data-Array", Payload.Raw(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
            await workflow.SignalAsync("Signal-Data-Stream", new MemoryStream(new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }));

#if NETCOREAPP3_1_OR_GREATER
            await workflow.SignalAsync("Signal-Data-Span", Payload.Raw((new byte[] { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 }).AsSpan()));
            await workflow.SignalAsync("Signal-Data-Memory", (new byte[] { 31, 32, 33, 34, 35, 36, 37, 38, 39, 40 }).AsMemory());
#endif
            await workflow.SignalAsync("Signal-Data-ByteString", ByteString.CopyFrom((new byte[] { 41, 42, 43, 44, 45, 46, 47, 48, 49, 50 })));

            await workflow.SignalAsync("Signal-IMessage1", new StartWorkflowExecutionRequest()
            {
                Namespace = "IMessage1-NS",
                WorkflowId = "IMessage1-Workflow-Id",
                TaskQueue = new TaskQueue()
                {
                    Name = "IMessage1-Task-Queue",
                }
            });

            await workflow.SignalAsync("Signal-CompilerGenedClass", new
            {
                Name = "Captain Picard",
                List = new List<int>() { 100, 200, 300 },
                TaskQueue = new TaskQueue()
                {
                    Name = "IMessage1-Task-Queue",
                }
            });

            await workflow.SignalAsync("Signal-String", "I am a string");

            await workflow.SignalAsync("Signal-Int32", 1234);

            try
            {
                await workflow.SignalAsync("Signal-Array", new char[] { 'f', '0', 'o' });
                throw new Exception("We should never get there, becasue the above like is expected to throw.");
            }
            catch (InvalidOperationException)
            {
                // Expected exception;
            }

            await workflow.SignalAsync("Signal-ArrayAsOne", Payload.Unnamed<char[]>(new char[] { 'k', 'a', 'b', 'o', 'o' }));
            await workflow.SignalAsync("Signal-ArrayAsMultiple1", Payload.Unnamed<char>(new char[] { 'm', 'o', 'u' }));
            await workflow.SignalAsync("Signal-ArrayAsMultiple2", Payload.Unnamed<char>('b', '@', 'r'));

            await workflow.SignalAsync("Signal-ArrayAsMultiple3", Payload.Unnamed(1,
                                                                                  "two",
                                                                                  new List<double>() { 31, 3.2 },
                                                                                  this,
                                                                                  null,
                                                                                  Payload.Void,
                                                                                  Payload.Enumerable(new char[] { 'z', 'x', 'y' }),
                                                                                  Payload.Unnamed<char>('c', 'b', 'a'),
                                                                                  Payload.Unnamed<char[]>(new[] { 'q', 'w', 'E' }, new[] { '*', 'x' }),
                                                                                  Payload.Unnamed(new[] { 'r', 't', 'y' })));

            await workflow.SignalAsync("Signal-Enumerable1", Payload.Enumerable(new char[] { 'z', 'p', 's' }));

            await workflow.SignalAsync("Signal-Enumerable2", Payload.Enumerable(new List<object>() { '1', "second", new int[] { 31, 32 }, this }));

            Console.WriteLine("Completed sending signals with different payloads.");

            Console.WriteLine("Started automatic termination invoker...");

            _ = Task.Run(async () =>
            {
                TimeSpan delayTermination = TimeSpan.FromSeconds(2);
                Console.WriteLine($"Started automatic termination invoker with a delay of '{delayTermination}'.");

                await Task.Delay(delayTermination);
                Console.WriteLine($"Delay of {delayTermination} elapsed. Terminating workflow...");

                await workflow.TerminateAsync("Good-reason-for-termination",
                                              details: new { TimeStamp = DateTimeOffset.Now, Answer = 42, Bytes = ByteString.CopyFromUtf8("Hello World") });
                Console.WriteLine($"Workflow terminated.");
            });

            Console.WriteLine();
            Console.WriteLine("Waiting for workflow to conclude as a result of the termination...");
            Console.WriteLine();

            IWorkflowRunResult wfResult = await workflow.AwaitConclusionAsync();

            Console.WriteLine($"Workflow concluded. Status: {wfResult.Status}.");

            Console.WriteLine();
        }
    }
}
