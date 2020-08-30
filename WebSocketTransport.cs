using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;

namespace Woof.WebSocket {

    /// <summary>
    /// WebSocket transport base class to be used by both clients and servers.
    /// </summary>
    /// <typeparam name="TMessageId">Message identifier type.</typeparam>
    /// <typeparam name="TTypeIndex">Message type index type.</typeparam>
    public abstract class WebSocketTransport<TTypeIndex, TMessageId, TCodec> : IStateProvider  where TCodec : SubProtocolCodec<TTypeIndex, TMessageId>, new() {

        #region Public API

        public WebSocketTransport() {
            Codec = new TCodec { State = this };
            RequestsIncomplete = new RequestIncompleteCollection<TTypeIndex, TMessageId>(Codec);
        }

        /// <summary>
        /// Gets the cancellation token used for the client or server instance.
        /// </summary>
        public CancellationToken CancellationToken => CTS?.Token ?? CancellationToken.None;

        /// <summary>
        /// Gets the current client or server state.
        /// </summary>
        public ServiceState State { get; protected set; } = ServiceState.Stopped;

        #endregion

        #region Implement

        /// <summary>
        /// Gets the subprotocol codec.
        /// </summary>
        protected TCodec Codec { get; }

        /// <summary>
        /// A collection of incomplete requests, that require the other party's response.
        /// </summary>
        protected RequestIncompleteCollection<TTypeIndex, TMessageId> RequestsIncomplete { get; }

        #endregion

        #region Override

        /// <summary>
        /// Gets the WebSocket end point URI.
        /// </summary>
        protected Uri EndPointUri { get; set; }

        /// <summary>
        /// Gets the limit of the message size that can be received (default 1MB).<br/>
        /// Override to zero or a negative number to remove the limitation (unsafe).<br/>
        /// </summary>
        public virtual int MaxReceiveMessageSize => 0x00100000; // 1MB

        public SessionProvider SessionProvider { get; } = new SessionProvider();

        public IAuthenticationProvider AuthenticationProvider { get; set; }

        /// <summary>
        /// Implementing class must provide the asynchronous code to handle the message received event.
        /// </summary>
        /// <param name="decodeResult">Message decoding result.</param>
        /// <param name="context">WebSocket context, that can be used to send the response to.</param>
        /// <param name="id">Message identifier received.</param>
        /// <returns>Task completed when the message handling is done.</returns>
        protected virtual Task MessageReceivedAsync(DecodeResult<TTypeIndex, TMessageId> decodeResult, WebSocketContext context, TMessageId id) => Task.CompletedTask;

        /// <summary>
        /// Implementing class must provide the asynchronous code to handle the message received event.
        /// </summary>
        /// <param name="decodeResult">Message decoding result.</param>
        /// <param name="context">WebSocket context, that can be used to send the response to.</param>
        /// <returns>Task completed when the message handling is done.</returns>
        protected virtual void MessageReceived(DecodeResult<TTypeIndex, TMessageId> decodeResult, WebSocketContext context, TMessageId id) { }

        /// <summary>
        /// Implementing class may provide its own exception handler for the exceptions that occur during message receiving.
        /// </summary>
        /// <param name="exception">Exception passed.</param>
        protected virtual void ReceiveException(Exception exception) { }

        /// <summary>
        /// Implementing class may provide some code to react to the client or server state changed.
        /// </summary>
        protected virtual void StateChanged(ServiceState state) { }

        #endregion

        #region Transport tools

        /// <summary>
        /// Reads binary messages from the socket, deserializes them and calls <see cref="MessageReceived(object, WebSocketContext, Guid)"/>.
        /// </summary>
        /// <param name="context">WebSocket context.</param>
        /// <returns>Receive loop task.</returns>
        private async Task Receive(WebSocketContext context) {
            if (State != ServiceState.Started) {
                State = ServiceState.Started;
                StateChanged(State);
            }
            while (!CancellationToken.IsCancellationRequested && context.IsOpen) {
                try {
                    var decodeResult = await Codec.DecodeMessageAsync(context, CancellationToken, MaxReceiveMessageSize); // FIXME: Access denied exceptions!
                    if (decodeResult.IsCloseFrame) break;
                    if (RequestsIncomplete.TryRemoveResponseSynchronizer(decodeResult.Id, out var responseSynchronizer)) {
                        responseSynchronizer.Message = decodeResult.Message;
                        responseSynchronizer.Semaphore.Release();
                    }
                    else {
                        MessageReceived(decodeResult, context, decodeResult.Id);
                        await MessageReceivedAsync(decodeResult, context, decodeResult.Id);
                    }
                }
                catch (Exception exception) {
                    if (exception is TaskCanceledException) throw;
                    if (exception is WebSocketException wsx && wsx.InnerException is TaskCanceledException) throw;
                    ReceiveException(exception);
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
                    CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default
                );
            else
                await Task.Factory.StartNew(
                    async () => {
                        await Receive(context);
                        await cleanUpAsync(context);
                    },
                    CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default
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
        protected async Task SendMessageAsync<TMessage>(TMessage message, WebSocketContext context, TMessageId id = default)
            => await Codec.EncodeMessageAsync(context, CancellationToken, message, id);

        /// <summary>
        /// Sends a message to the specified context and awaits until the response of the specified type is received.
        /// </summary>
        /// <typeparam name="TRequest">Request message type.</typeparam>
        /// <typeparam name="TResponse">Response message type.</typeparam>
        /// <param name="request">Request message.</param>
        /// <param name="context">Target context.</param>
        /// <returns>Task returning the response message.</returns>
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