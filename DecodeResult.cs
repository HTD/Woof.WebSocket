using System;
using System.Net.WebSockets;

namespace Woof.WebSocket {

    /// <summary>
    /// Defines message decoding result.
    /// </summary>
    /// <typeparam name="TTypeIndex">Message type index type.</typeparam>
    /// <typeparam name="TMessageId">Message identifier type.</typeparam>
    public class DecodeResult<TTypeIndex, TMessageId> {

        /// <summary>
        /// Gets the decoded message.
        /// </summary>
        public object Message { get; }

        /// <summary>
        /// Gets the message type identifier.
        /// </summary>
        public TTypeIndex TypeId { get; }

        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        public TMessageId MessageId { get; }

        /// <summary>
        /// Gets a value indicating whether the CLOSE frame was received instead of a message.
        /// </summary>
        public bool IsCloseFrame { get; }

        /// <summary>
        /// Gets a value indicating whether the message signature is valid.
        /// </summary>
        public bool IsSignatureValid { get; }

        /// <summary>
        /// Gets a value indicating whether the message is unauthorized:<br/>
        /// It's not a sign-in message, it should be signed, but the signature is not valid.
        /// </summary>
        public bool IsUnauthorized { get; }

        /// <summary>
        /// Gets a value indicating wheter the message was received correctly.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets an exception caught during receiving the message.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the WebSocket close status if the CLOSE frame was received.
        /// </summary>
        public WebSocketCloseStatus? CloseStatus { get; }

        /// <summary>
        /// Gets the WebSocket close status description if the CLOSE frame was received.
        /// </summary>
        public string CloseStatusDescription { get; }

        /// <summary>
        /// Creates "full" decode result with the received and decoded message.
        /// </summary>
        /// <param name="typeId">Message type identifier.</param>
        /// <param name="id">Message identifier.</param>
        /// <param name="message">Message content.</param>
        /// <param name="isValidSignatureRequired">True, if a valid signature of the message is required.</param>
        /// <param name="isSignatureValid">True, if the message signature is verified.</param>
        public DecodeResult(TTypeIndex typeId, TMessageId id, object message, bool isValidSignatureRequired = false, bool isSignatureValid = false) {
            TypeId = typeId;
            MessageId = id;
            Message = message;
            IsSignatureValid = isSignatureValid;
            IsUnauthorized = isValidSignatureRequired && !isSignatureValid;
            IsSuccess = true;
        }

        /// <summary>
        /// Creates "close" decode result from <see cref="WebSocketReceiveResult"/>.
        /// </summary>
        /// <param name="receiveResult">"CLOSE" <see cref="WebSocketReceiveResult"/></param>
        public DecodeResult(WebSocketReceiveResult receiveResult) {
            if (receiveResult.MessageType == WebSocketMessageType.Close) {
                IsCloseFrame = true;
                IsSuccess = true;
                CloseStatus = receiveResult.CloseStatus;
                CloseStatusDescription = receiveResult.CloseStatusDescription;
            }
        }

        /// <summary>
        /// Creates error decode result when the message metadata is read.
        /// </summary>
        /// <param name="typeId">Message type identifier.</param>
        /// <param name="id">Message identifier.</param>
        /// <param name="exception">Exception while receiving the message.</param>
        public DecodeResult(TTypeIndex typeId, TMessageId id, Exception exception) {
            TypeId = typeId;
            MessageId = id;
            Exception = exception;
        }

        /// <summary>
        /// Creates error decode result when the message metadata could not be read.
        /// </summary>
        /// <param name="exception"></param>
        public DecodeResult(Exception exception) => Exception = exception;

    }

}