using System;

namespace Woof.WebSocket {

    /// <summary>
    /// Message type with some metadata.
    /// </summary>
    public class MessageTypeContext {

        /// <summary>
        /// Gets the message type.
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// Gets a value indicating whether the message should be signed.
        /// </summary>
        public bool IsSigned { get; }

        /// <summary>
        /// Gets a value indicating whether the message is a sign in request.
        /// </summary>
        public bool IsSignInRequest { get; }

        /// <summary>
        /// Creates a new message type context.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="isSigned">True if message should be signed.</param>
        /// <param name="isSignInRequest">True if the message is a sign in request.</param>
        public MessageTypeContext(Type messageType, bool isSigned = false, bool isSignInRequest = false) {
            MessageType = messageType;
            IsSigned = isSigned;
            IsSignInRequest = isSignInRequest || messageType.GetInterface(nameof(ISignInRequest)) != null;
        }

    }

}