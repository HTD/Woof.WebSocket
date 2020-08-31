using System;
using System.Threading.Tasks;

using Woof.WebSocket.Test.Api;

namespace Woof.WebSocket.Test.Server {

    class TestServer : Woof.WebSocket.Server {

        public TestServer() {
            EndPointUri = Api.Properties.EndPointUri;
            Codec.LoadMessageTypes();
            AuthenticationProvider = new TestAuthenticationProvider();
            MessageReceived += TestServer_MessageReceived;
        }

        private async void TestServer_MessageReceived(object sender, MessageReceivedEventArgs<int, Guid> e) {
            if (e.DecodeResult.IsUnauthorized) {
                await SendMessageAsync(new AccessDeniedResponse(), e.Context, e.DecodeResult.Id);
                return;
            }
            switch (e.Message) {
                case HelloRequest helloRequest:
                    await SendMessageAsync(new HelloResponse { MessageText = $"Hello, {helloRequest.Name}!" }, e.Context, e.DecodeResult.Id);
                    break;
                case SubscribeRequest subscribeRequest:
                    switch (subscribeRequest.Name) {
                        case "time":
                            await AsyncLoop.FromIterationAsync(async () => {
                                await SendMessageAsync(new TimeNotification { Time = DateTime.Now }, e.Context);
                                await Task.Delay(subscribeRequest.Period);
                            }, CancellationToken, OnReceiveException, () => e.Context.IsOpen);
                            break;
                    }
                    break;
                case SignInRequest signInRequest:
                    var session = SessionProvider.GetSession<ApiClientSession>(e.Context);
                    session.Key = await AuthenticationProvider.GetKeyAsync(signInRequest.ApiKey);
                    await SendMessageAsync(new SignInResponse { IsSuccess = e.DecodeResult.IsSignatureValid && session.Key != null }, e.Context, e.DecodeResult.Id);
                    break;
                case SignOutRequest signOutRequest:
                    SessionProvider.CloseSession(e.Context);
                    await SendMessageAsync(new SignOutResponse { ServerTime = DateTime.Now }, e.Context, e.DecodeResult.Id);
                    break;
                case AuthenticatedRequest authenticatedRequest:
                    await SendMessageAsync(new AuthenticatedResponse {
                        Answer = e.DecodeResult.IsSignatureValid
                        ? $"{authenticatedRequest.Question} 42?"
                        : "GET LOST!"
                    }, e.Context, e.DecodeResult.Id);
                    break;
                case EmptyRequest emptyRequest:
                    await SendMessageAsync(new EmptyResponse(), e.Context, e.DecodeResult.Id);
                    break;
            }
        }


        
        

    }

}