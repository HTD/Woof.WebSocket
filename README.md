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
> A: .NET 5.0, protobuf-net. Can be made to work with older frameworks, in order to do so use the source code, change the target framework, implement necessary changes.

> Q: Thread-safe?<br/>
> A: Yes. Tested. Tested. Then tested some more.

> Q: Asynchronous?<br/>
> A: Task based. Fully asynchronous.

> Q: How call `SendMessageAsync()` and friends in an event handler?<br/>
> A: Make the event handler "async void" and ensure no exceptions are thrown there. Pass exceptions as data when necessary.

> Q: Will the server work with clients in other environments (non .NET)?<br/>
> A: Yes. WOOF codec uses compatible Google Protocol Buffer serializer available for many environments. It's relatively easy to make your own codec using JSON or any other object serializer.<br/>

> Q: Why Protocol Buffers?<br/>
> A: Because it efficient (small serialized size), fast (simple algorithm, fixed size reads), well established (invented by Goolge, developed by active community).

> Q: Where can I find an example of a client written in language x?<br/>
> A: Be the first to write it and don't forget to send a pull request.

> Q: Can custom subprotocols be used with WebSocket transport?<br/>
> A: Yes. Any subprotocol can be used, all it takes is to implement `SubProtocolCodec` abstract class. Do JSON as an excercise ;)

> Q: So why use this `Client` and `Server` without a subprotocol?<br/>
> A: A great deal of the transport hand-shaking already done. Tidy structure to implement. And you DO have a good WOOF subprotocol based on Protocol Buffers.

> Q: Can it be done simpler?<br/>
> A: No. I really tried. This is as simple as it gets.

> Q: Is that all?<br/>
> A: No. There will be streaming support soon. But don't hold your breath. Or just add it and send a pull request. Look at Skype: it uses another port (you can use just another endpoint, like a different path) to use separate server just for streaming data.

> Q: Wow, neat DI there ;) But what if my `IAuthenticationProvider` implementation throws an exception? I don't see it anywhere, is it a bug?<br/>
> A: Nope. It's a feature. Check the `Exception` property of the `DecodeResult` in your `OnMessageReceived` handler. It's there to be handled in your code.

> Q: About the last one, isn't that dangerous? I can have non-breaking (silent) exception in authentication code.<br/>
> A: You still have `IsSignatureValid` `false` and `IsUnauthorized` `true` in `DecodeResult`. Do use them.

## Usage:

First time: Pull the Git repository. See the provided examples. They contain full documentation in XMLdoc.<br/>
Not first time: Just install Woof.WebSocket with NuGet.
