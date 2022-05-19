using System;
using Temporal.Api.Common.V1;
using Temporal.Common.Payloads;
using Temporal.Serialization;
using Temporal.TestUtil;
using Xunit;

namespace Temporal.Sdk.Common.Tests.Serialization
{
    public class UnnamedContainerPayloadConverterTest
    {
        [Fact]
        public void TrySerialize_String()
        {
            UnnamedContainerPayloadConverter instance = new();
            Payloads p = new();
            Assert.False(instance.TrySerialize(String.Empty, p));
            Assert.Empty(p.Payloads_);
        }

        [Fact]
        public void TrySerialize_Null()
        {
            UnnamedContainerPayloadConverter instance = new();
            Payloads p = new();
            Assert.False(instance.TrySerialize<string>(null, p));
            Assert.Empty(p.Payloads_);
        }

        [Fact]
        public void TrySerialize_Unnamed_SerializedDataBacked()
        {
            UnnamedContainerPayloadConverter instance = new();
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
        public void TrySerialize_Unnamed_InstanceBacked()
        {
            static void AssertDeserialization<T>(IPayloadConverter i, Payloads p)
            {
                Assert.True(i.TryDeserialize(p, out PayloadContainers.Unnamed.SerializedDataBacked cl));
                Assert.NotEmpty(cl);
                Assert.True(cl.TryGetValue(0, out string val));
                Assert.Equal("hello", val);
            }

            UnnamedContainerPayloadConverter instance = new();
            instance.InitDelegates(new[] { new NewtonsoftJsonPayloadConverter() });
            Payloads p = new();
            PayloadContainers.Unnamed.InstanceBacked<string> data = new(new[] { "hello" });
            Assert.True(instance.TrySerialize(data, p));
            Assert.NotEmpty(p.Payloads_);
            Assert.False(instance.TryDeserialize(p, out PayloadContainers.Unnamed.InstanceBacked<string> _));
            AssertDeserialization<PayloadContainers.Unnamed.SerializedDataBacked>(instance, p);
            AssertDeserialization<PayloadContainers.IUnnamed>(instance, p);
        }

        [Fact]
        public void TrySerialize_Unnamed_Empty()
        {
            UnnamedContainerPayloadConverter instance = new();
            instance.InitDelegates(new[] { new NewtonsoftJsonPayloadConverter() });
            Payloads p = new();
            PayloadContainers.Unnamed.Empty data = new();
            Assert.True(instance.TrySerialize(data, p));
            Assert.Empty(p.Payloads_);
        }
    }
}