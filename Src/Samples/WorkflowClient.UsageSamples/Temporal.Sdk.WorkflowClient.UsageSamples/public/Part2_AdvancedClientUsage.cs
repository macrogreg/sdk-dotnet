﻿using System;
using System.Threading.Tasks;
using Temporal.Api.Enums.V1;
using Temporal.Api.WorkflowService.V1;
using Temporal.Common;
using Temporal.Util;
using Temporal.WorkflowClient;
using Temporal.WorkflowClient.Errors;

namespace Temporal.Sdk.WorkflowClient.UsageSamples
{
    /// <summary>
    /// This Part demonstates the less common and more advanced usage scenarios within the Workflow Client SDK.
    /// </summary>
    /// <remarks>This class contains usage sample intended for education and API review. It may not actually execute
    /// successfully because IDs used in these samples may not actually exist on the backing Temporal server.</remarks>
    public class Part2_AdvancedClientUsage
    {
        public void Run()
        {
            Console.WriteLine($"\n{this.GetType().Name}.{nameof(Run)}(..) started.\n");

            RunAsync().GetAwaiter().GetResult();

            Console.WriteLine();
            Console.WriteLine($"\n{this.GetType().Name}.{nameof(Run)}(..) finished.\nPress Enter.");
            Console.ReadLine();
        }

        public async Task RunAsync()
        {
            Console.WriteLine($"\n{this.GetType().Name}.{nameof(RunAsync)}(..) started.\n");

            Console.WriteLine($"\nExecuting {nameof(GetWorkflowQueueInfoAsync)}...");
            await GetWorkflowQueueInfoAsync();

            Console.WriteLine($"\nExecuting {nameof(GetWorkflowStatusAsync)}...");
            await GetWorkflowStatusAsync();

            Console.WriteLine($"\nExecuting {nameof(GetWorkflowTypeNameAsync)}...");
            await GetWorkflowTypeNameAsync();

            Console.WriteLine($"\nExecuting {nameof(CheckWorkflowExistsAsync)}...");
            await CheckWorkflowExistsAsync();

            Console.WriteLine($"\nExecuting {nameof(WaitForWorkflowToFinishAsync)}...");
            await WaitForWorkflowToFinishAsync();

            Console.WriteLine($"\nExecuting {nameof(CancelWorkflowAndWaitWithProgressAsync)}...");
            await CancelWorkflowAndWaitWithProgressAsync();

            Console.WriteLine($"\nExecuting {nameof(StartNewWorkflowUsingHandleAsync)}...");
            await StartNewWorkflowUsingHandleAsync();

            Console.WriteLine($"\n{this.GetType().Name}.{nameof(RunAsync)}(..) finished.\n");
        }

        public async Task GetWorkflowQueueInfoAsync()
        {
            using IWorkflowHandle workflow = await ObtainWorkflowAsync();

            DescribeWorkflowExecutionResponse resDescrWf = await workflow.DescribeAsync();

            Console.WriteLine($"The *Task Queue Info* for the current workflow with id \"{workflow.WorkflowId}\":");
            Console.WriteLine($"    Queue name: \"{resDescrWf.ExecutionConfig.TaskQueue.Name}\".");
            Console.WriteLine($"    Queue kind: '{resDescrWf.ExecutionConfig.TaskQueue.Kind}'.");
        }

        public async Task GetWorkflowStatusAsync()
        {
            // There are convenience APIs for the most common info items obtainable from `DescribeAsync()`.
            // Another such item is Workflow Status.

            using IWorkflowHandle workflow = await ObtainWorkflowAsync();

            WorkflowExecutionStatus wfStatus = await workflow.GetStatusAsync();

            Console.WriteLine($"The *Workflow Status* for the current workflow with id \"{workflow.WorkflowId}\" is:");
            Console.WriteLine($"    '{wfStatus}' (={(int) wfStatus})");
        }

        public async Task GetWorkflowTypeNameAsync()
        {
            // There are convenience APIs for the most common info items obtainable from `DescribeAsync()`.
            // One such item is Workflow Type Name.

            using IWorkflowHandle workflow = await ObtainWorkflowAsync();

            string wfTypeName = await workflow.GetWorkflowTypeNameAsync();

            Console.WriteLine($"The *Workflow Type Name* for the current workflow with id \"{workflow.WorkflowId}\" is:");
            Console.WriteLine($"    \"{wfTypeName}\"");
        }

