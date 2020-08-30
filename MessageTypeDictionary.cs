using System;
using System.Collections.Generic;
using System.Linq;

namespace Woof.WebSocket {

    /// <summary>
    /// Message type dictionary used to resolve message type from type identifier read from message header.
    /// </summary>
    /// <typeparam name="TTypeIndex">Message type index type.</typeparam>
    public class MessageTypeDictionary<TTypeIndex> : Dictionary<TTypeIndex, MessageTypeContext> { 
    
        /// <summary>
        /// Gets the type identifier for the specified type if defined.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <returns>Type identifier.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the type does not exists in the dictionary.</exception>
        public TTypeIndex GetTypeId<TMessage>() => this.First(pair => pair.Value.MessageType == typeof(TMessage)).Key;
    
    }

    /// <summary>
    /// Message type dictionary used to resolve message type from type identifier read from message header.
    /// </summary>
    public class MessageTypeDictionary : MessageTypeDictionary<int> { }

}