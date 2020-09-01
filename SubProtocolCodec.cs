using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Woof.WebSocket {

    /// <summary>
    /// An interface of subprotocol codecs for WebSocket servers and clients.<br/>
    /// Allows implementing virtually any subprotocol for a WebSocket API.
    /// </summary>
    public abstract class SubProtocolCodec {

        /// <summary>
        /// Gets the buffer serializer implementation.
        /// </summary>
        protected abstract IBufferSerializer Serializer { get; }

        /// <summary>
        /// Gets or sets the state used to access <see cref="SessionProvider"/> and <see cref="IAuthenticationProvider"/>.
        /// </summary>
        public IStateProvider State { get; internal set; }

        /// <summary>
        /// Gets the subprotocol name.
        /// </summary>
        public abstract string SubProtocol { get; }

        /// <summary>
        /// Gets the new unique message identifier.
        /// </summary>
        public abstract Guid NewId { get; }

        /// <summary>
        /// Gets the message types available in the API.
        /// </summary>
        protected MessageTypeDictionary MessageTypes { get; } = new MessageTypeDictionary();

        /// <summary>
        /// Loads message types if applicable.
        /// </summary>
        public virtual void LoadMessageTypes() { }

        /// <summary>
        /// Reads and decodes a message from the WebSocket context.
        /// </summary>
        /// <param name="context">WebSocket context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="limit">Optional message length limit, applied if positive value provided.</param>
        /// <returns>Task returning decoded message with the identifier.</returns>
        public abstract Task<DecodeResult> DecodeMessageAsync(WebSocketContext context, CancellationToken token, int limit = -1);

        /// <summary>
        /// Encodes the message and sends it to the WebSocket context.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="context">WebSocket context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="message">Message to send.</param>
        /// <param name="id">Optional message identifier, if not set - new unique identifier will be used.</param>
        /// <returns>Task completed when the message is sent.</returns>
        public abstract Task EncodeMessageAsync<TMessage>(WebSocketContext context, CancellationToken token, TMessage message, Guid id = default);

        /// <summary>
        /// Signs a serialized message payload with a type of HMAC algorithm.
        /// </summary>
        /// <param name="message">Binary message payload.</param>
        /// <param name="key">Message signing key.</param>
        /// <returns>Message signature (20 bytes, 160 bits).</returns>
        public virtual byte[] Sign(ArraySegment<byte> message, byte[] key) {
            using var hmac = new HMACSHA256(key); return hmac.ComputeHash(message.Array, message.Offset, message.Count);
        }

        /// <summary>
        /// Gets a hash of a key.
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns>32 bytes (128 bits).</returns>
        public virtual byte[] GetHash(byte[] apiKey) {
            using var sha = SHA256.Create();
            return sha.ComputeHash(apiKey);
        }

        /// <summary>
        /// Gets a new key.
        /// </summary>
        /// <returns>64 bytes (128 bits).</returns>
        public virtual byte[] GetKey() {
            using var hmac = HMACSHA256.Create();
            return hmac.Key;
        }

        /// <summary>
        /// Gets a key from string.
        /// </summary>
        /// <param name="keyString">Key string.</param>
        /// <returns>Key bytes.</returns>
        public virtual byte[] GetKey(string keyString) => Convert.FromBase64String(keyString);

        /// <summary>
        /// Gets a key string from key bytes.
        /// </summary>
        /// <param name="key">Key bytes.</param>
        /// <returns>Key string.</returns>
        public virtual string GetKeyString(byte[] key) => Convert.ToBase64String(key);

    }

}