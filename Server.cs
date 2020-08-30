using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Text.RegularExpressions;
using Woof.WebSocket.WoofSubProtocol;

namespace Woof.WebSocket {

    #region WOOF Server

    /// <summary>
    /// WebSocket server base to provide simple WebSocket API over <see cref="WebSocket.Codec"/>.
    /// </summary>
    public abstract class Server : Server<int, Guid, WoofCodec> { }

    #endregion

    #region Generic Server

    /// <summary>
    /// WebSocket server base to provide simple WebSocket transport and handshake over any given subprotocol.
    /// </summary>
    public abstract class Server<TTypeIndex, TMessageId, TCodec> : WebSocketTransport<TTypeIndex, TMessageId, TCodec>, IDisposable where TCodec : SubProtocolCodec<TTypeIndex, TMessageId>, new() {

        #region Public API

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <returns>Task completed when initialized and listening.</returns>
        public async Task StartAsync() {
            if (CTS != null || Listener != null || State == ServiceState.Started) throw new InvalidOperationException("Server already started");
            if (State == ServiceState.Starting) throw new InvalidOperationException("Server is starting");
            State = ServiceState.Starting;
            StateChanged(State);
            CTS = new CancellationTokenSource();
            Listener = new HttpListener();
            var prefix = RxWS.Replace(EndPointUri.ToString(), "http");
            Listener.Prefixes.Add(prefix);
            Listener.Start();
            await AsyncLoop.FromIterationAsync(ClientConnectedAsync, CTS.Token, ConnectException);
            State = ServiceState.Started;
            StateChanged(State);
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        /// <returns>Task completed when all server tasks are stopped and all connections are closed.</returns>
        public async Task StopAsync() {
            if (CTS is null && Listener is null) return;
            State = ServiceState.Stopping;
            StateChanged(State);
            CTS.CancelAfter(TimeSpan.FromSeconds(2));
            while (Clients.Any()) {
                var client = Clients.Last();
                var statusDescription = "SERVER SHUTDOWN";
                await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, statusDescription, CancellationToken.None);
                try { await client.CloseAsync(WebSocketCloseStatus.NormalClosure, statusDescription, CancellationToken.None); }
                catch (Exception) { }
                finally { client.Dispose(); }
                Clients.Remove(client);
            }
            Listener.Stop();
            SessionProvider.CloseAllSessions();
            Listener = null;
            CTS.Cancel();
            CTS = null;
            State = ServiceState.Stopped;
            StateChanged(State);
        }

        /// <summary>
        /// Sends a message to the specified context.
        /// </summary>
        /// <typeparam name="T">Type of the message.</typeparam>
        /// <param name="message">Message to send.</param>
        /// <param name="context">Target context.</param>
        /// <param name="id">Optional message identifier, if not set - new unique identifier will be used.</param>
        /// <returns>Task completed when the sending is done.</returns>
        public new async Task SendMessageAsync<T>(T message, WebSocketContext context, TMessageId id = default)
            => await base.SendMessageAsync(message, context, id);

        /// <summary>
        /// Sends a message to the specified context and awaits until the response of the specified type is received.
        /// </summary>
        /// <typeparam name="TRequest">Request message type.</typeparam>
        /// <typeparam name="TResponse">Response message type.</typeparam>
        /// <param name="request">Request message.</param>
        /// <param name="context">Target context.</param>
        /// <returns>Task returning the response message.</returns>
        public new async Task<TResponse> SendAndReceiveAsync<TRequest, TResponse>(TRequest request, WebSocketContext context)
            => await base.SendAndReceiveAsync<TRequest, TResponse>(request, context);

        /// <summary>
        /// Sends a message to all connected clients.
        /// </summary>
        /// <typeparam name="T">Type of the message.</typeparam>
        /// <param name="message">Message to send.</param>
        public void BroadcastMessageAsync<T>(T message) => Parallel.ForEach(Clients, async context => await base.SendMessageAsync(message, context));

        /// <summary>
        /// Disposes all resources used by the server.
        /// Closes all the connections if not already closed.
        /// </summary>
        public void Dispose() => StopAsync().Wait();

        #endregion

        #region Helpers

        /// <summary>
        /// Called when a client is connected.
        /// </summary>
        /// <returns>Task completed when receiving loop is started.</returns>
        private async Task ClientConnectedAsync() {
            var httpListenerContext = await Listener.GetContextAsync();
            if (httpListenerContext.Request.IsWebSocketRequest) {
                HttpListenerWebSocketContext httpListenerWebSocketContext = null;
                httpListenerWebSocketContext = await httpListenerContext.AcceptWebSocketAsync(Codec.SubProtocol);
                var context = new WebSocketContext(httpListenerWebSocketContext);
                if (context.IsOpen) {
                    SessionProvider.OpenSession(context);
                    Clients.Add(context);
                    await StartReceiveAsync(context, CTS.Token, cleanUpAsync: ClientDisconnectedAsync);
                }
            }
        }

        /// <summary>
        /// Called when a client is disconnected. Closes, disposes and removes the disconnected client's socket.
        /// </summary>
        /// <param name="socket">Thread-safe WebSocket pack.</param>
        /// <returns>Task completed when the socket is closed, disposed and removed.</returns>
        private async Task ClientDisconnectedAsync(WebSocketContext socket) {
            SessionProvider.CloseSession(socket);
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CTS.Token);
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CTS.Token);
            socket.Dispose();
            Clients.Remove(socket);
        }

        /// <summary>
        /// Called when the exception occurs during connecting a client.
        /// </summary>
        /// <param name="exception"></param>
        protected virtual void ConnectException(Exception exception) { }


        #endregion

        #region Data fields

        private readonly List<WebSocketContext> Clients = new List<WebSocketContext>();
        private readonly Regex RxWS = new Regex(@"^ws", RegexOptions.Compiled);
        private HttpListener Listener;

        #endregion


    }

    #endregion

}