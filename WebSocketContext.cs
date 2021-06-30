using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Woof.WebSocket {

    /// <summary>
    /// Thread-safe, extended WebSocket context.
    /// </summary>
    public class WebSocketContext : IDisposable {

        /// <summary>
        /// Gets a value indicating whether the underlying <see cref="System.Net.WebSockets.WebSocket"/> is open.
        /// </summary>
        public bool IsOpen => Socket.State == WebSocketState.Open;

        /// <summary>
        /// Gets the <see cref="HttpListenerWebSocketContext"/> if the socket was obtained from <see cref="System.Net.HttpListener"/>.
        /// </summary>
        public HttpListenerWebSocketContext? HttpContext { get; }

        /// <summary>
        /// Gets the server IP address and port number to which the request is directed.
        /// </summary>
        public IPEndPoint? LocalEndPoint { get; }

        /// <summary>
        /// Gets the client IP address and port number from which the request originated.
        /// </summary>
        public IPEndPoint? RemoteEndPoint { get; }

        /// <summary>
        /// Allows the remote endpoint to describe the reason why the connection was closed.
        /// </summary>
        public string? CloseStatusDescription => Socket.CloseStatusDescription;

        /// <summary>
        /// Indicates the reason why the remote endpoint initiated the close handshake.
        /// </summary>
        public WebSocketCloseStatus? CloseStatus => Socket.CloseStatus;

        /// <summary>
        /// Returns the current state of the WebSocket connection.
        /// </summary>
        public WebSocketState State => Socket.State;

        /// <summary>
        /// The subprotocol that was negotiated during the opening handshake.
        /// </summary>
        public string? SubProtocol => Socket.SubProtocol;

        /// <summary>
        /// Cretes the context from the <see cref="System.Net.WebSockets.WebSocket"/>.
        /// </summary>
        /// <param name="socket">Base socket.</param>
        public WebSocketContext(System.Net.WebSockets.WebSocket socket) {
            Socket = socket;
            Semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Creates the context from the <see cref="HttpListenerWebSocketContext"/>.
        /// </summary>
        /// <param name="httpContext">HTTP context.</param>
        public WebSocketContext(HttpListenerWebSocketContext httpContext, HttpListenerRequest request) {
            LocalEndPoint = request.LocalEndPoint;
            RemoteEndPoint = request.RemoteEndPoint;
            var xForwardedForHeader = httpContext.Headers["X-Forwarded-For"];
            if (xForwardedForHeader != null)
                LocalEndPoint = IPEndPoint.Parse(xForwardedForHeader);
            if (LocalEndPoint.Port < 1) LocalEndPoint.Port = 443;
            HttpContext = httpContext;
            Socket = HttpContext.WebSocket;
            Semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Aborts the WebSocket connection and cancels any pending IO operations.
        /// </summary>
        public void Abort() => Socket.Abort();

        /// <summary>
        /// Closes the WebSocket connection as an asynchronous operation using the close
        /// handshake defined in the WebSocket protocol specification section 7.
        /// </summary>
        /// <param name="closeStatus">Indicates the reason for closing the WebSocket connection.</param>
        /// <param name="statusDescription">Specifies a human readable explanation as to why the connection is closed.</param>
        /// <param name="cancellationToken">The token that can be used to propagate notification that operations should be canceled.</param>
        /// <returns>Task completed when connection is closed.</returns>
        public Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
            => Socket.CloseAsync(closeStatus, statusDescription, cancellationToken);

        /// <summary>
        /// Initiates or completes the close handshake defined in the WebSocket protocol specification section 7.
        /// </summary>
        /// <param name="closeStatus">Indicates the reason for closing the WebSocket connection.</param>
        /// <param name="statusDescription">Allows applications to specify a human readable explanation as to why the connection is closed.</param>
        /// <param name="cancellationToken">The token that can be used to propagate notification that operations should be canceled.</param>
        /// <returns>Task completed when the CLOSE frame is sent.</returns>
        public async Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) {
            await Semaphore.WaitAsync(cancellationToken);
            try {
                if (Socket.State == WebSocketState.Open || Socket.State == WebSocketState.CloseReceived)
                    await Socket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
            } finally {
                Semaphore.Release();
            }
        }

        /// <summary>
        /// Receives data from the <see cref="System.Net.WebSockets.WebSocket"/> connection asynchronously.
        /// </summary>
        /// <param name="buffer">References the application buffer that is the storage location for the received data.</param>
        /// <param name="cancellationToken">Propagates the notification that operations should be canceled.</param>
        /// <returns>Task returning received data.</returns>
        public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            => Socket.ReceiveAsync(buffer, cancellationToken);

        /// <summary>
        /// Sends data over the <see cref="System.Net.WebSockets.WebSocket"/> connection asynchronously.
        /// </summary>
        /// <param name="buffer">The buffer to be sent over the connection.</param>
        /// <param name="messageType">Indicates whether the application is sending a binary or text message.</param>
        /// <param name="endOfMessage">Indicates whether the data in "buffer" is the last part of a message.</param>
        /// <param name="cancellationToken">The token that propagates the notification that operations should be canceled.</param>
        /// <returns>Task completed when the sending is done.</returns>
        public async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) {
            await Semaphore.WaitAsync(cancellationToken); 
            try {    
                await Socket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
            }
            finally {
                Semaphore.Release();
            }
        }

        /// <summary>
        /// Sends multiple buffers over the <see cref="System.Net.WebSockets.WebSocket"/> connection asynchronously.<br/>
        /// All send operations will be called uninterrupted in one thread.
        /// </summary>
        /// <param name="buffers">The buffers to be sent over the connection.</param>
        /// <param name="messageType">Indicates whether the application is sending a binary or text message.</param>
        /// <param name="cancellationToken">The token that propagates the notification that operations should be canceled.</param>
        /// <returns>Task completed when the sending is done.</returns>
        public async Task SendAsync(IEnumerable<ArraySegment<byte>> buffers, WebSocketMessageType messageType, CancellationToken cancellationToken) {
            await Semaphore.WaitAsync(cancellationToken);
            try {
                var e = buffers.GetEnumerator();
                var isLast = !e.MoveNext();
                while (!isLast) {
                    var buffer = e.Current;
                    isLast = !e.MoveNext();
                    await Socket.SendAsync(buffer, messageType, isLast, cancellationToken);
                }
            }
            finally {
                Semaphore.Release();
            }
        }

        /// <summary>
        /// Disposes the semaphore and the socket.
        /// </summary>
        public void Dispose() {
            Semaphore.Dispose();
            Socket.Dispose();
            GC.SuppressFinalize(this);
        }

        #region Data fields

        /// <summary>
        /// The actual <see cref="System.Net.WebSockets.WebSocket"/>.
        /// </summary>
        private readonly System.Net.WebSockets.WebSocket Socket;

        /// <summary>
        /// The <see cref="SemaphoreSlim"/> used to limit the access to the socket for the one thread.<br/>
        /// It should be awaited before each send operation and released after the operation completes.
        /// </summary>
        private readonly SemaphoreSlim Semaphore;

        #endregion

    }

}