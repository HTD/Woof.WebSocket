﻿using System;

using ProtoBuf;

/// <summary>
/// Test API definition designed as a part of package documentation.
/// </summary>
namespace Woof.WebSocket.Test.Api {

    // MESSAGE DEFINITIONS:

    /// <summary>
    /// Special sign-in request, note that it's signed and inherits <see cref="ISignInRequest"/> interface.<br/>
    /// Every message capable of being transpored using <see cref="WoofCodec"/> must have <see cref="MessageAttribute"/> and <see cref="ProtoContractAttribute"/> set.
    /// </summary>
    [Message(1, IsSigned = true), ProtoContract]
    public class SignInRequest : ISignInRequest {

        /// <summary>
        /// Gets or sets the API key in binary form. Reqiured by <see cref="ISignInRequest"/> interface.<br/>
        /// Every property of the message transported using <see cref="WoofCodec"/> must have <see cref="ProtoMemberAttribute"/> set.
        /// </summary>
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
    public class PingRequest { }

    [Message(8), ProtoContract]
    public class PingResponse { }

    [Message(9), ProtoContract]
    public class DivideRequest { 
    
        /// <summary>
        /// Note that the decimal type is just to make it harder for the serializer.
        /// </summary>
        [ProtoMember(1)]
        public decimal X { get; set; }

        [ProtoMember(2)]
        public decimal Y { get; set; }

    }

    [Message(10), ProtoContract]
    public class DivideResponse { 

        [ProtoMember(1)]
        public decimal Result { get; set; }

    }

    /// <summary>
    /// Note that the message is required to be signed.<br/>
    /// Check <see cref="DecodeResult{TTypeIndex, TMessageId}.IsSignatureValid"/> in <see cref="MessageReceivedEventArgs"/>.
    /// </summary>
    [Message(11, IsSigned = true), ProtoContract]
    public class PrivateRequest { 

        [ProtoMember(1)]
        public DateTime ClientTime { get; set; }

    }

    [Message(12), ProtoContract]
    public class PrivateResponse { 
    
        [ProtoMember(1)]
        public string Secret { get; set; }
    
    }

    /// <summary>
    /// Note that this particular request doesn't have matching response.
    /// </summary>
    [Message(13), ProtoContract]
    public class TimeSubscribeRequest {

        [ProtoMember(1)]
        public TimeSpan Period { get; set; }

    }

    [Message(14), ProtoContract]
    public class TimeNotification {

        [ProtoMember(1)]
        public DateTime Time { get; set; }

    }

    [Message(15), ProtoContract]
    public class TestUnexpectedRequest {

        [ProtoMember(1)]
        public int TypeId { get; set; }

        [ProtoMember(2)]
        public byte[] Data { get; set; }

    }

    [Message(16), ProtoContract]
    public class TestUnexpectedResponse {

        [ProtoMember(1)]
        public int TypeId { get; set; }

        [ProtoMember(2)]
        public byte[] Data { get; set; }

    }

    [Message(17), ProtoContract]
    public class IgnoreMessagesRequest {

        [ProtoMember(1)]
        public int Number { get; set; }

    }

    [Message(18), ProtoContract]
    public class IntroduceLagRequest {

        [ProtoMember(1)]
        public TimeSpan Time { get; set; }

    }

    [Message(19), ProtoContract]
    public class RestartRequest { }


    [ProtoContract]
    public class SingleItem {

        [ProtoMember(1)]
        public Guid Id { get; set; }

        [ProtoMember(2)]
        public int Number { get; set; }

        [ProtoMember(3)]
        public string Text { get; set; }

        [ProtoMember(4)]
        public int[] Array { get; set; }

    }

    [Message(21), ProtoContract]
    public class ComplexArray {

        [ProtoMember(1)]
        public SingleItem[] Items { get; set; }

    }

    [Message(100), ProtoContract]
    public class GetUriRequest { }

    [Message(101), ProtoContract]
    public class GetUriResponse {

        [ProtoMember(1)]
        public string RequestUri { get; set; }

    }
    
}