# Woof.WebSocket

Full, high-level WebSocket client and server.
Designed to make WebSocket APIs blazing fast.

## Priority list:
- ease of use,
- code speed,
- stability,
- extensibility
- completness.

## Q/A:

> Q: Dependencies?<br/>
> A: .NET Standard 2.0, protobuf-net.

> Q: Thread-safe?<br/>
> A: Yes. Tested.

> Q: Asynchronous?<br/>
> A: Task based. Fully asynchronous.

> Q: How call `SendMessageAsync()` and friends in an event handler?<br/>
> A: Make the event handler "async void" and ensure no exceptions are thrown there.

> Q: Will the server work with clients in other environments (non .NET)?<br/>
> A: Yes. WOOF codec uses compatible Google Protocol Buffer serializer available for many environments.

> Q: Where can I find an example of a client written in language x?<br/>
> A: Be the first to write it and don't forget to send a pull request.

> Q: Can custom subprotocols be used with WebSocket transport?<br/>
> A: Yes. Any subprotocol can be used, all it takes is to implement `SubProtocolCodec` abstract class.

> Q: So why use this `Client` and `Server` without a subprotocol?<br/>
> A: A great deal of the transport hand-shaking already done. Tidy structure to implement.

> Q: Can it be done simpler?<br/>
> A: No.

> Q: Is that all?<br/>
> A: No. There will be streaming support soon.

## Usage:

First time: Pull the Git repository. See the provided examples. They contain full documentation in XMLdoc.<br/>
Not first time: Just install Woof.WebSocket with NuGet.