        public async Task CheckWorkflowExistsAsync()
        {
            // APIs that interact with a workflow throw a `WorkflowNotFoundException` if a workflow with the specified ID does not exist.
            // In a cases where it is only neccesary to know whether a particular workflow actually exists (i.e. invoking futher APIs
            // on that workflow is not required in the same code context) developers can use the `ExistsAsync(..)` method to avoid
            // using exceptions to control the program execution flow.

            // For example, consider a (simplified) code sub-section of a page controller for a shopping system page where users land
            // after they just logged on.

            string userName = "Some User-Name";
            string userId = "Some-User-Id";
            string shoppingCartWfId = $"ShoppingCart-Wf-|{userId}|";

            ITemporalClient client = ObtainClient();

            using IWorkflowHandle workflow = client.CreateWorkflowHandle(shoppingCartWfId);
            bool wfExists = await workflow.ExistsAsync();

            Console.WriteLine($"Welcome {userName}!");

            if (wfExists)
            {
                Console.WriteLine($"Looks like you were shopping before.");
                Console.WriteLine($"Click <a href='/page/ViewCart?id={shoppingCartWfId}'>here</a> to view your shopping cart.");
            }
            else
            {
                Console.WriteLine($"We are glad to see you in our store");
                Console.WriteLine($"Click <a href='/page/Catalogue'>here</a> to view our catalogue.");
            }

            Console.WriteLine($"Or click <a href='/page/Profile?id={userId}'>here</a> to configure your profile.");

            // Note that it is an anti-pattern to use `ExistsAsync(..)` as a qualifier for another API,
            // becasue the state on the server can change concurrently. For example the following is
            // NOT A RECOMMENDED way to use `ExistsAsync(..)`:

            if (!await workflow.ExistsAsync())
            {
                await client.StartWorkflowAsync(shoppingCartWfId, "Shopping-Cart-Wf", "Some-Task-Queue");
            }

            // Here, the even if the initial IF check shows that a workflow did not exist, it may be created concurrently by another
            // client, and the `StartWorkflowAsync(..)` call may still fail. The correct approach is:

            try
            {
                await client.StartWorkflowAsync(shoppingCartWfId, "Shopping-Cart-Wf", "Some-Task-Queue");
            }
            catch (WorkflowNotFoundException)
            {
                // Process exception...
            }

            // `ExistsAsync(..)` enables scenarios such as the first example in this method, where LOCAL logic depends on the
            // exisatance of a remote workflow.
        }

        public async Task WaitForWorkflowToFinishAsync()
        {
            // In some scenarios users want to wait for a workflow to conclude, but they do not care about the actual result value.
            // One way of achieving that was shown in a previous sample:
            // Use `IPayload.Void` as the type of the result value.
            // (In this case, an exception will be thrown if the workflow concludes with any status other than `Completed`.)
            {
                using IWorkflowHandle workflow = await ObtainWorkflowAsync();

                await workflow.GetResultAsync<IPayload.Void>();
            }

            // The use of `IPayload.Void` may not look nice,
            // and there are scenarios where users may want to wait for a workflow to conclude with any Status, without using
            // exceptions  to control program execution flow. The `AwaitConclusionAsync(..)` method allows doing that:
            {
                using IWorkflowHandle workflow = await ObtainWorkflowAsync();

                await workflow.AwaitConclusionAsync();
            }

            // The `AwaitConclusionAsync(..)` method returns a property bag that contains
            // useful information about the conclusion of the workflow:
            {
                using IWorkflowHandle workflow = await ObtainWorkflowAsync();

                IWorkflowRunResult wfResultInfo = await workflow.AwaitConclusionAsync();

                Console.WriteLine($"The *Workflow Run Result Info* for the current workflow with id \"{workflow.WorkflowId}\":");
                Console.WriteLine($"    Namespace:    \"{wfResultInfo.Namespace}\".");
                Console.WriteLine($"    WorkflowId:   \"{wfResultInfo.WorkflowId}\".");
                Console.WriteLine($"    Failure:      {(wfResultInfo.Failure == null ? "None" : wfResultInfo.Failure.TypeAndMessage())}.");

                // The actual result value can also be obtained from the result-info property bag. E.g.:

                int wfResultValue = wfResultInfo.GetValue<int>();
                Console.WriteLine($"    Result value: {wfResultValue}.");
            }
        }

