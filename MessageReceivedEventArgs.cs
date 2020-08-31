using System;

namespace Woof.WebSocket {

    /// <summary>
    /// Event data for the WebSocket MessageReceived events. (WOOF subprotocol version).
    /// </summary>
    public class MessageReceivedEventArgs : MessageReceivedEventArgs<int, Guid> { 
        
        /// <summary>
        /// Creates new data for WebSocket MessageReceived events.
        /// </summary>
        /// <param name="decodeResult"></param>
        /// <param name="context"></param>
        public MessageReceivedEventArgs(DecodeResult<int, Guid> decodeResult, WebSocketContext context) : base(decodeResult, context) { }

    }

    /// <summary>
    /// Event data for the WebSocket MessageReceived events.
    /// </summary>
    /// <typeparam name="TTypeIndex">Message type index type.</typeparam>
    /// <typeparam name="TMessageId">Message identifier type.</typeparam>
    public class MessageReceivedEventArgs<TTypeIndex, TMessageId> : WebSocketEventArgs {

        /// <summary>
        /// Gets the decoded message.
        /// </summary>
        public object Message => DecodeResult.Message;

        /// <summary>
        /// Gets the additional message metadata.
        /// </summary>
        public DecodeResult<TTypeIndex, TMessageId> DecodeResult { get; }

        /// <summary>
        /// Creates new WebSocket MessageReceived event data.
        /// </summary>
        /// <param name="decodeResult">Message decoding result.</param>
        /// <param name="context">WebSocket context.</param>
        public MessageReceivedEventArgs(DecodeResult<TTypeIndex, TMessageId> decodeResult, WebSocketContext context)
            : base(context) => DecodeResult = decodeResult;

    }

}