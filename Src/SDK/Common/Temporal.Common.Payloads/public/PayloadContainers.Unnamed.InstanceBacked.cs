using System;
using System.Collections;
using System.Collections.Generic;
using Temporal.Util;

namespace Temporal.Common.Payloads
{
    public static partial class PayloadContainers
    {
        /// <summary>
        /// <c>PayloadContainers.IUnnamed</c> implementation backed by actual values (rather than a raw payload).
        /// </summary>
        public static partial class Unnamed
        {
            public struct InstanceBacked<T> : PayloadContainers.IUnnamed, IPayload
            {
                private readonly IReadOnlyList<T> _values;

                public InstanceBacked(IReadOnlyList<T> values)
                {
                    Validate.NotNull(values);

                    _values = values;
                }

                public int Count
                {
                    get { return _values.Count; }
                }

                public TVal GetValue<TVal>(int index)
                {
                    if (index >= 0 && index < Count)
                    {
                        return _values[index].Cast<T, TVal>();
                    }

                    throw PayloadContainers.Util.CreateNoSuchIndexException(index, Count, this);
                }

                public bool TryGetValue<TVal>(int index, out TVal value)
                {
                    if (index >= 0 && index < Count)
                    {
                        return _values[index].TryCast<T, TVal>(out value);
                    }

                    value = default(TVal);
                    return false;
                }

                public Type GetValueType(int index)
                {
                    if (index >= 0 && index < Count)
                    {
                        return _values[index].TypeOf();
                    }

                    throw PayloadContainers.Util.CreateNoSuchIndexException(index, Count, this);
                }

                public IEnumerable<PayloadContainers.UnnamedEntry> Values
                {
                    get
                    {
                        for (int i = 0; i < Count; i++)
                        {
                            yield return new PayloadContainers.UnnamedEntry(i, this);
                        }
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
                        if (index >= 0 && index < Count)
                        {
                            return new PayloadContainers.UnnamedEntry(index, this);
                        }

                        throw PayloadContainers.Util.CreateNoSuchIndexException(index, Count, this);
                    }
                }
            }
        }
    }
}
