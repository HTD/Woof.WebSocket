using System;
using System.Threading.Tasks;

using Woof.WebSocket.Test.Api;

namespace Woof.WebSocket.Test.Server {

    class TestServer : Woof.WebSocket.Server {

        public TestServer() {
            EndPointUri = Api.Properties.EndPointUri;
            Codec.LoadMessageTypes();
            AuthenticationProvider = new TestAuthenticationProvider();
        }

        protected override void StateChanged(ServiceState state)
            => Console.WriteLine($"SERVER STATE CHANGED: {state}");

        protected override async Task MessageReceivedAsync(DecodeResult<int, Guid> decodeResult, WebSocketContext context, Guid guid) {
            if (decodeResult.IsUnauthorized) {
                await SendMessageAsync(new AccessDeniedResponse(), context, guid);
                return;
            }
            switch (decodeResult.Message) {
                case HelloRequest helloRequest:
                    await SendMessageAsync(new HelloResponse { MessageText = $"Hello, {helloRequest.Name}!" }, context, guid);
                    break;
                case SubscribeRequest subscribeRequest:
                    switch (subscribeRequest.Name) {
                        case "time":
                            await AsyncLoop.FromIterationAsync(async () => {
                                await SendMessageAsync(new TimeNotification { Time = DateTime.Now }, context);
                                await Task.Delay(subscribeRequest.Period);
                            }, CancellationToken, ReceiveException, () => context.IsOpen);
                            break;
                    }
                    break;
                case SignInRequest signInRequest:
                    var session = SessionProvider.GetSession<ApiClientSession>(context);
                    session.Key = await AuthenticationProvider.GetKeyAsync(signInRequest.ApiKey);
                    await SendMessageAsync(new SignInResponse { IsSuccess = decodeResult.IsSignatureValid && session.Key != null }, context, guid);
                    break;
                case SignOutRequest signOutRequest:
                    SessionProvider.CloseSession(context);
                    await SendMessageAsync(new SignOutResponse { ServerTime = DateTime.Now }, context, guid);
                    break;
                case AuthenticatedRequest authenticatedRequest:
                    await SendMessageAsync(new AuthenticatedResponse {
                        Answer = decodeResult.IsSignatureValid
                        ? $"{authenticatedRequest.Question} 42?"
                        : "GET LOST!"
                    }, context, guid);
                    break;
                case EmptyRequest emptyRequest:
                    await SendMessageAsync(new EmptyResponse(), context, guid);
                    break;
            }
        }

        protected override void ReceiveException(Exception exception) {
            while (exception.InnerException != null) exception = exception.InnerException;
            Console.WriteLine($"SERVER EXCEPTION: {exception.Message}");
        }

    }

}