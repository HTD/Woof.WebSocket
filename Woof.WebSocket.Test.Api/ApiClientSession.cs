using System;

namespace Woof.WebSocket.Test.Api {

    public class ApiClientSession : ISession {

        public ApiClientSession() => Guid = Guid.NewGuid();

        public Guid Guid { get; set; }

        public string ApiKey { get; set; }

        public byte[] Key { get; set; }

    }

}