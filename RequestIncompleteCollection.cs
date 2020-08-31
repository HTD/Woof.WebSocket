using System;
using System.Collections.Generic;

namespace Woof.WebSocket {

    #region WOOF version

    /// <summary>
    /// A collection used for matching response messages to the request messages.
    /// </summary>
    internal class RequestIncompleteCollection : RequestIncompleteCollection<Guid, int> {
        
        /// <summary>
        /// Creates a new instance for codec.
        /// </summary>
        /// <param name="codec">Codec instance.</param>
        public RequestIncompleteCollection(SubProtocolCodec<Guid, int> codec) : base(codec) { }
    
    }

    #endregion

    #region Generic version

    /// <summary>
    /// A collection used for matching response messages to the request messages.
    /// </summary>
    /// <typeparam name="TTypeIndex">Message type index type.</typeparam>
    /// <typeparam name="TMessageId">Message identifier type.</typeparam>
    public class RequestIncompleteCollection<TTypeIndex, TMessageId> : Dictionary<TMessageId, ResponseSynchronizer>, IDisposable {

        /// <summary>
        /// Gets the subprotocol codec.
        /// </summary>
        protected SubProtocolCodec<TTypeIndex, TMessageId> Codec { get; }

        /// <summary>
        /// Gets a new response pack with a new identifier.
        /// </summary>
        public (TMessageId, ResponseSynchronizer) NewResponseSynchronizer {
            get {
                var id = Codec.NewId;
                var synchronizer = new ResponseSynchronizer();
                this[id] = synchronizer;
                return (id, synchronizer);
            }
        }

        /// <summary>
        /// Creates the collection for the specified codec.
        /// </summary>
        /// <param name="codec">Subprotocol codec.</param>
        public RequestIncompleteCollection(SubProtocolCodec<TTypeIndex, TMessageId> codec) => Codec = codec;

        /// <summary>
        /// Try to remove the response synchronizer pointed with <paramref name="id"/> from the collection and return it.
        /// </summary>
        /// <param name="id">Message identifier.</param>
        /// <param name="responseSynchronizer">Response synchronizer.</param>
        /// <returns>True, if the response synchronizer was stored in the collection.</returns>
        public bool TryRemoveResponseSynchronizer(TMessageId id, out ResponseSynchronizer responseSynchronizer) {
            if (ContainsKey(id)) {
                responseSynchronizer = this[id];
                Remove(id);
                return true;
            }
            responseSynchronizer = null;
            return false;
        }

        /// <summary>
        /// Disposes all disposable values inside the collection.
        /// </summary>
        public void Dispose() {
            foreach (var i in Values) i.Dispose();
            Clear();
        }

    }

    #endregion

}