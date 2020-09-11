using System;

namespace Woof.WebSocket.Test.Api {

    public class Session : ISession {

        public Session() => Guid = Guid.NewGuid();

        public WebSocketContext Context { get; set; }

        public Guid Guid { get; set; }

        public string ApiKey { get; set; }

        public byte[] Key { get; set; }

    }

}