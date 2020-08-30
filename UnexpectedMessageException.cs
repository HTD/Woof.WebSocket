using System;
using System.Collections.Generic;
using System.Text;

namespace Woof.WebSocket {
    
    public class UnexpectedMessageException : Exception {

        public UnexpectedMessageException(object message) : base($"{message?.GetType()?.Name ?? "null"} received") => Message = message;

        public new object Message { get; set; }

    }

}