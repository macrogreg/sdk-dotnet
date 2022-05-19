using System;
using Temporal.Common.Payloads;
using Xunit;

namespace Temporal.Sdk.Common.Tests
{
    public class PayloadContainersUnnamedEmptyTest : PayloadContainersUnnamedTestBase
    {
        public PayloadContainersUnnamedEmptyTest()
            : base(new PayloadContainers.Unnamed.Empty(), 0)
        {
        }

        [Fact]
        public void Ctor()
        {
            PayloadContainers.Unnamed.Empty empty = new();
            Assert.Empty(empty);
            Assert.Throws<ArgumentOutOfRangeException>(() => empty.GetValue<object>(0));
            Assert.False(empty.TryGetValue<object>(0, out _));
        }
    }
}