using System;
using System.Collections;
using System.Collections.Generic;

namespace Temporal.Common.Payloads
{
    public static partial class PayloadContainers
    {
        public static partial class Unnamed
        {
            /// <summary>
            /// <c>PayloadContainers.IUnnamed</c> implementation that contains no items.
            /// </summary>
            public struct Empty : PayloadContainers.IUnnamed, IPayload
            {
                public int Count
                {
                    get { return 0; }
                }

                public TVal GetValue<TVal>(int index)
                {
                    throw CreateNoSuchIndexException(index);
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    value = default(TVal);
                    return false;
                }

                public IEnumerable<PayloadContainers.UnnamedEntry> Values
                {
                    get
                    {
                        yield break;
                    }
                }

                public IEnumerator<PayloadContainers.UnnamedEntry> GetEnumerator()
                {
                    return new PayloadContainers.UnnamedEnumerator(this);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return this.GetEnumerator();
                }

                public PayloadContainers.UnnamedEntry this[int index]
                {
                    get
                    {
                        throw CreateNoSuchIndexException(index);
                    }
                }

                private static ArgumentOutOfRangeException CreateNoSuchIndexException(int index)
                {
                    return new ArgumentOutOfRangeException(nameof(index), $"This container is empty, but `{nameof(index)}={index}` was specified.");
                }
            }
        }
    }
}
