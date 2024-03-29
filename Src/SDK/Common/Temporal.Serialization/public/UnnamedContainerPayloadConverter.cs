﻿using System;
using System.Collections;

using Temporal.Common;
using Temporal.Common.Payloads;
using Temporal.Util;

using SerializedPayloads = Temporal.Api.Common.V1.Payloads;

namespace Temporal.Serialization
{
    public sealed class UnnamedContainerPayloadConverter : DelegatingPayloadConverterBase
    {
        public override bool TrySerialize<T>(T item, SerializedPayloads serializedDataAccumulator)
        {
            if (item == null)
            {
                return false;
            }

            // If `item` is a payload container backed by serialized data AND its converter is the hame used here
            //  => use the serialized data directly, whtout round-triping it.

            if (item is PayloadContainers.Unnamed.SerializedDataBacked serializedDataBackedItemsContainer
                    && serializedDataBackedItemsContainer.PayloadConverter.Equals(DelegateConvertersContainer))
            {
                Validate.NotNull(serializedDataAccumulator);

                if (serializedDataBackedItemsContainer.SerializedData?.Payloads_ != null)
                {
                    serializedDataAccumulator.Payloads_.AddRange(serializedDataBackedItemsContainer.SerializedData.Payloads_);
                }

                return true;
            }

            // If `item` is another kind of `PayloadContainers.IUnnamed`
            //  => delegate each contained unnamed value separately to the downstream converters.

            if (item != null && item is PayloadContainers.IUnnamed itemsContainer)
            {
                Validate.NotNull(serializedDataAccumulator);

                for (int i = 0; i < itemsContainer.Count; i++)
                {
                    try
                    {
                        object contVal = itemsContainer.GetValue<object>(i);

                        if (PayloadConverter.IsNormalEnumerable(contVal, out IEnumerable enumblVal))
                        {
                            PayloadConverter.Serialize(DelegateConvertersContainer,
                                                       Payload.Enumerable(enumblVal),
                                                       serializedDataAccumulator);
                        }
                        else if (contVal is PayloadContainers.IUnnamed subCont)
                        {
                            PayloadConverter.Serialize(DelegateConvertersContainer,
                                                       new PayloadContainers.Unnamed.ValuesSerializationContainer(subCont),
                                                       serializedDataAccumulator);
                        }
                        else
                        {
                            PayloadConverter.Serialize(DelegateConvertersContainer,
                                                       contVal,
                                                       serializedDataAccumulator);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Error serializing value at index {i} of the specified container"
                                                          + $" of type \"{itemsContainer.GetType().FullName}\".",
                                                            ex);
                    }
                }

                return true;
            }

            // Otherwise
            //  => This converter cannot handle `item`.

            return false;
        }

        public override bool TryDeserialize<T>(SerializedPayloads serializedData, out T deserializedItem)
        {
            Validate.NotNull(serializedData);

            // `PayloadContainers.Unnamed.SerializedDataBacked` is a container that supports strictly typed
            // lazy deserialization of data when the value is actually requested.
            // It supports multiple `Payload`-entries within the `Payloads`-collection.
            // That container is be used by SDK to offer APIs that access data when needed.

            // We can handle the conversion
            // if the user asked for any type `T` that can be assigned to `PayloadContainers.Unnamed.SerializedDataBacked`
            // OR if the user asked for any `PayloadContainers.IUnnamed`.

            if (typeof(PayloadContainers.Unnamed.SerializedDataBacked).IsAssignableFrom(typeof(T))
                    || typeof(PayloadContainers.IUnnamed) == typeof(T))
            {
                PayloadContainers.Unnamed.SerializedDataBacked container = new(serializedData, DelegateConvertersContainer);
                deserializedItem = container.Cast<PayloadContainers.Unnamed.SerializedDataBacked, T>();
                return true;
            }

            deserializedItem = default(T);
            return false;
        }
    }
}
