using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using ProtoBuf;

using CodecBase = Woof.WebSocket.SubProtocolCodec<int, System.Guid>;
using DecodeResult = Woof.WebSocket.DecodeResult<int, System.Guid>;

namespace Woof.WebSocket.WoofSubProtocol {

    /// <summary>
    /// Implements WOOF subprotocol codec.
    /// </summary>
    public class WoofCodec : CodecBase {

        #region Public API

        /// <summary>
        /// Common subprotocol name.
        /// </summary>
        public const string Name = "WOOF";

        /// <summary>
        /// Gets the subprotocol name.
        /// </summary>
        public override string SubProtocol => Name;

        /// <summary>
        /// Gets the new unique message id.
        /// </summary>
        public override Guid NewId => Guid.NewGuid();

        /// <summary>
        /// Loads message types from the current application domain assemblies.
        /// </summary>
        public override void LoadMessageTypes() {
            foreach (var t in
                AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<MessageAttribute>() != null)
                .Select(t => new { Type = t, Meta = t.GetCustomAttribute<MessageAttribute>() })) {
                MessageTypes.Add(t.Meta.MessageTypeId, new MessageTypeContext(t.Type, t.Meta.IsSigned, t.Meta.IsSignInRequest));
            }
        }

        /// <summary>
        /// Reads and decodes a message from the socket.
        /// </summary>
        /// <param name="context">Thread-safe WebSocket pack.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="limit">Optional message length limit, applied if positive value provided.</param>
        /// <returns>Task returning decoded message with the identifier.</returns>
        public override async Task<DecodeResult> DecodeMessageAsync(WebSocketContext context, CancellationToken token, int limit = default) {
            var metaLengthBuffer = GetBuffer(1);
            var receiveResult = await context.ReceiveAsync(metaLengthBuffer, token);
            if (receiveResult.MessageType == WebSocketMessageType.Close)
                return new DecodeResult(receiveResult);
            if (receiveResult.MessageType != WebSocketMessageType.Binary)
                return new DecodeResult(new InvalidOperationException(EInvalidType));
            if (receiveResult.EndOfMessage || receiveResult.Count < 1)
                return new DecodeResult(new InvalidOperationException(EHeaderIncomplete));
            var metaLength = metaLengthBuffer.Array[0];
            var metaDataBuffer = GetBuffer(metaLength);
            receiveResult = await context.ReceiveAsync(metaDataBuffer, token);
            if (receiveResult.MessageType == WebSocketMessageType.Close)
                return new DecodeResult(receiveResult);
            if (receiveResult.MessageType != WebSocketMessageType.Binary)
                return new DecodeResult(new InvalidOperationException(EInvalidType));
            if (receiveResult.Count < metaLength)
                return new DecodeResult(new InvalidOperationException(EHeaderIncomplete));
            var metaData = Deserialize<MessageMetadata>(metaDataBuffer);
            if (!MessageTypes.ContainsKey(metaData.TypeId))
                return new DecodeResult(metaData.TypeId, metaData.Id, new InvalidOperationException(EUnknownType));
            if (limit >= 0 && metaData.PayloadLength > limit)
                return new DecodeResult(metaData.TypeId, metaData.Id, new InvalidOperationException(String.Format(ELengthExceeded, nameof(limit))));
            var typeContext = MessageTypes[metaData.TypeId];
            if (receiveResult.EndOfMessage)
                return new DecodeResult(metaData.TypeId, metaData.Id, Activator.CreateInstance(typeContext.MessageType));
            var messageBuffer = GetBuffer(metaData.PayloadLength);
            receiveResult = await context.ReceiveAsync(messageBuffer, token);
            if (receiveResult.Count < metaData.PayloadLength || !receiveResult.EndOfMessage)
                return new DecodeResult(metaData.TypeId, metaData.Id, new InvalidOperationException(EMessageIncomplete));
            var message = Deserialize(MessageTypes[metaData.TypeId].MessageType, messageBuffer);
            var isSignatureValid = false;
            var isSignInRequest = typeContext.IsSignInRequest || message is ISignInRequest;
            if (typeContext.IsSigned && metaData.Signature != null) {
                var key =
                    isSignInRequest
                    ? await State.AuthenticationProvider?.GetKeyAsync((message as ISignInRequest).ApiKey)
                    : State.SessionProvider.GetKey(context);
                if (key != null) {
                    byte[] expected = Sign(messageBuffer, key);
                    isSignatureValid = metaData.Signature.SequenceEqual(expected);
                }
            }
            return new DecodeResult(metaData.TypeId, metaData.Id, message, !isSignInRequest && typeContext.IsSigned, isSignatureValid);
        }