        public async Task CancelWorkflowAndWaitWithProgressAsync()
        {
            // This is another sample scenario where the user needs to work with the conclusion status of a workflow,
            // without the need to access the actual result value or to deal with exceptions to control program execution flow.

            // Here, we request that a workflow with some specific ID is cancelled.
            // After requstin the cancellation, we wait for the workflow to respect the request.
            // During the wait, we display progress at regular intervals.
            // If the request is not respeced after a certain time, we eventually terminate the workflow.

            const string WorkflowIdToCancel = "Some-Workflow-Id";
            TimeSpan progressUpdatePeriod = TimeSpan.FromSeconds(5);
            TimeSpan maxWaitForCancellationDuration = TimeSpan.FromSeconds(30);

            ITemporalClient client = ObtainClient();
            IWorkflowHandle workflow = client.CreateWorkflowHandle(WorkflowIdToCancel);

            // Request cancellation and remember the time:
            await workflow.RequestCancellationAsync();
            DateTime cancellationReqTime = DateTime.Now;

            // This task represents the conclusion of the workflow with any status:
            Task<IWorkflowRunResult> workflowConclusion = workflow.AwaitConclusionAsync();

            // This task represents the conclusion of the display update period:
            Task displayDelayConclusion = Task.Delay(progressUpdatePeriod);

            // Await until either the workflow or the waiting period finishes:
            await Task.WhenAny(workflowConclusion, displayDelayConclusion);
            TimeSpan elapsed = DateTime.Now - cancellationReqTime;

            // If workflow is still running, display progress and keep waiting:
            while (!workflowConclusion.IsCompleted)
            {
                Console.WriteLine($"Still waiting for the workflow to react to the cancellation request."
                                + $" Time elapsed: {elapsed}.");

                if (elapsed > maxWaitForCancellationDuration)
                {
                    Console.WriteLine($"Elapsed time exceeded {nameof(maxWaitForCancellationDuration)} (={maxWaitForCancellationDuration})."
                                    + $" Terminating the workflow.");

                    await workflow.TerminateAsync("Max wait-for-cancellation duration was exceeded.");
                }

                displayDelayConclusion = Task.Delay(progressUpdatePeriod);
                await Task.WhenAny(workflowConclusion, displayDelayConclusion);
                elapsed = DateTime.Now - cancellationReqTime;
            }

            // Get the result handle and display the final status:
            IWorkflowRunResult workflowResult = workflowConclusion.Result;
            Console.WriteLine($"Workflow finished. Terminal status: {workflowResult.Status}.");
        }

        public async Task StartNewWorkflowUsingHandleAsync()
        {
            // In some cases, a user may be working with code that does not have access to the `ITemporalClient` instance,
            // but only to a `IWorkflowHandle` instance. Such code may need to start a workflow just using the available handle.

            ITemporalClient client = ObtainClient();
            IWorkflowHandle workflow = client.CreateWorkflowHandle("Some-Workflow-Id");

            await SomeApiWithFixedSignature(workflow);

        }

        internal async Task SomeApiWithFixedSignature(IWorkflowHandle workflow)
        {
            Validate.NotNull(workflow);

            int wfInput = ComputeWorkflowInput();
            await workflow.StartAsync("Sample-Workflow-Type-Name", "Sample-Task-Queue", wfInput);

            UseWorkflow(workflow);
        }

        private int ComputeWorkflowInput()
        {
            return 42;
        }

        #region --- Helpers ---

        private void UseWorkflow(IWorkflowHandle workflow)
        {
            Validate.NotNull(workflow);
            // Do stuff with `workflow`.
        }

        private ITemporalClient ObtainClient()
        {
            TemporalClientConfiguration clinetConfig = TemporalClientConfiguration.ForLocalHost();
            return new TemporalClient(clinetConfig);
        }

        private string CreateUniqueWorkflowId()
        {
            return $"Sample-Workflow-Id-{Guid.NewGuid().ToString("D")}";
        }

        private async Task<IWorkflowHandle> ObtainWorkflowAsync()
        {
            ITemporalClient client = ObtainClient();

            string workflowId = CreateUniqueWorkflowId();
            IWorkflowHandle workflow = await client.StartWorkflowAsync(workflowId,
                                                                       "Sample-Workflow-Type-Name",
                                                                       "Sample-Task-Queue");

            return workflow;
        }

        #endregion --- Helpers ---
    }
}
