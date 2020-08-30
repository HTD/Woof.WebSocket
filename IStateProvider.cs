namespace Woof.WebSocket {

    /// <summary>
    /// Provides <see cref="SessionProvider"/> and <see cref="AuthenticationProvider"/> services.
    /// </summary>
    public interface IStateProvider {

        /// <summary>
        /// Provides session management for both client and server.
        /// </summary>
        public SessionProvider SessionProvider { get; }

        /// <summary>
        /// Gets or sets a module that allows ansynchronous authentication of the API key.
        /// </summary>
        public IAuthenticationProvider AuthenticationProvider { get; set; }

    }

}