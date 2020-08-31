using System;

namespace Woof.WebSocket {

    /// <summary>
    /// An interface to isolate the concrete serializers from their implementations.
    /// <see cref="ArraySegment{T}"/> is used as a buffer.
    /// </summary>
    public interface IBufferSerializer {

        /// <summary>
        /// Serializes a message to a buffer.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="message">Message.</param>
        /// <returns>Buffer.</returns>
        public ArraySegment<byte> Serialize<TMessage>(TMessage message);

        /// <summary>
        /// Deserializes a message from a buffer.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="source">Buffer.</param>
        /// <returns>Message.</returns>
        public TMessage Deserialize<TMessage>(ArraySegment<byte> source);

        /// <summary>
        /// Deserializes a message from a buffer.
        /// </summary>
        /// <param name="type">Message type.</param>
        /// <param name="source">Buffer.</param>
        /// <returns>Message.</returns>
        public object Deserialize(Type type, ArraySegment<byte> source);

    }

}