using System;
using Temporal.Util;
using Google.Protobuf;
using Temporal.Api.Common.V1;
using Temporal.Common.Payloads;
using System.Threading;
using System.Threading.Tasks;

namespace Temporal.Serialization
{
    public static class PayloadConverter
    {
        public const string PayloadMetadataEncodingKey = "encoding";

        /// <summary>
        /// Utility method used by several <c>IPayloadConverter</c> implementations.
        /// If <c>valueBytes</c> is NOT null, return the <c>ByteString</c>-object referenced by <c>valueBytes</c>.
        /// Otherwise, convert the specified <c>value</c> into a <c>ByteString</c>, store it into the location referenced
        /// by <c>valueBytes</c>, and return the resulting object.
        /// </summary>        
        public static ByteString GetOrCreateBytes(string value, ref ByteString valueBytes)
        {
            ByteString bytes = valueBytes;
            if (bytes == null)
            {
                bytes = ByteString.CopyFromUtf8(value);
                bytes = Concurrent.TrySetOrGetValue(ref valueBytes, bytes);
            }

            return bytes;
        }

        public static bool IsNormalEnumerable<T>(T item)
        {
            return IsNormalEnumerable<T>(item, out _);
        }

        public static bool IsNormalEnumerable<T>(T item, out System.Collections.IEnumerable enumerableItem)
        {
            enumerableItem = null;

            if (item != null)
            {
                if (item is PayloadContainers.IUnnamed                  // Wrapped in Container => False
                        || item is PayloadContainers.Enumerable         // Wrapped in Enumerable Container => False
                        || item is Newtonsoft.Json.Linq.JObject         // Json JObject => False
                        || item is string)                              // String => False
                {
                    return false;
                }

                // Return wether Is IEnumerable:
                if (item is System.Collections.IEnumerable emItem)
                {
                    enumerableItem = emItem;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return IsNormalEnumerable<T>();
        }

        public static bool IsNormalEnumerable<T>()
        {
            Type checkedType = typeof(T);

            // If Wrapped in Container => False:
            if (typeof(PayloadContainers.IUnnamed).IsAssignableFrom(checkedType)            // Wrapped in IUnnamed Container => False
                    || typeof(PayloadContainers.Enumerable).IsAssignableFrom(checkedType)   // Wrapped in Enumerable Container => False
                    || typeof(Newtonsoft.Json.Linq.JObject).IsAssignableFrom(checkedType)   // Json JObject => False
                    || typeof(string).IsAssignableFrom(checkedType))                        // String => False
            {
                return false;
            }

            // Return wether Is IEnumerable:
            return typeof(System.Collections.IEnumerable).IsAssignableFrom(checkedType);
        }

        /// <summary>
        /// Convenience wrapper method for the corresponding <see cref="IPayloadConverter" />-API.
        /// (See <see cref="IPayloadConverter.TryDeserialize{T}(Payloads, out T)" />.)
        /// Calls the respective <c>IPayloadConverter</c>-method, and in case of a failure (<c>false</c> return value),
        /// generates exceptions with detailed diagnostic messages.
        /// </summary>
        public static T Deserialize<T>(this IPayloadConverter converter, Payloads serializedData)
        {
            Validate.NotNull(converter);
            Validate.NotNull(serializedData);

            try
            {
                return DeserializeOrThrow<T>(converter, serializedData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot {nameof(Deserialize)} the specified {nameof(serializedData)};"
                                                  + $" type of the specified {nameof(converter)}: \"{converter.GetType().FullName}\";"
                                                  + $" static type of the de-serialization target: \"{typeof(T).FullName}\";"
                                                  + $" number of specified {nameof(Temporal.Api.Common.V1.Payload)}-entries:"
                                                  + $" {Format.SpellIfNull(serializedData.Payloads_?.Count)}.",
                                                    ex);
            }
        }

        public static async Task<T> DeserializeAsync<T>(this IPayloadConverter converter,
                                                        IPayloadCodec codec,
                                                        Payloads serializedData,
                                                        CancellationToken cancelToken)
        {
            if (codec != null && serializedData != null)
            {
                try
                {
                    serializedData = await codec.DecodeAsync(serializedData, cancelToken);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Cannot {nameof(IPayloadCodec.DecodeAsync)} the specified"
                                                      + $" {nameof(serializedData)};"
                                                      + $" type of the specified {nameof(codec)}: \"{codec.GetType().FullName}\";"
                                                      + $" number of specified {nameof(Temporal.Api.Common.V1.Payload)}-entries:"
                                                      + $" {Format.SpellIfNull(serializedData.Payloads_?.Count)}.",
                                                        ex);
                }
            }

            return converter.Deserialize<T>(serializedData);
        }

        /// <summary>
        /// Convenience wrapper method for the corresponding <see cref="IPayloadConverter" />-API.
        /// (See <see cref="IPayloadConverter.TrySerialize{T}(T, Payloads)" />.)
        /// Calls the respective <c>IPayloadConverter</c>-method, and in case of a failure (<c>false</c> return value),
        /// generates exceptions with detailed diagnostic messages.
        /// </summary>
        public static void Serialize<T>(this IPayloadConverter converter, T item, Payloads serializedDataAccumulator)
        {
            Validate.NotNull(converter);

            try
            {
                SerializeOrThrow(converter, item, serializedDataAccumulator);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot {nameof(Serialize)} the specified {nameof(item)};"
                                                  + $" type of the {nameof(converter)}: \"{converter.GetType().FullName}\";"
                                                  + $" static type of the specified {nameof(item)}: \"{typeof(T).FullName}\";"
                                                  + (item == null
                                                        ? $" the specified {nameof(item)} is null."
                                                        : $" runtime type of the specified {nameof(item)}: \"{item.GetType().FullName}\"."),
                                                    ex);
            }
        }

