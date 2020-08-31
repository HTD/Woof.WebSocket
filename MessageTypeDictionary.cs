using System;
using System.Collections.Generic;
using System.Linq;

namespace Woof.WebSocket {

    /// <summary>
    /// Message type dictionary used to resolve message type from type identifier read from message header.
    /// </summary>
    public class MessageTypeDictionary : Dictionary<int, MessageTypeContext> { 
    
        /// <summary>
        /// Gets the type identifier for the specified type if defined.
        /// </summary>
        /// <returns>Type identifier.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the type does not exists in the dictionary.</exception>
        public int GetTypeId<TMessage>() => this.First(pair => pair.Value.MessageType == typeof(TMessage)).Key;
    
    }

}