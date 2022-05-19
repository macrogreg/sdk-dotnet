using Temporal.Api.Enums.V1;
using Xunit;

namespace Temporal.Sdk.Common.Tests
{

    public class WorkflowExecutionStatusExtensionsTest
    {
        [Theory]
        [InlineData(WorkflowExecutionStatus.Unspecified, false)]
        [InlineData(WorkflowExecutionStatus.Running, false)]
        [InlineData(WorkflowExecutionStatus.Completed, true)]
        [InlineData(WorkflowExecutionStatus.Failed, true)]
        [InlineData(WorkflowExecutionStatus.Canceled, true)]
        [InlineData(WorkflowExecutionStatus.Terminated, true)]
        [InlineData(WorkflowExecutionStatus.ContinuedAsNew, true)]
        [InlineData(WorkflowExecutionStatus.TimedOut, true)]
        public void IsTerminal(WorkflowExecutionStatus status, bool expected)
        {
            Assert.Equal(expected, status.IsTerminal());
        }
    }
}