        public static async Task<Payloads> SerializeAsync<T>(this IPayloadConverter converter,
                                                             IPayloadCodec codec,
                                                             T item,
                                                             CancellationToken cancelToken)
        {
            Payloads serializedData = new();
            converter.Serialize<T>(item, serializedData);

            if (codec != null)
            {
                try
                {
                    serializedData = await codec.EncodeAsync(serializedData, cancelToken);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Cannot {nameof(IPayloadCodec.EncodeAsync)} the serialized form of the specified"
                                                      + $" {nameof(item)};"
                                                      + $" type of the specified {nameof(codec)}: \"{codec.GetType().FullName}\";"
                                                      + $" number of {nameof(Temporal.Api.Common.V1.Payload)}-entries within"
                                                      + $" the serialized form of the specified {nameof(item)}:"
                                                      + $" {Format.SpellIfNull(serializedData.Payloads_?.Count)};"
                                                      + $" static type of the specified {nameof(item)}: \"{typeof(T).FullName}\";"
                                                      + (item == null
                                                            ? $" the specified {nameof(item)} is null."
                                                            : $" runtime type of the specified {nameof(item)}: \"{item.GetType().FullName}\"."),
                                                        ex);
                }
            }

            return serializedData;
        }

        private static T DeserializeOrThrow<T>(IPayloadConverter converter, Payloads serializedData)
        {
            if (converter.TryDeserialize<T>(serializedData, out T deserializedItem))
            {
                return deserializedItem;  // success
            }

            string message = (converter is CompositePayloadConverter compositeConverter)
                        ? $"Cannot {nameof(Deserialize)} the specified {nameof(serializedData)}"
                            + $" because none of the {compositeConverter.Converters.Count} {nameof(IPayloadConverter)}"
                            + $"-instances wrapped within the specified {nameof(CompositePayloadConverter)} can convert that data"
                            + $" to the required target type."
                        : $"Cannot {nameof(Deserialize)} the specified {nameof(serializedData)}"
                            + $" because the specified {nameof(IPayloadConverter)} of cannot convert that data"
                            + $" to the required target type.";

            if (serializedData.Payloads_.Count > 1)
            {
                message = message
                        + $"\nThe specified serialized {nameof(Temporal.Api.Common.V1.Payloads)}-collection contains multiple"
                        + $" {nameof(Temporal.Api.Common.V1.Payload)}-entries. Built-in {nameof(IPayloadConverter)} implementations"
                        + $" only support deserializing such data into a target container of type"
                        + $" \"{typeof(PayloadContainers.Unnamed.SerializedDataBacked).FullName}\"."
                        + $" Use such container as de-serialization target, or implement a custom {nameof(IPayloadConverter)}"
                        + $" to handle multiple {nameof(Temporal.Api.Common.V1.Payload)}-entries within a single"
                        + $" {nameof(Temporal.Api.Common.V1.Payloads)}-collection.";
            }

            throw new InvalidOperationException(message);
        }

