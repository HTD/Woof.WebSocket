using System;
using System.IO;

namespace Woof.WebSocket {

    /// <summary>
    /// Protocol Buffers serializer implementation.
    /// </summary>
    public class ProtoBufSerializer : IBufferSerializer {

        /// <summary>
        /// Serializes the message from the buffer.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="source">Buffer.</param>
        /// <returns>Message.</returns>
        public TMessage Deserialize<TMessage>(ArraySegment<byte> source)
            => ProtoBuf.Serializer.Deserialize<TMessage>((ReadOnlyMemory<byte>)source);

        /// <summary>
        /// Deserializes the message from the buffer.
        /// </summary>
        /// <param name="type">Message type.</param>
        /// <param name="source">Buffer.</param>
        /// <returns>Message.</returns>
        public object Deserialize(Type type, ArraySegment<byte> source)
            => ProtoBuf.Serializer.NonGeneric.Deserialize(type, (ReadOnlyMemory<byte>)source);

        /// <summary>
        /// Serializes the message to the buffer.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="message">Message.</param>
        /// <returns>Buffer.</returns>
        public ArraySegment<byte> Serialize<TMessage>(TMessage message) {
            using var targetStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(targetStream, message);
            targetStream.TryGetBuffer(out var buffer);
            return buffer;
        }

    }

}