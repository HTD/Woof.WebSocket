using System;

namespace Woof.WebSocket {

    /// <summary>
    /// An interface to isolate the concrete serializers from their implementations.
    /// <see cref="ArraySegment{T}"/> is used as a buffer.
    /// </summary>
    public interface IBufferSerializer {

        /// <summary>
        /// Serializes the message to the buffer.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="typeHint">Type hint.</param>
        /// <returns>Buffer.</returns>
        public ArraySegment<byte> Serialize(object? message, Type? typeHint = null);

        /// <summary>
        /// Serializes a message to a buffer.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="message">Message.</param>
        /// <returns>Buffer.</returns>
        public ArraySegment<byte> Serialize<TMessage>(TMessage? message) where TMessage : class;

        /// <summary>
        /// Deserializes a message from a buffer.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="source">Buffer.</param>
        /// <returns>Message.</returns>
        public TMessage? Deserialize<TMessage>(ArraySegment<byte> source) where TMessage : class;

        /// <summary>
        /// Deserializes a message from a buffer.
        /// </summary>
        /// <param name="typeHint">Message type.</param>
        /// <param name="source">Buffer.</param>
        /// <returns>Message.</returns>
        public object? Deserialize(Type? typeHint, ArraySegment<byte> source);

    }

}