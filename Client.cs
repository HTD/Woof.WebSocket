using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

using Woof.WebSocket.WoofSubProtocol;

namespace Woof.WebSocket {

    #region WOOF Client

    /// <summary>
    /// WebSocket client base providing a simple WebSocket API over the <see cref="WebSocket.WoofSubProtocol.WoofCodec"/>.
    /// </summary>
    public abstract class Client : Client<int, Guid, WoofCodec> {

        /// <summary>
        /// Occurs when a message is received by the client.
        /// </summary>
        public new event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Handles <see cref="MessageReceived"/> event.
        /// </summary>
        /// <param name="decodeResult">Message decoding result.</param>
        /// <param name="context">WebSocket context.</param>
        protected override void OnMessageReceived(DecodeResult<int, Guid> decodeResult, WebSocketContext context)
            => MessageReceived?.Invoke(this, new MessageReceivedEventArgs(decodeResult, context));

    }

    #endregion

    #region Generic Client

    /// <summary>
    /// WebSocket client base providing simple WebSocket transport and handshake over any given subprotocol.
    /// </summary>
    public abstract class Client<TTypeIndex, TMessageId, TCodec> : WebSocketTransport<TTypeIndex, TMessageId, TCodec>, IDisposable where TCodec : SubProtocolCodec<TTypeIndex, TMessageId>, new() {

        #region Public API

        /// <summary>
        /// Starts the client conection.
        /// </summary>
        /// <returns>Task completed when initialized and the receiving task is started.</returns>
        public async Task StartAsync() {
            if (CTS != null || Context != null) throw new InvalidOperationException("Client already started");
            State = ServiceState.Starting;
            OnStateChanged(State);
            CTS = new CancellationTokenSource();
            var clientWebSocket = new ClientWebSocket();
            Context = new WebSocketContext(clientWebSocket);
            clientWebSocket.Options.AddSubProtocol(Codec.SubProtocol);
            await clientWebSocket.ConnectAsync(EndPointUri, CTS.Token);
            await StartReceiveAsync(Context, CTS.Token, OnCloseReceivedAsync);
        }

        /// <summary>
        /// Stops the client connection.
        /// </summary>
        /// <returns>Task completed when all client tasks are stopped and the connection is closed.</returns>
        public async Task StopAsync() {
            if (State == ServiceState.Stopping || State == ServiceState.Stopped) return;
            if (State == ServiceState.Starting) throw new InvalidOperationException("Cannot shutdown during startup");
            State = ServiceState.Stopping;
            OnStateChanged(State);
            try {
                CTS.CancelAfter(TimeSpan.FromSeconds(2));
                await Context.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CTS.Token);
                await Context.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CTS.Token);
            } catch (Exception) { }
            finally {
                Context?.Dispose();
                Context = null;
                CTS?.Dispose();
                CTS = null;
                State = ServiceState.Stopped;
                OnStateChanged(State);
            }
        }

        /// <summary>
        /// Sends a message to the server context.
        /// </summary>
        /// <typeparam name="T">Message type.</typeparam>
        /// <param name="message">Message to send.</param>
        /// <param name="id">Optional message identifier, if not set - new unique identifier will be used.</param>
        /// <returns>Task completed when the sending is done.</returns>
        public async Task SendMessageAsync<T>(T message, TMessageId id = default)
            => await SendMessageAsync(message, Context, id);

        /// <summary>
        /// Sends a message to the server context and awaits until the response of the specified type is received.
        /// </summary>
        /// <typeparam name="TRequest">Request message type.</typeparam>
        /// <typeparam name="TResponse">Response message type.</typeparam>
        /// <param name="request">Request message.</param>
        /// <returns>Task returning the response message.</returns>
        public async Task<TResponse> SendAndReceiveAsync<TRequest, TResponse>(TRequest request)
            => await SendAndReceiveAsync<TRequest, TResponse>(request, Context);

        /// <summary>
        /// Disposes all resources used by the client.
        /// Closes the connection if not already closed.
        /// </summary>
        public void Dispose() => StopAsync().Wait();

        #endregion

        #region Helpers

        /// <summary>
        /// Stops the client when the server is closed.
        /// </summary>
        /// <param name="context">WebSocket context.</param>
        /// <returns>Task completed when the client is closed.</returns>
        private async Task OnCloseReceivedAsync(WebSocketContext context) => await StopAsync();

        #endregion

        #region Data fields

        /// <summary>
        /// WebSocket context used to exchange binary data with the server.
        /// </summary>
        WebSocketContext Context;

        #endregion
    
    }

    #endregion

}