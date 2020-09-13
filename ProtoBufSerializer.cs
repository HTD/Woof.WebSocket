using System;
using System.IO;
using System.Linq;
using System.Reflection;

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
        /// <param name="message">Message.</param>
        /// <param name="typeHint">Type hint.</param>
        /// <returns>Buffer.</returns>
        public ArraySegment<byte> Serialize(object message, Type typeHint = null) {
            if (message is null && typeHint is null) throw new ArgumentNullException();
            using var targetStream = new MemoryStream();
            var messageType = typeHint ?? message.GetType();
            GenericSerializer.MakeGenericMethod(messageType).Invoke(null, new object[] { targetStream, message });
            targetStream.TryGetBuffer(out var buffer);
            return buffer;
        }

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

        /// <summary>
        /// Generic serializer method to use with run-time types.
        /// </summary>
        private readonly static MethodInfo GenericSerializer
            = typeof(ProtoBuf.Serializer).GetMethods(BindingFlags.Public | BindingFlags.Static).Single(i => {
                var parameters = i.GetParameters();
                return i.Name == "Serialize" && parameters.Length == 2 && parameters[0].ParameterType == typeof(Stream);
            });

    }

}