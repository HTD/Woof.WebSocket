namespace Woof.WebSocket {

    /// <summary>
    /// A session having a message signing key.
    /// </summary>
    public interface ISession {

        /// <summary>
        /// Gets a WebSocket context for the session.
        /// </summary>
        WebSocketContext Context { get; set; }

        /// <summary>
        /// Gets a message signing key for the session.
        /// </summary>
        byte[] Key { get; set; }

    }

}