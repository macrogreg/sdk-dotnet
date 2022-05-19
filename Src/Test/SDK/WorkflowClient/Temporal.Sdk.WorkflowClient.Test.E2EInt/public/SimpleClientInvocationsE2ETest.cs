using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

using Temporal.Api.Enums.V1;
using Temporal.Common;
using Temporal.TestUtil;
using Temporal.Util;
using Temporal.WorkflowClient;
using Temporal.WorkflowClient.Errors;

namespace Temporal.Sdk.WorkflowClient.Test.E2EInt
{
    [Collection("SequentialTextExecution")]
    public class SimpleClientInvocationsE2ETest : IntegrationTestBase
    {
        public SimpleClientInvocationsE2ETest(ITestOutputHelper tstout)
            : base(tstout)
        {
        }

        [Fact]
        public async Task E2EScenarioAsync()
        {
            TstoutPrefixLine("Creating a client over a plain (unsecured) channel...");

            ITemporalClient client = await TemporalClient.ConnectAsync(TemporalClientConfiguration.ForLocalHost());

            string demoWfId = TestCaseContextMonikers.ForWorkflowId(this);
            string demoTastQueue = TestCaseContextMonikers.ForTaskQueue(this);

            TstoutWriteLine("Starting a workflow...");
            IWorkflowHandle workflow = await client.StartWorkflowAsync(demoWfId,
                                                                      "DemoWorkflowTypeName",
                                                                      demoTastQueue);
            TstoutWriteLine("Started. Info:");
            TstoutWriteLine($"    Namespace:       {workflow.Namespace}");
            TstoutWriteLine($"    WorkflowId:      {workflow.WorkflowId}");
            TstoutWriteLine($"    IsBound:         {workflow.IsBound}");
            TstoutWriteLine($"    WorkflowChainId: {workflow.WorkflowChainId}");

            TstoutWriteLine();
            TstoutWriteLine("Attempting to start a workflow with the same WorkflowId...");
            TstoutWriteLine();

            try
            {
                await client.StartWorkflowAsync(demoWfId, "DemoWorkflowTypeName", demoTastQueue);

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (WorkflowAlreadyExistsException waeEx)
            {
                TstoutWriteLine("Received expected exception.");
                TstoutWriteLine(waeEx.TypeAndMessage());

                Exception innerEx = waeEx.InnerException;
                while (innerEx != null)
                {
                    TstoutWriteLine("\n  Inner --> " + innerEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            TstoutWriteLine();
            TstoutWriteLine("Creating a handle to the existing workflow...");
            TstoutWriteLine();

            IWorkflowHandle workflow2 = client.CreateWorkflowHandle(demoWfId);

            TstoutWriteLine("Created. Info:");
            TstoutWriteLine($"    Namespace:       {workflow2.Namespace}");
            TstoutWriteLine($"    WorkflowId:      {workflow2.WorkflowId}");
            TstoutWriteLine($"    IsBound:         {workflow2.IsBound}");

            try
            {
                TstoutWriteLine($"    WorkflowChainId: {workflow2.WorkflowChainId}");

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (InvalidOperationException invOpEx)
            {
                TstoutWriteLine($"    Expected exception while getting {nameof(workflow2.WorkflowChainId)}:");
                TstoutWriteLine($"    --> {invOpEx.TypeAndMessage()}");

                Exception innerEx = invOpEx.InnerException;
                while (innerEx != null)
                {
                    TstoutWriteLine("\n      Inner --> " + invOpEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            TstoutWriteLine();
            TstoutWriteLine("Obtaining Workflow Type Name...");
            TstoutWriteLine();

            string workflowTypeName = await workflow2.GetWorkflowTypeNameAsync();

            TstoutWriteLine($"Obtained. {nameof(workflowTypeName)}={workflowTypeName.QuoteOrNull()}.");
            TstoutWriteLine("Updated handle info:");
            TstoutWriteLine($"    IsBound:         {workflow2.IsBound}");
            TstoutWriteLine($"    WorkflowChainId: {workflow2.WorkflowChainId}");

            TstoutWriteLine();
            TstoutWriteLine("Sending signal to a workflow...");
            TstoutWriteLine();

            await workflow.SignalAsync("Some-Signal-01", "Some-Signal-Argument");

            TstoutWriteLine("Signal sent. Look for it in the workflow history.");

            TstoutWriteLine();
            TstoutWriteLine("Sending query to a workflow...");

            try
            {
                TimeSpan delayQueryCancel = TimeSpan.FromSeconds(2);
                TstoutWriteLine($"The workflow was not designed for queries,"
                                + $" so we will cancel the wait for query completion after '{delayQueryCancel}'.");
                TstoutWriteLine();

                using CancellationTokenSource query01CancelControl = new(delayQueryCancel);
                object resQuery01 = await workflow.QueryAsync<object, string>("Some-Query-01",
                                                                              "Some-Query-Argument",
                                                                              cancelToken: query01CancelControl.Token);

                TstoutWriteLine("Query sent. Look for it in the workflow history.");
                TstoutWriteLine($"Query result: |{Format.QuoteIfString(resQuery01)}|.");
            }
            catch (OperationCanceledException opCncldEx)
            {
                TstoutWriteLine("Received expected exception.");
                TstoutWriteLine(opCncldEx.TypeAndMessage());

                Exception innerEx = opCncldEx.InnerException;
                while (innerEx != null)
                {
                    TstoutWriteLine("\n  Inner --> " + innerEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            TstoutWriteLine();
            TstoutWriteLine("Sending cancellation request to a workflow...");
            TstoutWriteLine();

            await workflow.RequestCancellationAsync();

            TstoutWriteLine("Cancellation requested. However, it the workflow was not designed to honor it, so it will remain ignored. Look for the request in the workflow history.");
            TstoutWriteLine("Look for the request in the workflow history.");

            TstoutWriteLine();
            TstoutWriteLine("Getting LatestRun of the workflow...");

            IWorkflowRunHandle latestRun = await workflow.GetLatestRunAsync();

            TstoutWriteLine("Got the latest run. Calling Getting LatestRun of the workflow...");
            TstoutWriteLine($"    Namespace:       {latestRun.Namespace}");
            TstoutWriteLine($"    WorkflowId:      {latestRun.WorkflowId}");
            TstoutWriteLine($"    WorkflowRunId:   {latestRun.WorkflowRunId}");

            TstoutWriteLine();
            TstoutWriteLine("Sending signal to a run...");
            TstoutWriteLine();

            await latestRun.SignalAsync("Some-Signal-02", Payload.Unnamed(42, DateTimeOffset.Now, "Last-Signal-Argument-Value"));

            TstoutWriteLine("Signal sent to run. Look for it in the run history.");

            TstoutWriteLine();
            TstoutWriteLine("Creating an independent workflow run handle...");

            IWorkflowRunHandle run2 = client.CreateWorkflowRunHandle(latestRun.WorkflowId, latestRun.WorkflowRunId);

            TstoutWriteLine("Independent run handle created.");
            TstoutWriteLine($"    Namespace:       {latestRun.Namespace}");
            TstoutWriteLine($"    WorkflowId:      {latestRun.WorkflowId}");
            TstoutWriteLine($"    WorkflowRunId:   {latestRun.WorkflowRunId}");

            TstoutWriteLine();
            TstoutWriteLine("Obtaining owner workflow of independent run handle...");
            TstoutWriteLine();

            IWorkflowHandle ownerWorkflow = await run2.GetOwnerWorkflowAsync();

            TstoutWriteLine("Owner workflow obtained.");
            TstoutWriteLine($"    Namespace:       {ownerWorkflow.Namespace}");
            TstoutWriteLine($"    WorkflowId:      {ownerWorkflow.WorkflowId}");
            TstoutWriteLine($"    IsBound:         {ownerWorkflow.IsBound}");
            TstoutWriteLine($"    WorkflowChainId: {ownerWorkflow.WorkflowChainId}");

            TstoutWriteLine();
            TstoutWriteLine($"Owner-workflow-handle and the Original-workflow refer to the same chain (TRUE expected):"
                            + $" {ownerWorkflow.WorkflowChainId.Equals(workflow.WorkflowChainId)}");

            TstoutWriteLine($"Owner-workflow-handle and the Original-workflow are the same instance (FALSE expected):"
                            + $" {Object.ReferenceEquals(ownerWorkflow, workflow)}");

            TstoutWriteLine();
            TstoutWriteLine("Sending four signals to a run using independent handle...");
            TstoutWriteLine();

            object[] signal3Inputvalues = new object[] { 42, new { Custom = "Foo", Datatype = 18 } };
            await latestRun.SignalAsync("Some-Signal-03a", Payload.Unnamed(signal3Inputvalues));
            await latestRun.SignalAsync("Some-Signal-03b", Payload.Unnamed<object[]>(signal3Inputvalues));
            await latestRun.SignalAsync("Some-Signal-03c", Payload.Unnamed(42, 43, 44));
            await latestRun.SignalAsync("Some-Signal-03d", Payload.Unnamed<int[]>(new[] { 42, 43, 44 }));

            TstoutWriteLine("Signasl sent via intependent run handle. Look for them in the run history.");

            _ = Task.Run(async () =>
            {
                TimeSpan delayTermination = TimeSpan.FromSeconds(2);
                TstoutWriteLine($"Started automatic termination invoker with a delay of '{delayTermination}'.");

                await Task.Delay(delayTermination);
                TstoutWriteLine($"Delay of {delayTermination} elapsed. Terminating workflow...");

                await workflow.TerminateAsync("Good-reason-for-termination", details: DateTimeOffset.Now);
                TstoutWriteLine($"Workflow terminated.");
            });

            TstoutWriteLine();
            TstoutWriteLine("Waiting for result...");
            TstoutWriteLine();

            try
            {
                await workflow.GetResultAsync<object>();

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (WorkflowConcludedAbnormallyException wcaEx) when (wcaEx.ConclusionStatus == WorkflowExecutionStatus.Terminated)
            {
                TstoutWriteLine("Received expected exception.");
                TstoutWriteLine(wcaEx.TypeAndMessage());

                Exception innerEx = wcaEx.InnerException;
                while (innerEx != null)
                {
                    TstoutWriteLine("\n  Inner --> " + innerEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            TstoutWriteLine();
            TstoutWriteLine("Creating a handle to a non-existing workflow...");

            IWorkflowHandle workflow3 = client.CreateWorkflowHandle("Non-Existing-Workflow-Id");

            TstoutWriteLine("Created. Info:");
            TstoutWriteLine($"    Namespace:       {workflow3.Namespace}");
            TstoutWriteLine($"    WorkflowId:      {workflow3.WorkflowId}");
            TstoutWriteLine($"    IsBound:         {workflow3.IsBound}");

            TstoutWriteLine();
            TstoutWriteLine("Verifying existence...");
            TstoutWriteLine();

            bool wfExists = await workflow3.ExistsAsync();

            TstoutWriteLine($"Verified. {nameof(wfExists)}={wfExists}.");
            TstoutWriteLine("Updated handle info:");
            TstoutWriteLine($"    IsBound:         {workflow3.IsBound}");

            try
            {
                TstoutWriteLine($"    WorkflowChainId: {workflow3.WorkflowChainId}");

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (InvalidOperationException invOpEx)
            {
                TstoutWriteLine($"    Expected exception while getting {nameof(workflow3.WorkflowChainId)}:");
                TstoutWriteLine($"    --> {invOpEx.TypeAndMessage()}");

                Exception innerEx = invOpEx.InnerException;
                while (innerEx != null)
                {
                    TstoutWriteLine("\n      Inner --> " + invOpEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            TstoutWriteLine();
            TstoutWriteLine("Sending signal to a non-existing workflow...");
            TstoutWriteLine();

            try
            {
                await workflow3.SignalAsync("Some-Signal-04");

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (WorkflowNotFoundException wnfEx)
            {
                TstoutWriteLine("Received expected exception.");
                TstoutWriteLine(wnfEx.TypeAndMessage());

                Exception innerEx = wnfEx.InnerException;
                while (innerEx != null)
                {
                    TstoutWriteLine("\n  Inner --> " + innerEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            TstoutWriteLine();
            TstoutWriteLine("Waiting for result of a non-existing workflow...");
            TstoutWriteLine();

            try
            {
                await workflow3.GetResultAsync<object>();

                throw new Exception("ERROR. We should never get here, because the above code is expected to throw.");
            }
            catch (WorkflowNotFoundException wnfEx)
            {
                TstoutWriteLine("Received expected exception.");
                TstoutWriteLine(wnfEx.TypeAndMessage());

                Exception innerEx = wnfEx.InnerException;
                while (innerEx != null)
                {
                    TstoutWriteLine("\n  Inner --> " + innerEx.TypeAndMessage());
                    innerEx = innerEx.InnerException;
                }
            }

            TstoutWriteLine();
            TstoutWriteLine($"E2E Scenario complete ({nameof(SimpleClientInvocationsE2ETest)}).");
            TstoutWriteLine();
        }
    }
}