using Temporal.Api.Common.V1;
using Temporal.Common;
using Temporal.Common.Payloads;
using Temporal.Serialization;
using Xunit;

namespace Temporal.Sdk.Common.Tests.Serialization
{
    // TODO: Add roundtrip tests for more types
    public class CompositePayloadConverterTest
    {
        [Fact]
        public void Void_Roundtrip()
        {
            CompositePayloadConverter converter = new();
            Payloads p = new();
            Assert.True(converter.TrySerialize(new IPayload.Void(), p));
            Assert.Empty(p.Payloads_);
            Assert.True(converter.TryDeserialize(p, out IPayload.Void item));
            Assert.NotNull(item);
        }

        [Fact]
        public void Null_Roundtrip()
        {
            CompositePayloadConverter converter = new();
            Payloads p = new();
            Assert.True(converter.TrySerialize<string>(null, p));
            Assert.Single(p.Payloads_);
            Assert.True(converter.TryDeserialize(p, out string item));
            Assert.Null(item);
        }

        [Fact]
        public void Unnamed_Roundtrip()
        {
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
    }
}