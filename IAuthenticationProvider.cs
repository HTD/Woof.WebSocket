using System.Threading.Tasks;

namespace Woof.WebSocket {

    /// <summary>
    /// Implement this to allow ansynchronous authentication of the API key.
    /// </summary>
    public interface IAuthenticationProvider {

        /// <summary>
        /// Gets the message signing key from the API key.
        /// </summary>
        /// <param name="apiKey">API key.</param>
        /// <returns>Message signing key or null if not authenticated.</returns>
        public Task<byte[]> GetKeyAsync(byte[] apiKey);

    }

}