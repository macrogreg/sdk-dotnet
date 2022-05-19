using Temporal.Api.Common.V1;
using Temporal.Serialization;
using Temporal.TestUtil;
using Xunit;

namespace Temporal.Sdk.Common.Tests.Serialization
{
    public class ProtobufPayloadConverterTest
    {
        [Fact]
        public void IMessage_Roundtrip()
        {
            WorkflowExecution wf = new() { WorkflowId = "test", RunId = "tset" };
            ProtobufPayloadConverter instance = new();
            Payloads p = new();
            Assert.True(instance.TrySerialize(wf, p));
            Assert.True(instance.TryDeserialize(p, out WorkflowExecution actual));
            Assert.NotNull(actual);
            Assert.Equal(wf.WorkflowId, actual.WorkflowId);
            Assert.Equal(wf.RunId, actual.RunId);
        }

        [Fact]
        public void POCO_Roundtrip_Failure()
        {
            ProtobufPayloadConverter instance = new();
            Payloads p = new();
            Assert.False(instance.TrySerialize(SerializableClass.CreateDefault(), p));
            Assert.False(instance.TryDeserialize(p, out SerializableClass actual));
            Assert.Null(actual);
        }
    }
}