        private static void SerializeOrThrow<T>(this IPayloadConverter converter, T item, Payloads serializedDataAccumulator)
        {
            if (converter.TrySerialize(item, serializedDataAccumulator))
            {
                return;  // success
            }

            string message = (converter is CompositePayloadConverter compositeConverter)
                        ? $"Cannot {nameof(Serialize)} the specified {nameof(item)}"
                            + $" because none of the {compositeConverter.Converters.Count} {nameof(IPayloadConverter)}"
                            + $"-instances wrapped within the specified {nameof(CompositePayloadConverter)} can convert that {nameof(item)}."
                        : $"Cannot {nameof(Serialize)} the specified {nameof(item)}"
                            + $" because the specified {nameof(IPayloadConverter)} of cannot convert that {nameof(item)}.";

            if (item != null && item is PayloadContainers.IUnnamed valuesContainer)
            {
                message = message
                        + $"\nThe specified data item is an {nameof(PayloadContainers.IUnnamed)} that holds {valuesContainer.Count} values."
                        + $" Although the specified {nameof(IPayloadConverter)} may be able to handle the container itself,"
                        + $" it may have not been able to handle one or more of the values within the container.";
            }
            else if (IsNormalEnumerable<T>(item))
            {
                message = message
                        + $"\nThe specified data item is an IEnumerable. Specifying Enumerables (arrays,"
                        + $" collections, ...) for workflow payloads is not supported by the built-in"
                        + $" {nameof(IPayloadConverter)} implementations because it is ambiguous"
                        + $" whether you intended to use the Enumerable as the SINGLE argument/payload,"
                        + $" or whether you intended to use MULTIPLE arguments/payloads, one for each element of your collection."
                        + $" To serialize an IEnumerable using the built-in {nameof(IPayloadConverter)}s"
                        + $" you need to wrap your data into an {nameof(Temporal.Common.IPayload)} container."
                        + $"\nFor example, to use an array of integers (`int[] data`) as a SINGLE argument, you can wrap it like this:"
                        + $" `SomeWorkflowApi(.., {nameof(Temporal.Common.Payload)}.{nameof(Temporal.Common.Payload.Unnamed)}<int[]>(data))`."
                        + $"\nTo use the contents of the aforementioned `data` array as MULTIPLE integer arguments, you can wrap it like this:"
                        + $" `SomeWorkflowApi(.., {nameof(Temporal.Common.Payload)}.{nameof(Temporal.Common.Payload.Unnamed)}<int>(data))`."
                        + $"\nNote that, if suported by the workflow implementation, it is preferable to use a"
                        + $" {nameof(Temporal.Common.Payload.Named)} {nameof(Temporal.Common.Payload)}-container."
                        + $" To avoid using {nameof(Temporal.Common.IPayload)} containers, implement a custom {nameof(IPayloadConverter)}"
                        + $" to handle IEnumerable arguments/payloads as required.";
            }

            throw new InvalidOperationException(message);
        }
    }
}
