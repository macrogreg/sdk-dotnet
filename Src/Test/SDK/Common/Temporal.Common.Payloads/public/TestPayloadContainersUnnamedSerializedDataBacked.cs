using Temporal.Api.Common.V1;
using Temporal.Common.Payloads;
using Temporal.Serialization;
using Temporal.TestUtil;
using Xunit;

namespace Temporal.Sdk.Common.Tests
{
    public class TestPayloadContainersUnnamedSerializedDataBacked : AbstractUnnamedTest
    {
        public TestPayloadContainersUnnamedSerializedDataBacked()
            : base(
                new PayloadContainers.Unnamed.SerializedDataBacked(CreateDefaultPayloads(), new NewtonsoftJsonPayloadConverter()),
                1)
        {
        }

        [Fact]
        [Trait("Category", "Common")]
        public void Test_Payload_Containers_Unnamed_Instance_Backed_Type_Conversion()
        {
            SerializableClass defaultValue = SerializableClass.Default;
            PayloadContainers.Unnamed.SerializedDataBacked instance =
                new PayloadContainers.Unnamed.SerializedDataBacked(CreateDefaultPayloads(), new NewtonsoftJsonPayloadConverter());
            SerializableClass value = instance.GetValue<SerializableClass>(0);
            AssertWellFormed(defaultValue, value);
            Assert.True(instance.TryGetValue(0, out value));
            AssertWellFormed(defaultValue, value);
        }

        private static void AssertWellFormed(SerializableClass expected, SerializableClass actual)
        {
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Value, actual.Value);
        }

        private static Payloads CreateDefaultPayloads()
        {
            Payloads payload = new Payloads();
            NewtonsoftJsonPayloadConverter converter = new NewtonsoftJsonPayloadConverter();
            converter.Serialize(SerializableClass.Default, payload);
            return payload;
        }
    }
}