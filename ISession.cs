namespace Woof.WebSocket {

    /// <summary>
    /// A session having a message signing key.
    /// </summary>
    public interface ISession {

        /// <summary>
        /// Gets or sets a message signing key for the session.
        /// </summary>
        public byte[] Key { get; set; }

    }

}