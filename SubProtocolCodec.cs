﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Woof.WebSocket {

    /// <summary>
    /// An interface of subprotocol codecs for WebSocket servers and clients.<br/>
    /// Allows implementing virtually any subprotocol for a WebSocket API.
    /// </summary>
    /// <typeparam name="TTypeIndex">Message type index type.</typeparam>
    /// <typeparam name="TMessageId">Message identifier type.</typeparam>
    public abstract class SubProtocolCodec<TTypeIndex, TMessageId> {

        /// <summary>
        /// Gets or sets the state used to access <see cref="SessionProvider"/> and <see cref="IAuthenticationProvider"/>.
        /// </summary>
        internal IStateProvider State { get; set; }

        /// <summary>
        /// Gets the subprotocol name.
        /// </summary>
        public abstract string SubProtocol { get; }

        /// <summary>
        /// Gets the new unique message identifier.
        /// </summary>
        public abstract TMessageId NewId { get; }

        /// <summary>
        /// Gets the message types available in the API.
        /// </summary>
        protected MessageTypeDictionary<TTypeIndex> MessageTypes { get; } = new MessageTypeDictionary<TTypeIndex>();

        /// <summary>
        /// Loads message types.
        /// </summary>
        public abstract void LoadMessageTypes();

        /// <summary>
        /// Reads and decodes a message from the WebSocket context.
        /// </summary>
        /// <param name="context">WebSocket context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="limit">Optional message length limit, applied if positive value provided.</param>
        /// <returns>Task returning decoded message with the identifier.</returns>
        public abstract Task<DecodeResult<TTypeIndex, TMessageId>> DecodeMessageAsync(WebSocketContext context, CancellationToken token, int limit = -1);

        /// <summary>
        /// Encodes the message and sends it to the WebSocket context.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="context">WebSocket context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="message">Message to send.</param>
        /// <param name="id">Optional message identifier, if not set - new unique identifier will be used.</param>
        /// <returns>Task completed when the message is sent.</returns>
        public abstract Task EncodeMessageAsync<TMessage>(WebSocketContext context, CancellationToken token, TMessage message, TMessageId id = default);

        /// <summary>
        /// Signs a serialized message payload with a type of HMAC algorithm.
        /// </summary>
        /// <param name="message">Binary message payload.</param>
        /// <param name="key">Message signing key.</param>
        /// <returns>Message signature.</returns>
        public abstract byte[] Sign(ArraySegment<byte> message, byte[] key);

        /// <summary>
        /// Gets a hash of a key.
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns>Hash bytes.</returns>
        public abstract byte[] GetHash(byte[] apiKey);

        /// <summary>
        /// Gets a new key.
        /// </summary>
        /// <returns>Key bytes.</returns>
        public abstract byte[] GetKey();

        /// <summary>
        /// Gets a key from string.
        /// </summary>
        /// <param name="keyString">Key string.</param>
        /// <returns>Key bytes.</returns>
        public abstract byte[] GetKey(string keyString);

        /// <summary>
        /// Gets a key string from key bytes.
        /// </summary>
        /// <param name="key">Key bytes.</param>
        /// <returns>Key string.</returns>
        public abstract string GetKeyString(byte[] key);

    }

}