using System;
using Temporal.Common.Payloads;
using Xunit;

namespace Temporal.Sdk.Common.Tests
{
    public abstract class PayloadContainersUnnamedTestBase
    {
        private readonly PayloadContainers.IUnnamed _instance;
        private readonly int _length;

        protected PayloadContainersUnnamedTestBase(PayloadContainers.IUnnamed instance, int length)
        {
            _instance = instance;
            _length = length;
        }

        [Fact]
        public void GetValue_NegativeIndex()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _instance.GetValue<object>(-1));
        }

        [Fact]
        public void GetValue_IndexOutOfBounds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _instance.GetValue<object>(_instance.Count + 1));
        }

        [Fact]
        public void TryGetValue_NegativeIndex()
        {
            Assert.False(_instance.TryGetValue<object>(-1, out _));
        }

        [Fact]
        public void TryGetValue_IndexOutOfBounds()
        {
            Assert.False(_instance.TryGetValue<object>(_instance.Count + 1, out _));
        }

        [Fact]
        public void Length_IsExpectedValue()
        {
            Assert.Equal(_length, _instance.Count);
        }
    }
}