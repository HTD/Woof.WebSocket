using System;
using System.Threading.Tasks;

using Woof.WebSocket.Test.Api;

namespace Woof.WebSocket.Test.Client {

    public class TestClient : Woof.WebSocket.Client {

        public TestClient() {
            EndPointUri = Api.Properties.EndPointUri;
            Codec.LoadMessageTypes();
            MessageReceived += TestClient_MessageReceived;
        }

        private void TestClient_MessageReceived(object sender, MessageReceivedEventArgs e) {
            switch (e.DecodeResult.Message) {
                case TimeNotification timeNotification: Console.WriteLine($"SERVER TIME: {timeNotification.Time}"); break;
            }
        }

        public async Task PingAsync()
            => await SendAndReceiveAsync<EmptyRequest, EmptyResponse>(new EmptyRequest());

        public async Task<string> HelloAsync(string name)
            => (await SendAndReceiveAsync<HelloRequest, HelloResponse>(new HelloRequest { Name = name })).MessageText;

        public async Task SubscribeAsync(string name, TimeSpan period = default)
            => await SendMessageAsync(new SubscribeRequest { Name = name, Period = period == default ? TimeSpan.FromSeconds(1) : period });

        public async Task<bool> SingInAsync(string apiKey, string apiSecret) {
            var apiKeyData = Codec.GetKey(apiKey);
            var apiSecretData = Codec.GetKey(apiSecret);
            var request = new SignInRequest { ApiKey = apiKeyData };
            var session = SessionProvider.GetSession<ClientSession>();
            session.Key = apiSecretData;
            var response = await SendAndReceiveAsync<SignInRequest, SignInResponse>(request);
            if (response?.IsSuccess == true) return true;
            session.Key = null;
            return false;
        }

        public async Task<DateTime> SignOutAsync()
            => (await SendAndReceiveAsync<SignOutRequest, SignOutResponse>(new SignOutRequest { ClientTime = DateTime.Now })).ServerTime;

        public async Task<string> AskServerAsync(string question)
            => (await SendAndReceiveAsync<AuthenticatedRequest, AuthenticatedResponse>(new AuthenticatedRequest { Question = question })).Answer;

    }

}