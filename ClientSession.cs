namespace Woof.WebSocket {

    /// <summary>
    /// Basic client session data, provides secret key for the client.
    /// </summary>
    public class ClientSession : ISession {

        /// <summary>
        /// Gets a WebSocket context for the session.
        /// </summary>
        public WebSocketContext? Context { get; set; }

        /// <summary>
        /// Gets or sets a message signing key for the session.
        /// </summary>
        public byte[]? Key { get; set; }

        
    }

}