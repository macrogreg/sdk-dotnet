using System;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Temporal.Api.Common.V1;

namespace Temporal.Serialization
{
    public class ProtobufPayloadConverter : IPayloadConverter
    {
        public const string PayloadMetadataEncodingValue = "binary/protobuf";

        private static ByteString s_payloadMetadataEncodingValueBytes = null;

        private static ByteString PayloadMetadataEncodingValueBytes
        {
            get { return PayloadConverter.GetOrCreateBytes(PayloadMetadataEncodingValue, ref s_payloadMetadataEncodingValueBytes); }
        }

        private bool TryGetMessageDescriptor<T>(out MessageDescriptor messageDescriptor)
        {
            // @ToDo: Consider for future: This would be a lot faster if we could cache Message Descriptors
            // for commonly used types. A LRU cache with a size of 30-50 items might speed things up a lot.
            // But that's a lot of complexity, so we shuld only do it if perf measurements point us into
            // this directipon.

            try
            {
                // Will fail if there is no defrault ctor.
                object messageObj = Activator.CreateInstance<T>();
                if (messageObj is IMessage message)
                {
                    messageDescriptor = message.Descriptor;
                    return true;
                }
            }
            catch
            {
                // Likely a type mismatch. Just return false;
            }

            messageDescriptor = null;
            return false;
        }

        public bool TryDeserialize<T>(Payloads serializedData, out T item)
        {
            if (SerializationUtil.TryGetSinglePayload(serializedData, out Payload serializedItem)
                    && typeof(IMessage).IsAssignableFrom(typeof(T))
                    && !PayloadConverter.IsNormalEnumerable<T>()
                    && serializedItem.Metadata.TryGetValue(PayloadConverter.PayloadMetadataEncodingKey, out ByteString encodingBytes)
                    && PayloadMetadataEncodingValueBytes.Equals(encodingBytes)
                    && TryGetMessageDescriptor<T>(out MessageDescriptor messageDescriptor))
            {
                IMessage message = messageDescriptor.Parser.ParseFrom(serializedItem.Data);
                item = (T) message;

                return true;
            }

            item = default(T);
            return false;
        }

        public bool TrySerialize<T>(T item, Payloads serializedDataAccumulator)
        {
            if (item != null && item is IMessage messageItem && !PayloadConverter.IsNormalEnumerable(item))
            {
                Payload serializedItemData = new();
                serializedItemData.Metadata.Add(PayloadConverter.PayloadMetadataEncodingKey, PayloadMetadataEncodingValueBytes);
                serializedItemData.Data = messageItem.ToByteString();
                SerializationUtil.Add(serializedDataAccumulator, serializedItemData);
                return true;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            return Object.ReferenceEquals(this, obj) || ((obj != null) && this.GetType().Equals(obj.GetType()));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}