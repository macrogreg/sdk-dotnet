using System;
using System.Collections;
using System.Collections.Generic;
using Temporal.Util;

using SerializedPayloads = Temporal.Api.Common.V1.Payloads;

namespace Temporal.Serialization
{
    public sealed class CompositePayloadConverter : IPayloadConverter, IEnumerable<IPayloadConverter>, IDisposable
    {
        public static IList<IPayloadConverter> CreateDefaultConverters()
        {
            List<IPayloadConverter> converters = new(capacity: 6)
            {
                new VoidPayloadConverter(),
                new NullPayloadConverter(),
                new RawMemoryPayloadConverter(),
                new UnnamedContainerPayloadConverter(),
                new ProtobufJsonPayloadConverter(),
                new ProtobufPayloadConverter(),
                new NewtonsoftJsonPayloadConverter()
            };

            return converters;
        }

        private readonly List<IPayloadConverter> _converters;

        public CompositePayloadConverter()
            : this(CreateDefaultConverters())
        {
        }

        public CompositePayloadConverter(IEnumerable<IPayloadConverter> converters)
        {
            _converters = SerializationUtil.EnsureIsList(converters);

            for (int c = 0; c < _converters.Count; c++)
            {
                if (_converters[c] is DelegatingPayloadConverterBase delegatingConverter)
                {
                    delegatingConverter.InitDelegates(this);
                }
            }
        }

        public IReadOnlyList<IPayloadConverter> Converters { get { return _converters; } }

        public bool TrySerialize<T>(T item, SerializedPayloads serializedDataAccumulator)
        {
            Validate.NotNull(serializedDataAccumulator);

            for (int c = 0; c < _converters.Count; c++)
            {
                if (_converters[c] != null && _converters[c].TrySerialize(item, serializedDataAccumulator))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryDeserialize<T>(SerializedPayloads serializedData, out T deserializedItem)
        {
            Validate.NotNull(serializedData);

            // Only the first converter that CAN serialize got applied during serialization. So must traverse in the same order.
            // Given the small number of converter kinds this is more efficient than a lookup.
            for (int c = 0; c < _converters.Count; c++)
            {
                if (_converters[c] != null && _converters[c].TryDeserialize<T>(serializedData, out deserializedItem))
                {
                    return true;
                }
            }

            deserializedItem = default(T);
            return false;
        }

        public void Dispose()
        {
            while (_converters.Count > 0)
            {
                IPayloadConverter converter = _converters[_converters.Count - 1];
                _converters.RemoveAt(_converters.Count - 1);

                if (converter != null && converter is IDisposable disposableConverter)
                {
                    disposableConverter.Dispose();
                }
            }
        }

        IEnumerator<IPayloadConverter> IEnumerable<IPayloadConverter>.GetEnumerator()
        {
            return Converters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IPayloadConverter>) this).GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            return (obj != null)
                        && (obj is CompositePayloadConverter compositePayloadConverter)
                        && Equals(compositePayloadConverter);
        }

        /// <summary>
        /// Determines if this payload converter can be considered equal to the specified <c>other</c> converter.
        /// Among other things, converters are compared for equality when data held in a lazily deresialized container
        /// is re-serialized. In such cases, if the serializing and the deserializing converters are equal, data does not
        /// to re round-tripped.
        /// See <see cref="Temporal.Common.Payloads.PayloadContainers.Unnamed.SerializedDataBacked" /> and 
        /// <see cref="UnnamedContainerPayloadConverter" />.
        /// </summary>        
        public bool Equals(CompositePayloadConverter other)
        {
            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            if ((other == null) || !this.GetType().Equals(other.GetType()))
            {
                return false;
            }

            int countConverters = Converters.Count;
            if (countConverters != other.Converters.Count)
            {
                return false;
            }

            for (int c = 0; c < countConverters; c++)
            {
                IPayloadConverter c1 = Converters[c];
                IPayloadConverter c2 = other.Converters[c];

                if (!(Object.ReferenceEquals(c1, c2) || c1.Equals(c2)))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
