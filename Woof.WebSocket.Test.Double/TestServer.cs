using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Woof.WebSocket.Test.Api;

namespace Woof.WebSocket.Test.DoubleServer {

    /// <summary>
    /// Test server designed as a part of package documentation.
    /// </summary>
    class TestServer : Server<WoofCodec> {

        private int IgnoreMessagesCount { get; set; }

        private TimeSpan LagTime { get; set; }

        /// <summary>
        /// Initializes the test server instance.
        /// </summary>
        public TestServer(Uri endPointUri) {
            EndPointUri = endPointUri;
            Assembly.Load("Woof.WebSocket.Test.Api");
            Codec.LoadMessageTypes(); // IMPORTANT: IT MUSTN'T BE CALLED UNLESS AT LEAST ONE API ASSEMBLY MEMBER WAS NOT TOUCHED!
        }

        /// <summary>
        /// Handles MessageReceived events.<br/>
        /// Note that since the function is "async void" it should not throw because such exceptions can't be caught.
        /// </summary>
        /// <param name="decodeResult">Message receive result.</param>
        /// <param name="context">WebSocket (client) context.</param>
        protected override async void OnMessageReceived(DecodeResult decodeResult, WebSocketContext context) {
            if (IgnoreMessagesCount > 0) {
                IgnoreMessagesCount--;
                return;
            }
            if (LagTime > TimeSpan.Zero) await Task.Delay(LagTime);
            if (decodeResult.IsUnauthorized) {
                await SendMessageAsync(new AccessDeniedResponse(), context, decodeResult.MessageId);
                return;
            }
            switch (decodeResult.Message) {
                case GetUriRequest:
                    await SendMessageAsync<GetUriResponse>(new GetUriResponse { RequestUri = context.HttpContext.RequestUri.ToString() }, context, decodeResult.MessageId);
                    break;
            }
        
        }

    }

}