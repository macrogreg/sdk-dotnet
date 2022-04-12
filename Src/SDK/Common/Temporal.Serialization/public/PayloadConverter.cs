﻿using System;
using Candidly.Util;
using Google.Protobuf;
using Temporal.Api.Common.V1;
using Temporal.Common.Payloads;

namespace Temporal.Serialization
{
    public static class PayloadConverter
    {
        public const string PayloadMetadataEncodingKey = "encoding";

        /// <summary>
        /// Utility method used by several <c>IPayloadConverter</c> implementations.
        /// If <c>valueBytes</c> is NOT null, return the object referenced by <c>valueBytes</c>.
        /// Otherwise, convert the specified <c>value</c> into a <c>ByteString</c>, store it into the location referenced
        /// by <c>valueBytes</c>, and return the resulting object.
        /// </summary>        
        public static ByteString GetOrCreateBytes(string value, ref ByteString valueBytes)
        {
            ByteString bytes = valueBytes;
            if (bytes == null)
            {
                bytes = ByteString.CopyFromUtf8(value);
                valueBytes = bytes;
            }

            return bytes;
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
                                                  + $" type of the specified {nameof(converter)}: \"{converter.GetType().FullName}\";"
                                                  + $" static type of the specified {nameof(item)}: \"{typeof(T).FullName}\";"
                                                  + (item == null
                                                        ? $" the specified {nameof(item)} is null."
                                                        : $" runtime type of the specified {nameof(item)}: \"{item.GetType().FullName}\"."),
                                                    ex);
            }
        }

        private static T DeserializeOrThrow<T>(IPayloadConverter converter, Payloads serializedData)
        {
            if (converter.TryDeserialize<T>(serializedData, out T deserializedItem))
            {
                return deserializedItem;  // success
            }

            string message = (converter is AggregatePayloadConverter aggregateConverter)
                        ? $"Cannot {nameof(Deserialize)} the specified {nameof(serializedData)}"
                            + $" because none of the {aggregateConverter.Converters.Count} {nameof(IPayloadConverter)}"
                            + $"-instances wrapped within the specified {nameof(AggregatePayloadConverter)} can convert that data"
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
                        + $" \"{typeof(PayloadContainers.ForUnnamedValues.SerializedDataBacked).FullName}\"."
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

            string message = (converter is AggregatePayloadConverter aggregateConverter)
                        ? $"Cannot {nameof(Serialize)} the specified {nameof(item)}"
                            + $" because none of the {aggregateConverter.Converters.Count} {nameof(IPayloadConverter)}"
                            + $"-instances wrapped within the specified {nameof(AggregatePayloadConverter)} can convert that {nameof(item)}."
                        : $"Cannot {nameof(Serialize)} the specified {nameof(item)}"
                            + $" because the specified {nameof(IPayloadConverter)} of cannot convert that {nameof(item)}.";

            if (item != null && item is IUnnamedValuesContainer valuesContainer)
            {
                message = message
                        + $"\nThe specified data item is an {nameof(IUnnamedValuesContainer)} that holds {valuesContainer.Count} values."
                        + $" Although the specified {nameof(IPayloadConverter)} may be able to handle the container itself,"
                        + $" it may have not been able to handle one or more of the values within the container.";
            }
            else if (IsUnwrappedEnumerable<T>(item))
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

        private static bool IsUnwrappedEnumerable<T>(T item)
        {
            // If Wrapped => False:
            if ((item != null && item is IUnnamedValuesContainer)
                    || typeof(IUnnamedValuesContainer).IsAssignableFrom(typeof(T)))
            {
                return false;
            }

            // Return wether Is IEnumerable:
            return ((item != null && item is System.Collections.IEnumerable)
                    || typeof(System.Collections.IEnumerable).IsAssignableFrom(typeof(T)));
        }
    }
}
