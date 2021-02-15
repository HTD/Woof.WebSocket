using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Woof.WebSocket {

    /// <summary>
    /// WebSocket client base providing simple WebSocket transport and handshake over any given subprotocol.
    /// </summary>
    public abstract class Client<TCodec> : WebSocketTransport<TCodec>, IDisposable where TCodec : SubProtocolCodec, new() {

        #region Public API

        /// <summary>
        /// Gets or sets the operation timeout.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Starts the client conection.
        /// </summary>
        /// <returns>Task completed when initialized and the receiving task is started.</returns>
        public async Task StartAsync() {
            if (CTS != null || Context != null) throw new InvalidOperationException("Client already started");
            State = ServiceState.Starting;
            OnStateChanged(State);
            CTS = new CancellationTokenSource();
            using var timeoutCTS = new CancellationTokenSource(Timeout);
            using var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(CTS.Token, timeoutCTS.Token);
            var clientWebSocket = new ClientWebSocket();
            Context = new WebSocketContext(clientWebSocket);
            if (Codec.SubProtocol != null)
                clientWebSocket.Options.AddSubProtocol(Codec.SubProtocol);
            try {
                IsConnected = false;
                await clientWebSocket.ConnectAsync(EndPointUri, linkedCTS.Token);
                IsConnected = true;
                await StartReceiveAsync(Context, linkedCTS.Token, OnCloseReceivedAsync);
            } catch {
                await StopAsync();
                throw;
            }
        }

        /// <summary>
        /// Stops the client connection.
        /// </summary>
        /// <returns>Task completed when all client tasks are stopped and the connection is closed.</returns>
        public async Task StopAsync() {
            if (State == ServiceState.Stopping || State == ServiceState.Stopped) return;
            State = ServiceState.Stopping;
            OnStateChanged(State);
            try {
                if (Context != null && CTS != null && IsConnected) {
                    CTS.CancelAfter(Timeout);
                    await Context.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CTS.Token);
                    await Context.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CTS.Token);
                }
            } catch (Exception) { }
            finally {
                IsConnected = false;
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
        /// <param name="message">Message to send.</param>
        /// <param name="typeHint">Type hint.</param>
        /// <param name="id">Optional message identifier, if not set - new unique identifier will be used.</param>
        /// <returns>Task completed when the sending is done.</returns>
        public async Task SendMessageAsync(object message, Type? typeHint = null, Guid id = default) {
            if (Context != null) await SendMessageAsync(message, typeHint, Context, id);
        }

        /// <summary>
        /// Sends a message to the server context.
        /// </summary>
        /// <typeparam name="T">Message type.</typeparam>
        /// <param name="message">Message to send.</param>
        /// <param name="id">Optional message identifier, if not set - new unique identifier will be used.</param>
        /// <returns>Task completed when the sending is done.</returns>
        public async Task SendMessageAsync<T>(T message, Guid id = default) { 
            if (Context != null) await SendMessageAsync(message, Context, id);
        }

        /// <summary>
        /// Sends a message to the server context and awaits until the response of the specified type is received.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>Task returning the response message.</returns>
        public async Task<object?> SendAndReceiveAsync(object request)
            => Context != null ? await SendAndReceiveAsync(request, Context) : null;

        /// <summary>
        /// Sends a message to the server context and awaits until the response of the specified type is received.
        /// </summary>
        /// <typeparam name="TRequest">Request message type.</typeparam>
        /// <typeparam name="TResponse">Response message type.</typeparam>
        /// <param name="request">Request message.</param>
        /// <returns>Task returning the response message.</returns>
        public async Task<TResponse> SendAndReceiveAsync<TRequest, TResponse>(TRequest request) {
            if (Context is null) throw new NullReferenceException("There is no context for that request. The Client is not started.");
            return await SendAndReceiveAsync<TRequest, TResponse>(request, Context);
        }

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
        WebSocketContext? Context;

        bool IsConnected;

        #endregion
    
    }

}