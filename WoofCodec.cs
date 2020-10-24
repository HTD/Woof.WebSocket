﻿using System;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Woof.WebSocket {

    // TODO: Implement stream message type / stream mode with opening another WebSocket for it.

    /// <summary>
    /// Implements WOOF subprotocol codec.
    /// </summary>
    public sealed class WoofCodec : SubProtocolCodec {

        #region Public API

        /// <summary>
        /// Common subprotocol name.
        /// </summary>
        public const string Name = "WOOF";

        /// <summary>
        /// Gets the Protocol Buffers serializer.
        /// </summary>
        protected override IBufferSerializer Serializer { get; } = new ProtoBufSerializer();

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
                MessageTypes.Add(t.Meta.MessageTypeId, new MessageTypeContext(t.Meta.MessageTypeId, t.Type, t.Meta.IsSigned, t.Meta.IsSignInRequest, t.Meta.IsError));
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
            var metaLengthBuffer = new ArraySegment<byte>(new byte[1]);
            var receiveResult = await context.ReceiveAsync(metaLengthBuffer, token);
            if (receiveResult.MessageType == WebSocketMessageType.Close)
                return new DecodeResult(receiveResult);
            if (receiveResult.MessageType != WebSocketMessageType.Binary)
                return new DecodeResult(new InvalidOperationException(EInvalidType));
            if (receiveResult.EndOfMessage || receiveResult.Count < 1)
                return new DecodeResult(new InvalidOperationException(EHeaderIncomplete));
            var metaLength = metaLengthBuffer.Array[0];
            var metaDataBuffer = new ArraySegment<byte>(new byte[metaLength]);
            receiveResult = await context.ReceiveAsync(metaDataBuffer, token);
            if (receiveResult.MessageType == WebSocketMessageType.Close)
                return new DecodeResult(receiveResult);
            if (receiveResult.MessageType != WebSocketMessageType.Binary)
                return new DecodeResult(new InvalidOperationException(EInvalidType));
            if (receiveResult.Count < metaLength)
                return new DecodeResult(new InvalidOperationException(EHeaderIncomplete));
            var metaData = Serializer.Deserialize<MessageMetadata>(metaDataBuffer);
            if (metaData is null) return new DecodeResult(new NullReferenceException(EMissingMetadata), default);
            if (!MessageTypes.ContainsKey(metaData.TypeId))
                return new DecodeResult(new InvalidOperationException(EUnknownType), metaData.Id);
            if (limit >= 0 && metaData.PayloadLength > limit)
                return new DecodeResult(new InvalidOperationException(String.Format(ELengthExceeded, nameof(limit))), metaData.Id);
            var typeContext = MessageTypes[metaData.TypeId];
            if (receiveResult.EndOfMessage)
                return new DecodeResult(typeContext, Activator.CreateInstance(typeContext.MessageType), metaData.Id);
            var messageBuffer = new ArraySegment<byte>(new byte[metaData.PayloadLength]);
            receiveResult = await context.ReceiveAsync(messageBuffer, token);
            if (receiveResult.Count < metaData.PayloadLength || !receiveResult.EndOfMessage)
                return new DecodeResult(new InvalidOperationException(EMessageIncomplete), metaData.Id);
            var message = Serializer.Deserialize(MessageTypes[metaData.TypeId].MessageType, messageBuffer);
            var isSignatureValid = false;
            var isSignInRequest = typeContext.IsSignInRequest || message is ISignInRequest;
            if (typeContext.IsSigned && metaData.Signature != null) {
                var key =
                    isSignInRequest
                    ? (message is ISignInRequest signInRequest && State.AuthenticationProvider != null 
                        ? await State.AuthenticationProvider.GetKeyAsync(signInRequest.ApiKey) 
                        : null
                    )
                    : State.SessionProvider.GetKey(context);
                if (key != null) {
                    byte[] expected = Sign(messageBuffer, key);
                    isSignatureValid = metaData.Signature.SequenceEqual(expected);
                }
            }
            return new DecodeResult(typeContext, message, metaData.Id, !isSignInRequest && typeContext.IsSigned, isSignatureValid);
        }

        /// <summary>
        /// Encodes the message and sends it to the WebSocket context.
        /// </summary>
        /// <param name="context">WebSocket context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="message">Message to send.</param>
        /// <param name="typeHint">Type hint.</param>
        /// <param name="id">Optional message identifier, if not set - new unique identifier will be used.</param>
        /// <returns>Task completed when the message is sent.</returns>
        public override async Task EncodeMessageAsync(WebSocketContext context, CancellationToken token, object message, Type? typeHint = null, Guid id = default) {
            if (!context.IsOpen) return;
            var typeContext = MessageTypes.GetContext(message, typeHint);
            var messageBuffer = Serializer.Serialize(message, typeHint);
            await EncodeMessageAsync(context, token, typeContext, messageBuffer, id);
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
            var typeContext = MessageTypes.GetContext<TMessage>();
            var messageBuffer = Serializer.Serialize(message);
            await EncodeMessageAsync(context, token, typeContext, messageBuffer, id);
        }

        /// <summary>
        /// Encodes a serialized message and sends it to the WebSocket context.
        /// </summary>
        /// <param name="context">WebSocket context.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="typeContext">Type context.</param>
        /// <param name="buffer">A buffer with the message already encoded.</param>
        /// <param name="id">Optional message identifier, if not set - new unique identifier will be used.</param>
        /// <returns>Task completed when the message is sent.</returns>
        private async Task EncodeMessageAsync(WebSocketContext context, CancellationToken token, MessageTypeContext typeContext, ArraySegment<byte> buffer, Guid id = default) {
            var isPayloadPresent = buffer.Count > 0;
            var key = typeContext.IsSigned ? State.SessionProvider.GetKey(context) : null;
            var metadata =
                key is null || !isPayloadPresent
                ? new MessageMetadata {
                    Id = id == default ? NewId : id,
                    TypeId = typeContext.Id,
                    PayloadLength = buffer.Count
                }
                : new MessageMetadata {
                    Id = id == default ? NewId : id,
                    TypeId = typeContext.Id,
                    PayloadLength = buffer.Count,
                    Signature = Sign(buffer, key)
                };
            var metaDataBuffer = Serializer.Serialize(metadata);
            var metaSizeBuffer = new ArraySegment<byte>(new byte[1]);
            metaSizeBuffer.Array[0] = (byte)metaDataBuffer.Count;
            var messageParts =
                isPayloadPresent
                ? new ArraySegment<byte>[] { metaSizeBuffer, metaDataBuffer, buffer }
                : new ArraySegment<byte>[] { metaSizeBuffer, metaDataBuffer };
            if (!context.IsOpen) return;
            await context.SendAsync(messageParts, WebSocketMessageType.Binary, token);
        }

        #endregion

        #region Exception messages

        private const string EInvalidType = "Invalid message type, binary expected";
        private const string EHeaderIncomplete = "Header incomplete";
        private const string EMissingMetadata = "Missing message metadata";
        private const string EUnknownType = "Uknown message type";
        private const string ELengthExceeded = "Message length exceeds {0}";
        private const string EMessageIncomplete = "Message data incomplete";

        #endregion

    }

}