﻿using System;
using System.Runtime.Serialization;

namespace Woof.WebSocket {

    /// <summary>
    /// Provides session management for both client and server.
    /// </summary>
    public class SessionProvider {

        /// <summary>
        /// Gets the sessions collection for the server instance.
        /// </summary>
        public SessionCollection? Sessions { get; private set; }

        /// <summary>
        /// Initializes session identifier for the current client context.<br/>
        /// Switches <see cref="SessionProvider"/> to SERVER mode / multiple sessions.
        /// </summary>
        /// <param name="context">WebSocket from the connected client.</param>
        public void OpenSession(WebSocketContext context) {
            if (IdGenerator is null) IdGenerator = new ObjectIDGenerator();
            IdGenerator.GetId(context, out var _);
        }

        /// <summary>
        /// Removes and disposes session opened with <see cref="OpenSession(WebSocketContext)"/>.
        /// </summary>
        /// <param name="context">WebSocket from the connected client.</param>
        public void CloseSession(WebSocketContext context) {
            if (IdGenerator is null || Sessions is null) return;
            var sessionId = IdGenerator.GetId(context, out _);
            Sessions.Remove(sessionId);
        }

        /// <summary>
        /// Removes and disposes all sessions opened with <see cref="OpenSession(WebSocketContext)"/>.
        /// </summary>
        public void CloseAllSessions() => Sessions?.Dispose();

        /// <summary>
        /// Gets a session for the current client connection.<br/>
        /// If the session doesn't exist it's created with an empty constructor.
        /// </summary>
        /// <typeparam name="TSession">Session type.</typeparam>
        /// <param name="context">WebSocket from the connected client. Or null for the client single session.</param>
        /// <returns>Session object.</returns>
        /// <exception cref="NullReferenceException">Thrown when no context is provided for server.</exception>
        public TSession GetSession<TSession>(WebSocketContext? context = null) where TSession : ISession, new() {
            if (IdGenerator is null) return (TSession)(Session = new TSession()); // single session, started from client scenario.
            if (context is null) throw new NullReferenceException("Context is required for server use");
            var sessionId = IdGenerator.GetId(context, out _);
            if (Sessions is null) Sessions = new SessionCollection();
            if (!Sessions.ContainsKey(sessionId)) {
                var newSession = new TSession() { Context = context };
                Sessions.Add(sessionId, newSession);
                return newSession;
            }
            return (TSession)Sessions[sessionId];
        }

        /// <summary>
        /// Gets the message signing key associated with the current session.
        /// </summary>
        /// <param name="context">WebSocket from the connected client.</param>
        /// <returns>Message signing key.</returns>
        public byte[]? GetKey(WebSocketContext context) {
            if (IdGenerator is null && Session is null) return null;
            if (IdGenerator is null) return Session?.Key;
            var sessionId = IdGenerator.GetId(context, out _);
            if (Sessions != null && Sessions.ContainsKey(sessionId)) return Sessions[sessionId].Key;
            return null;
        }

        private ISession? Session;
        

        private ObjectIDGenerator? IdGenerator;

    }

}
