using System;
using System.Threading.Tasks;

using Woof.WebSocket.Test.Api;

namespace Woof.WebSocket.Test.Server {

    /// <summary>
    /// Test server designed as a part of package documentation.
    /// </summary>
    class TestServer : Server<WoofCodec> {

        /// <summary>
        /// Initializes the test server instance.
        /// </summary>
        public TestServer() {
            AuthenticationProvider = new TestAuthenticationProvider();
            EndPointUri = Api.Properties.EndPointUri;
            Codec.LoadMessageTypes(); // IMPORTANT: IT MUSTN'T BE CALLED UNLESS AT LEAST ONE API ASSEMBLY MEMBER WAS NOT TOUCHED!
        }

        /// <summary>
        /// Handles MessageReceived events.<br/>
        /// Note that since the function is "async void" it should not throw because such exceptions can't be caught.
        /// </summary>
        /// <param name="decodeResult">Message receive result.</param>
        /// <param name="context">WebSocket (client) context.</param>
        protected override async void OnMessageReceived(DecodeResult decodeResult, WebSocketContext context) {
            if (decodeResult.IsUnauthorized) {
                await SendMessageAsync(new AccessDeniedResponse(), context, decodeResult.MessageId);
                return;
            }
            switch (decodeResult.Message) {
                case SignInRequest signInRequest:
                    var session = SessionProvider.GetSession<Session>(context);
                    session.Key = await AuthenticationProvider.GetKeyAsync(signInRequest.ApiKey);
                    await SendMessageAsync(new SignInResponse { IsSuccess = decodeResult.IsSignatureValid && session.Key != null }, context, decodeResult.MessageId);
                    break;
                case SignOutRequest signOutRequest:
                    SessionProvider.CloseSession(context);
                    await SendMessageAsync(new SignOutResponse { ServerTime = DateTime.Now }, context, decodeResult.MessageId);
                    break;
                case PingRequest pingRequest:
                    await SendMessageAsync(new PingResponse(), context, decodeResult.MessageId);
                    break;
                case DivideRequest divideRequest:
                    try {
                        var result = divideRequest.X / divideRequest.Y;
                        await SendMessageAsync(new DivideResponse { Result = result }, context, decodeResult.MessageId);
                    }
                    catch (Exception divideException) {
                        await SendMessageAsync(new ErrorResponse { Code = 400, Description = divideException.Message }, context, decodeResult.MessageId);
                    }
                    break;
                case PrivateRequest privateRequest:
                    await SendMessageAsync(new PrivateResponse { Secret = "AUTHORIZED" }, context, decodeResult.MessageId);
                    break;
                case TimeSubscribeRequest subscribeRequest:
                    await AsyncLoop.FromIterationAsync(async () => {
                        var boxedMsg = new TimeNotification { Time = DateTime.Now };
                        await SendMessageAsync(boxedMsg, typeHint: null, context);
                        await Task.Delay(subscribeRequest.Period);
                    }, CancellationToken, OnReceiveException, () => context.IsOpen);
                    break;
            }
        }

    }

}