        /// <summary>
        /// Encodes the message and sends it to the WebSocket context.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="context">WebSocket context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="message">Message to send.</param>
        /// <param name="id">Optional message identifier, if not set - new unique identifier will be used.</param>
        /// <returns>Task completed when the message is sent.</returns>
        public override async Task EncodeMessageAsync<TMessage>(WebSocketContext context, CancellationToken token, TMessage message, Guid id = default) {
            if (!context.IsOpen) return;
            var typeId = MessageTypes.GetTypeId<TMessage>();
            var typeContext = MessageTypes[typeId];
            var messageBuffer = Serialize(message);
            var isPayloadPresent = messageBuffer.Count > 0;
            var key = typeContext.IsSigned ? State.SessionProvider.GetKey(context) : null;
            var metadata =
                key is null || !isPayloadPresent
                ? new MessageMetadata {
                    Id = id == default ? NewId : id,
                    TypeId = typeId,
                    PayloadLength = messageBuffer.Count
                }
                : new MessageMetadata {
                    Id = id == default ? NewId : id,
                    TypeId = typeId,
                    PayloadLength = messageBuffer.Count,
                    Signature = Sign(messageBuffer, key)
                };
            var metaDataBuffer = Serialize(metadata);
            var metaSizeBuffer = GetBuffer(1);
            metaSizeBuffer.Array[0] = (byte)metaDataBuffer.Count;
            var messageParts =
                isPayloadPresent 
                ? new ArraySegment<byte>[] { metaSizeBuffer, metaDataBuffer, messageBuffer }
                : new ArraySegment<byte>[] { metaSizeBuffer, metaDataBuffer };
            if (!context.IsOpen) return;
            await context.SendAsync(messageParts, WebSocketMessageType.Binary, token);
        }

        /// <summary>
        /// Signs a serialized message payload with a type of HMAC algorithm.
        /// </summary>
        /// <param name="message">Binary message payload.</param>
        /// <param name="key">Message signing key.</param>
        /// <returns>Message signature (20 bytes, 160 bits).</returns>
        public override byte[] Sign(ArraySegment<byte> message, byte[] key) {
            using var hmac = new HMACSHA256(key); return hmac.ComputeHash(message.Array, message.Offset, message.Count);
        }

        /// <summary>
        /// Gets a hash of a key.
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns>32 bytes (128 bits).</returns>
        public override byte[] GetHash(byte[] apiKey) {
            using var sha = SHA256.Create();
            return sha.ComputeHash(apiKey);
        }

        /// <summary>
        /// Gets a new key.
        /// </summary>
        /// <returns>64 bytes (128 bits).</returns>
        public override byte[] GetKey() {
            using var hmac = HMACSHA256.Create();
            return hmac.Key;
        }

        /// <summary>
        /// Gets a key from string.
        /// </summary>
        /// <param name="keyString">Key string.</param>
        /// <returns>Key bytes.</returns>
        public override byte[] GetKey(string keyString) => Convert.FromBase64String(keyString);

        /// <summary>
        /// Gets a key string from key bytes.
        /// </summary>
        /// <param name="key">Key bytes.</param>
        /// <returns>Key string.</returns>
        public override string GetKeyString(byte[] key) => Convert.ToBase64String(key);

        #endregion

        #region Helpers

        private ArraySegment<byte> GetBuffer(int size) => new ArraySegment<byte>(new byte[size]);

        private ArraySegment<byte> Serialize<TMessage>(TMessage message, int limit = default) {
            using var stream = limit >= 0 ? new MemoryStream(limit) : new MemoryStream();
            Serializer.Serialize(stream, message);
            if (stream.TryGetBuffer(out var buffer)) return buffer;
            else throw new InvalidOperationException();
        }

        private object Deserialize(Type type, ArraySegment<byte> buffer) {
            using var stream = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count);
            return Serializer.Deserialize(type, stream);
        }

        private T Deserialize<T>(ArraySegment<byte> buffer) {
            using var stream = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count);
            return Serializer.Deserialize<T>(stream);
        }

        #endregion

        #region Exception messages

        private const string EInvalidType = "Invalid message type, binary expected";
        private const string EHeaderIncomplete = "Header incomplete";
        private const string EUnknownType = "Uknown message type";
        private const string ELengthExceeded = "Message length exceeds {0}";
        private const string EMessageIncomplete = "Message data incomplete";

        #endregion

    }

}