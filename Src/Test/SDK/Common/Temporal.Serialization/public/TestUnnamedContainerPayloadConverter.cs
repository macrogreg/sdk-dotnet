using System;
using Temporal.Api.Common.V1;
using Temporal.Common.Payloads;
using Temporal.Serialization;
using Temporal.TestUtil;
using Xunit;

namespace Temporal.Sdk.Common.Tests.Serialization
{
    public class TestUnnamedContainerPayloadConverter
    {
        [Fact]
        [Trait("Category", "Common")]
        public void Test_UnnamedContainerPayloadConverter_TrySerialize_String()
        {
            UnnamedContainerPayloadConverter instance = new();
            Payloads p = new();
            Assert.False(instance.TrySerialize(String.Empty, p));
            Assert.Empty(p.Payloads_);
        }

        [Fact]
        [Trait("Category", "Common")]
        public void Test_UnnamedContainerPayloadConverter_TrySerialize_Null()
        {
            UnnamedContainerPayloadConverter instance = new();
            Payloads p = new();
            Assert.False(instance.TrySerialize<string>(null, p));
            Assert.Empty(p.Payloads_);
        }

        [Fact]
        [Trait("Category", "Common")]
        public void Test_UnnamedContainerPayloadConverter_TrySerialize_Unnamed_SerializedDataBacked()
        {
            UnnamedContainerPayloadConverter instance = new UnnamedContainerPayloadConverter();
            instance.InitDelegates(new[] { new NewtonsoftJsonPayloadConverter() });
            Payloads p = new();
            NewtonsoftJsonPayloadConverter converter = new();
            converter.Serialize(new SerializableClass { Name = "test", Value = 2 }, p);
            PayloadContainers.Unnamed.SerializedDataBacked data = new(p, converter);
            Assert.True(instance.TrySerialize(data, p));
            Assert.NotEmpty(p.Payloads_);
            Assert.True(instance.TryDeserialize(p, out PayloadContainers.Unnamed.SerializedDataBacked cl));
            Assert.NotNull(cl);
            SerializableClass deserializedData = cl.GetValue<SerializableClass>(0);
            Assert.Equal("test", deserializedData.Name);
            Assert.Equal(2, deserializedData.Value);
        }

        [Fact]
        [Trait("Category", "Common")]
        public void Test_UnnamedContainerPayloadConverter_TrySerialize_Unnamed_InstanceBacked()
        {
            void AssertDeserialization<T>(IPayloadConverter i, Payloads p)
            {
                Assert.True(i.TryDeserialize(p, out PayloadContainers.Unnamed.SerializedDataBacked cl));
                Assert.NotEmpty(cl);
                Assert.True(cl.TryGetValue(0, out string val));
                Assert.Equal("hello", val);
            }

            UnnamedContainerPayloadConverter instance = new UnnamedContainerPayloadConverter();
            instance.InitDelegates(new[] { new NewtonsoftJsonPayloadConverter() });
            Payloads p = new Payloads();
            PayloadContainers.Unnamed.InstanceBacked<string> data = new PayloadContainers.Unnamed.InstanceBacked<string>(new[] { "hello" });
            Assert.True(instance.TrySerialize(data, p));
            Assert.NotEmpty(p.Payloads_);
            Assert.False(instance.TryDeserialize(p, out PayloadContainers.Unnamed.InstanceBacked<string> _));
            AssertDeserialization<PayloadContainers.Unnamed.SerializedDataBacked>(instance, p);
            AssertDeserialization<PayloadContainers.IUnnamed>(instance, p);
        }

        [Fact]
        [Trait("Category", "Common")]
        public void Test_UnnamedContainerPayloadConverter_TrySerialize_Unnamed_Empty()
        {
            UnnamedContainerPayloadConverter instance = new UnnamedContainerPayloadConverter();
            instance.InitDelegates(new[] { new NewtonsoftJsonPayloadConverter() });
            Payloads p = new Payloads();
            PayloadContainers.Unnamed.Empty data = new PayloadContainers.Unnamed.Empty();
            Assert.True(instance.TrySerialize(data, p));
            Assert.Empty(p.Payloads_);
        }
    }
}