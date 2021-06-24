using System;
using System.IO;

namespace Woof.WebSocket {

    /// <summary>
    /// Dynamic heap memory buffer to read messages with unknown length.<br/>
    /// The buffer allocates more memory if it is needed.
    /// </summary>
    public struct DynamicBuffer {

        /// <summary>
        /// Gets the internal array buffer.
        /// </summary>
        public byte[] Array { get; private set; }

        /// <summary>
        /// Gets the slice offset.
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// Gets the slice byte count.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Contains hard limit for the buffer capacity.<br/>
        /// If unset (default) there is no limit.
        /// </summary>
        private readonly int Limit;

        /// <summary>
        /// Creates an empty buffer of specified initial size.
        /// </summary>
        /// <param name="size">Initial size.</param>
        /// <param name="limit">Optional capacity limit.</param>
        public DynamicBuffer(int size = 8192, int limit = default) {
            Array = new byte[size];
            Offset = 0;
            Count = size;
            Limit = limit;
        }

        /// <summary>
        /// Creates a buffer from an array.
        /// </summary>
        /// <param name="array">Array.</param>
        /// <param name="limit">Optional capacity limit.</param>
        public DynamicBuffer(byte[] array, int limit = default) {
            Array = array;
            Offset = 0;
            Count = array.Length;
            Limit = limit;
        }

        /// <summary>
        /// Create a buffer from a slice of an array.
        /// </summary>
        /// <param name="array">Array.</param>
        /// <param name="offset">Offset.</param>
        /// <param name="count">Count.</param>
        /// <param name="limit">Optional capacity limit.</param>
        public DynamicBuffer(byte[] array, int offset, int count, int limit = default) {
            Array = array;
            Offset = offset;
            Count = count;
            Limit = limit;
        }

        /// <summary>
        /// Creates a buffer from existing <see cref="ArraySegment{T}"/> buffer.
        /// </summary>
        /// <param name="arraySegment">Buffer.</param>
        /// <param name="limit">Optional capacity limit.</param>
        public DynamicBuffer(ArraySegment<byte> arraySegment, int limit = default) {
            if (arraySegment.Array is null) throw new NullReferenceException("Array segment provided must have non-null Array");
            Array = arraySegment.Array;
            Offset = arraySegment.Offset;
            Count = arraySegment.Count;
            Limit = limit;
        }

        /// <summary>
        /// Advances the buffer offset, allocates more memory for the next read operation if full capacity is used and the read operation is not the last one.
        /// <br/>
        /// When the last operation fills the exact buffer capacity without setting <paramref name="isLast"/>,
        /// <see cref="Advance(int, bool)"/> without parameters must be called to fix buffer count.
        /// </summary>
        /// <param name="readLength">The length of the data that was actually written to the buffer.</param>
        /// <param name="isLast">True, if the read operation was the last one. Prevents allocation if exact buffer length was read last.</param>
        public void Advance(int readLength = 0, bool isLast = false) {
            lock (Array) {
                if (readLength < Count || isLast) {
                    if (readLength == default && Offset == default) {
                        Count = 0;
                        return;
                    }
                    Offset += readLength;
                    Count = Offset;
                    Offset = 0;
                }
                else {
                    var newLength = Array.Length << 1;
                    if (Limit > 0 && newLength > Limit) newLength = Limit;
                    if (newLength <= Array.Length) throw new InternalBufferOverflowException();
                    var newArray = new byte[newLength];
                    Buffer.BlockCopy(Array, 0, newArray, 0, Array.Length);
                    Array = newArray;
                    Offset += readLength;
                    Count = newLength - Offset;
                }
            }
        }

        /// <summary>
        /// Converts to <see cref="ArraySegment{T}"/>.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        public static implicit operator ArraySegment<byte>(DynamicBuffer buffer) => new(buffer.Array, buffer.Offset, buffer.Count);

        /// <summary>
        /// Converts from <see cref="ArraySegment{T}"/>.
        /// </summary>
        /// <param name="arraySegment"><see cref="ArraySegment{T}"/>.</param>
        public static implicit operator DynamicBuffer(ArraySegment<byte> arraySegment) => new(arraySegment);

    }

}