using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;

namespace Woof.WebSocket {

    /// <summary>
    /// WebSocket transport base class to be used by both clients and servers.
    /// </summary>
    /// <typeparam name="TCodec">Message codec implementing the subprotocol.</typeparam>
    public abstract class WebSocketTransport<TCodec> : IStateProvider  where TCodec : SubProtocolCodec, new() {

        #region Public API

        #region Events

        /// <summary>
        /// Occurs when a message is received by the socket.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        
        /// <summary>
        /// Occurs when an exception is thrown during receive process.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ReceiveException;

        /// <summary>
        /// Occurs when the state of the client or server service is changed.
        /// </summary>
        public event EventHandler<StateChangedEventArgs> StateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the module providing session management for both client and server.
        /// </summary>
        public SessionProvider SessionProvider { get; } = new SessionProvider();

        /// <summary>
        /// A module providing API key authentication asynchronous method.<br/>
        /// <see cref="IAuthenticationProvider"/> implementation is necessary for built-in API key authentication support.<br/>
        /// <strong>This is not set by default.</strong>
        /// </summary>
        public IAuthenticationProvider AuthenticationProvider { get; set; }

        /// <summary>
        /// Gets the current client or server state.
        /// </summary>
        public ServiceState State { get; set; } = ServiceState.Stopped;

        #endregion

        /// <summary>
        /// Initializes the transport with the codec and request completion instances.
        /// </summary>
        public WebSocketTransport() {
            Codec = new TCodec { State = this };
            RequestsIncomplete = new RequestIncompleteCollection(Codec);
        }

        #endregion

        #region Protected state

        /// <summary>
        /// Gets the cancellation token used for the client or server instance.
        /// </summary>
        protected CancellationToken CancellationToken => CTS?.Token ?? CancellationToken.None;

        /// <summary>
        /// Gets the subprotocol codec.
        /// </summary>
        protected TCodec Codec { get; }

        /// <summary>
        /// Gets the WebSocket end point URI.
        /// </summary>
        protected Uri EndPointUri { get; set; }

        /// <summary>
        /// A collection of incomplete requests requiring the other party's response.
        /// </summary>
        protected RequestIncompleteCollection RequestsIncomplete { get; }

        #endregion

        #region Override

        /// <summary>
        /// Gets the limit of the message size that can be received (default 1MB).<br/>
        /// Override to zero or a negative number to remove the limitation (unsafe).<br/>
        /// </summary>
        public virtual int MaxReceiveMessageSize => 0x00100000; // 1MB

        /// <summary>
        /// Invokes <see cref="MessageReceived"/> event.
        /// </summary>
        /// <param name="decodeResult">Message decoding result.</param>
        /// <param name="context">WebSocket context, that can be used to send the response to.</param>
        /// <returns>Task completed when the message handling is done.</returns>
        protected virtual void OnMessageReceived(DecodeResult decodeResult, WebSocketContext context)
            => MessageReceived?.Invoke(this, new MessageReceivedEventArgs(decodeResult, context));

        /// <summary>
        /// Invokes <see cref="ReceiveException"/> event.
        /// </summary>
        /// <param name="exception">Exception passed.</param>
        protected virtual void OnReceiveException(Exception exception)
            => ReceiveException?.Invoke(this, new ExceptionEventArgs(exception));

        /// <summary>
        /// Invokes <see cref="StateChanged"/> event.
        /// </summary>
        protected virtual void OnStateChanged(ServiceState state)
            => StateChanged?.Invoke(this, new StateChangedEventArgs(state));

        #endregion

        #region Transport tools

