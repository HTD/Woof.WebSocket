using System;
using System.Threading.Tasks;

using Woof.WebSocket.Test.Api;
using Woof.WebSocket.WoofSubProtocol;

namespace Woof.WebSocket.Test.Client {

    /// <summary>
    /// Test client designed as a part of package documentation.
    /// </summary>
    public class TestClient : Client<WoofCodec> {

        /// <summary>
        /// Initializes the test server instance.
        /// </summary>
        public TestClient() {
            EndPointUri = Api.Properties.EndPointUri;
            Codec.LoadMessageTypes();
        }

        /// <summary>
        /// Performs asynchronous signing with an API key and the secret.
        /// If the authentication passes, an authorized client session is started.
        /// </summary>
        /// <param name="apiKey">API key.</param>
        /// <param name="apiSecret">API secret.</param>
        /// <returns>Task returning true if key and the secret are valid.</returns>
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

        /// <summary>
        /// Ends the authorized client session.
        /// </summary>
        /// <returns></returns>
        public async Task<DateTime> SignOutAsync()
            => (await SendAndReceiveAsync<SignOutRequest, SignOutResponse>(new SignOutRequest { ClientTime = DateTime.Now })).ServerTime;

        /// <summary>
        /// Performs a minimal request (no payload) and awaits a minimal response.
        /// </summary>
        /// <returns>Task completed when a response to a minimal request is received.</returns>
        public async Task PingAsync()
            => await SendAndReceiveAsync<PingRequest, PingResponse>(new PingRequest());

        /// <summary>
        /// Performs a test division of two decimal numbers.
        /// </summary>
        /// <param name="x">Divident.</param>
        /// <param name="y">Divisor.</param>
        /// <returns>Taks returning a decimal result.</returns>
        /// <exception cref="UnexpectedMessageException">Thrown when the operation fails.</exception>
        public async Task<decimal> DivideAsync(decimal x, decimal y)
            => (await SendAndReceiveAsync<DivideRequest, DivideResponse>(new DivideRequest { X = x, Y = y })).Result;

        /// <summary>
        /// Performs a test private (authorized) request and returns a test value.
        /// </summary>
        /// <returns>Task returning string "AUTHORIZED" if successful.</returns>
        /// <exception cref="UnexpectedMessageException">Thrown when unauthorized, should contain <see cref="AccessDeniedResponse"/> message.</exception>
        public async Task<string> CheckAuthorizedAsync()
            => (await SendAndReceiveAsync<PrivateRequest, PrivateResponse>(new PrivateRequest { ClientTime = DateTime.Now })).Secret;

        /// <summary>
        /// Subsribes to server time notification.
        /// </summary>
        /// <param name="period">Time period between notifications.</param>
        /// <returns>Task completed as soon as the subscription message is sent.</returns>
        public async Task TimeSubscribeAsync(TimeSpan period = default)
            => await SendMessageAsync(new TimeSubscribeRequest { Period = period == default ? TimeSpan.FromSeconds(1) : period });

    }

}