using System;
using System.Data;

using ProtoBuf;

using Woof.WebSocket.WoofSubProtocol;

namespace Woof.WebSocket.Test.Api {

    [Message(1, IsSigned = true), ProtoContract]
    public class SignInRequest : ISignInRequest {

        [ProtoMember(1)]
        public byte[] ApiKey { get; set; }

    }

    [Message(2), ProtoContract]
    public class SignInResponse {

        [ProtoMember(1)]
        public bool IsSuccess { get; set; }

    }

    [Message(3), ProtoContract]
    public class SignOutRequest {

        [ProtoMember(1)]
        public DateTime ClientTime { get; set; }

    }

    [Message(4), ProtoContract]
    public class SignOutResponse {

        [ProtoMember(1)]
        public DateTime ServerTime { get; set; }

    }

    [Message(5), ProtoContract]
    public class AccessDeniedResponse { }

    [Message(6), ProtoContract]
    public class ErrorResponse {

        [ProtoMember(1)]
        public int Code { get; set; }

        [ProtoMember(2)]
        public string Description { get; set; }

    }

    [Message(7), ProtoContract]
    public class HelloRequest {

        [ProtoMember(1)]
        public string Name { get; set; }
    }

    [Message(8), ProtoContract]
    public class HelloResponse {

        [ProtoMember(1)]
        public string MessageText { get; set; }

    }

    [Message(9), ProtoContract]
    public class SubscribeRequest {

        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public TimeSpan Period { get; set; }

    }

    [Message(10), ProtoContract]
    public class TimeNotification {

        [ProtoMember(1)]
        public DateTime Time { get; set; }

    }
    [Message(11, IsSigned = true), ProtoContract]
    public class AuthenticatedRequest {

        [ProtoMember(1)]
        public string Question { get; set; }

    }

    [Message(12), ProtoContract]
    public class AuthenticatedResponse {

        [ProtoMember(1)]
        public string Answer { get; set; }

    }

    [Message(13), ProtoContract]
    public class EmptyRequest { }

    [Message(14), ProtoContract]
    public class EmptyResponse { }

}