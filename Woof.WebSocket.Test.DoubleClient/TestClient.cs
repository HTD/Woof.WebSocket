using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Woof.WebSocket.Test.Api;

namespace Woof.WebSocket.Test.DoubleClient {

    /// <summary>
    /// Test client designed as a part of package documentation.
    /// </summary>
    public class TestClient : Client<WoofCodec> {

        /// <summary>
        /// Initializes the test server instance.
        /// </summary>
        public TestClient(Uri endPointUri) {
            EndPointUri = endPointUri;
            Assembly.Load("Woof.WebSocket.Test.Api");
            Codec.LoadMessageTypes();
            Timeout = TimeSpan.FromSeconds(2);
        }

        public async Task<string> GetUriAsync() => (await SendAndReceiveAsync<GetUriRequest, GetUriResponse>(new GetUriRequest())).RequestUri;

    }

}