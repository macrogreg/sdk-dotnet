using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

#if NETCOREAPP3_1_OR_GREATER
using System.Runtime.InteropServices;
#endif

using Temporal.Common.Payloads;
using Temporal.Util;

namespace Temporal.Common
{
    public static partial class Payload
    {
        public static readonly IPayload.Void Void = IPayload.Void.Instance;
        public static readonly Task<IPayload> VoidTask = Task.FromResult<IPayload>(Payload.Void);

        #region Named(..)

        public static object Named<T>(params object[] namedValues)
        {
            // This method is currently used in nameof(..) clauses when generating descriptive messages.
            // @ToDo: A full implementation will be added in later iterations.
            throw new NotImplementedException("@ToDo");
        }

        #endregion Named(..)

        #region Unnamed(..)

        public static PayloadContainers.Unnamed.InstanceBacked<object> Unnamed(params object[] values)
        {
            return Payload.Unnamed<object>(values);
        }

        public static PayloadContainers.Unnamed.InstanceBacked<T> Unnamed<T>(params T[] values)
        {
            return Payload.Unnamed((IReadOnlyList<T>) values);
        }

        public static PayloadContainers.Unnamed.InstanceBacked<T> Unnamed<T>(IEnumerable<T> values)
        {
            Validate.NotNull(values);

            if (values is IReadOnlyList<T> readyList)
            {
                return Payload.Unnamed<T>(readyList);
            }

            List<T> valsList = new();
            foreach (T val in values)
            {
                valsList.Add(val);
            }

            return Payload.Unnamed<T>(valsList);
        }

        public static PayloadContainers.Unnamed.InstanceBacked<T> Unnamed<T>(IReadOnlyList<T> values)
        {
            return new PayloadContainers.Unnamed.InstanceBacked<T>(values);
        }

        #endregion Unnamed(..)


        #region Enumerable(..)

        public static PayloadContainers.Enumerable Enumerable(IEnumerable enumerable)
        {
            return (enumerable is PayloadContainers.Enumerable alreadyWrapped)
                        ? alreadyWrapped
                        : new PayloadContainers.Enumerable(enumerable);
        }

        public static PayloadContainers.Enumerable<TElem> Enumerable<TElem>(IEnumerable<TElem> enumerable)
        {
            return (enumerable is PayloadContainers.Enumerable<TElem> alreadyWrapped)
                        ? alreadyWrapped
                        : new PayloadContainers.Enumerable<TElem>(enumerable);
        }

        #endregion Enumerable(..)


        #region Raw(..)

        public static Google.Protobuf.ByteString Raw(byte[] data)
        {
            return (data == null) ? null : Google.Protobuf.ByteString.CopyFrom(data);
        }

        public static Google.Protobuf.ByteString Raw(ArraySegment<byte> data)
        {
            return Raw(data.Array, data.Offset, data.Count);
        }

        public static Google.Protobuf.ByteString Raw(byte[] data, int index, int count)
        {
            return (data == null)
                        ? null
                        : Google.Protobuf.ByteString.CopyFrom(data, index, count);
        }

#if NETCOREAPP3_1_OR_GREATER
        public static Google.Protobuf.ByteString Raw(ReadOnlySpan<byte> data)
        {
            return Google.Protobuf.ByteString.CopyFrom(data);
        }

        public static Google.Protobuf.ByteString Raw<T>(ReadOnlySpan<T> data) where T : struct
        {
            ReadOnlySpan<byte> byteData = MemoryMarshal.AsBytes(data);
            return Raw(byteData);
        }
#endif

        #endregion Raw(..)
    }
}
