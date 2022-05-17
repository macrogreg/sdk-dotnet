using Temporal.Api.Common.V1;
using Temporal.Common;
using Temporal.Common.Payloads;
using Temporal.Serialization;
using Temporal.TestUtil;
using Xunit;

namespace Temporal.Sdk.Common.Tests.Serialization
{
    // TODO: Add roundtrip tests for more types
    public class TestCompositePayloadConverter
    {
        [Fact]
        [Trait("Category", "Common")]
        public void Test_CompositePayloadConverter_Void_Roundtrip()
        {
            CompositePayloadConverter converter = new();
            Payloads p = new();
            Assert.True(converter.TrySerialize(new IPayload.Void(), p));
            Assert.Empty(p.Payloads_);
            Assert.True(converter.TryDeserialize(p, out IPayload.Void item));
            Assert.NotNull(item);
        }

        [Fact]
        [Trait("Category", "Common")]
        public void Test_CompositePayloadConverter_Null_Roundtrip()
        {
            CompositePayloadConverter converter = new();
            Payloads p = new();
            Assert.True(converter.TrySerialize<string>(null, p));
            Assert.Single(p.Payloads_);
            Assert.True(converter.TryDeserialize(p, out string item));
            Assert.Null(item);
        }

        [Fact]
        [Trait("Category", "Common")]
        public void Test_CompositePayloadConverter_Unnamed_Roundtrip()
        {
            UnnamedContainerPayloadConverter unnamed = new();
            unnamed.InitDelegates(new[] { new NewtonsoftJsonPayloadConverter() });
            CompositePayloadConverter instance = new(new IPayloadConverter[]
            {
                new VoidPayloadConverter(),
                new NullPayloadConverter(),
                new UnnamedContainerPayloadConverter(),
                new NewtonsoftJsonPayloadConverter(),
            });
            Payloads p = new();
            PayloadContainers.Unnamed.InstanceBacked<string> data = new(new[] { "hello" });
            Assert.True(instance.TrySerialize(data, p));
            Assert.NotEmpty(p.Payloads_);
            Assert.True(instance.TryDeserialize(p, out PayloadContainers.Unnamed.SerializedDataBacked cl));
            Assert.NotEmpty(cl);
            Assert.True(cl.TryGetValue(0, out string val));
            Assert.Equal("hello", val);
        }

        [Fact]
        [Trait("Category", "Common")]
        public void Test_CompositePayloadConverter_Catchall_Roundtrip()
        {
            CompositePayloadConverter converter = new();
            Payloads p = new();
            Assert.True(converter.TrySerialize(new IPayload.Void(), p));
            Assert.Empty(p.Payloads_);
            Assert.True(converter.TryDeserialize(p, out IPayload.Void item));
            Assert.NotNull(item);
        }
    }
}