        /// <summary>
        /// Reads binary messages from the socket, deserializes them and triggers <see cref="MessageReceived"/> event.
        /// </summary>
        /// <param name="context">WebSocket context.</param>
        /// <returns>Receive loop task.</returns>
        private async Task Receive(WebSocketContext context) {
            if (State != ServiceState.Started) {
                State = ServiceState.Started;
                _ = Task.Run(() => OnStateChanged(State));
            }
            while (!CancellationToken.IsCancellationRequested && context.IsOpen) {
                try {
                    var decodeResult = await Codec.DecodeMessageAsync(context, CancellationToken, MaxReceiveMessageSize);
                    if (decodeResult.IsCloseFrame) break;
                    if (RequestsIncomplete.TryRemoveResponseSynchronizer(decodeResult.MessageId, out var responseSynchronizer)) {
                        responseSynchronizer.Message = decodeResult.Message;
                        responseSynchronizer.Semaphore.Release();
                    }
                    else if (decodeResult.TypeContext?.IsError == true && decodeResult.MessageId == default) {
                        RequestsIncomplete.Dispose(); // emergency release of incomplete requests on unmatched server error messages
                    }
                    else {
                        _ = Task.Run(() => OnMessageReceived(decodeResult, context));
                    }
                }
                catch (Exception exception) {
                    if (exception is TaskCanceledException) throw;
                    if (exception is WebSocketException wsx && wsx.InnerException is TaskCanceledException) throw;
                    _ = Task.Run(() => OnReceiveException(exception));
                }
            }
        }

        /// <summary>
        /// Starts the receiving task for the specified context.
        /// </summary>
        /// <param name="context">WebSocket context.</param>
        /// <param name="token">Cancellation token used to cancel the task created.</param>
        /// <param name="cleanUpAsync">Optional asynchronous clean up function executed after the receiving loop is ended.</param>
        /// <returns>Created task.</returns>
        protected async Task StartReceiveAsync(WebSocketContext context, CancellationToken token, Func<WebSocketContext, Task> cleanUpAsync = null) {
            if (cleanUpAsync is null)
                await Task.Factory.StartNew(
                    async() => await Receive(context),
                    token, TaskCreationOptions.LongRunning, TaskScheduler.Default
                );
            else
                await Task.Factory.StartNew(
                    async () => {
                        await Receive(context);
                        await cleanUpAsync(context);
                    },
                    token, TaskCreationOptions.LongRunning, TaskScheduler.Default
                );
        }

        /// <summary>
        /// Serializes and sends a message to the specified context.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="message">Message to send.</param>
        /// <param name="context">Target context.</param>
        /// <param name="id">Optional message identifier, if not set - new unique identifier will be used.</param>
        /// <returns>Task completed when the sending is done.</returns>
        protected async Task SendMessageAsync<TMessage>(TMessage message, WebSocketContext context, Guid id = default)
            => await Codec.EncodeMessageAsync(context, CancellationToken, message, id);

        /// <summary>
        /// Sends a message to the specified context and awaits until the response of the specified type is received.
        /// </summary>
        /// <typeparam name="TRequest">Request message type.</typeparam>
        /// <typeparam name="TResponse">Response message type.</typeparam>
        /// <param name="request">Request message.</param>
        /// <param name="context">Target context.</param>
        /// <returns>Task returning the response message.</returns>
        /// <exception cref="UnexpectedMessageException">Thrown when a defined, but unexpected type message is received instead of expected one.</exception>
        /// <exception cref="TaskCanceledException">Thrown when the client or server operation is cancelled.</exception>
        protected async Task<TResponse> SendAndReceiveAsync<TRequest, TResponse>(TRequest request, WebSocketContext context) {
            var (id, synchronizer) = RequestsIncomplete.NewResponseSynchronizer;
            try {
                await SendMessageAsync(request, context, id);
                await synchronizer.Semaphore.WaitAsync();
                if (synchronizer.Message is TResponse response) return response;
                else throw new UnexpectedMessageException(synchronizer.Message);
            }
            finally {
                synchronizer.Dispose();
            }
        }

        #endregion

        #region Data fields

        /// <summary>
        /// A cancellation token source used to cancel all the client and server tasks.
        /// </summary>
        protected CancellationTokenSource CTS;

        #endregion

    }

}