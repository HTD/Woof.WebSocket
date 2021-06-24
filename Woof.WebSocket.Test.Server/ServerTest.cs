using System;
using System.Threading;
using System.Threading.Tasks;

namespace Woof.WebSocket.Test.Server {

    /// <summary>
    /// Command line test for the server.
    /// </summary>
    class ServerTest {

        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <returns>Task completed when the program is completed.</returns>
        static async Task Main() {
            Server.StateChanged += Server_StateChanged;
            Server.ClientConnected += Server_ClientConnected;
            Server.ClientDisconnecting += Server_ClientDisconnecting;
            Server.ConnectException += Server_ConnectException;
            Server.ReceiveException += Server_ReceiveException;
            await Server.StartAsync();
            await WaitForCtrlCAsync();
            await Server.StopAsync();
        }

        /// <summary>
        /// Handles client connected event.
        /// </summary>
        /// <param name="sender">Server.</param>
        /// <param name="e">WebSocket context arguments.</param>
        private static void Server_ClientConnected(object sender, WebSocketEventArgs e)
            => Console.WriteLine("CLIENT CONNECTED.");

        /// <summary>
        /// Handles client disconnecting event.
        /// </summary>
        /// <param name="sender">Server.</param>
        /// <param name="e">WebSocket context arguments.</param>
        private static void Server_ClientDisconnecting(object sender, WebSocketEventArgs e)
            => Console.WriteLine("CLIENT DISCONNECTED.");

        /// <summary>
        /// Handles client state changed events.
        /// </summary>
        /// <param name="sender">Server.</param>
        /// <param name="e">State event data.</param>
        static void Server_StateChanged(object sender, StateChangedEventArgs e)
            => Console.WriteLine($"SERVER STATE CHANGED: {e.State}");

        /// <summary>
        /// Handles server connect exceptions.
        /// </summary>
        /// <param name="sender">Server.</param>
        /// <param name="e">Exception data.</param>
        static void Server_ConnectException(object sender, ExceptionEventArgs e)
            => Console.WriteLine($"SERVER RECEIVE EXCEPTION: {e.Exception.Message}");

        /// <summary>
        /// Handles server receive exceptions.
        /// </summary>
        /// <param name="sender">Server.</param>
        /// <param name="e">Exception data.</param>
        static void Server_ReceiveException(object sender, ExceptionEventArgs e)
            => Console.WriteLine($"SERVER RECEIVE EXCEPTION: {e.Exception.Message}");

        /// <summary>
        /// Waits for Ctrl+C.
        /// </summary>
        /// <param name="message">Message to display.</param>
        /// <returns>Task completed when the Ctrl+C is pressed.</returns>
        public static async Task WaitForCtrlCAsync(string message = "Press Ctrl+C to exit.") {
            using var semaphore = new SemaphoreSlim(0, 1);
            void handler(object s, ConsoleCancelEventArgs e) { Console.CancelKeyPress -= handler; e.Cancel = true; semaphore.Release(); }
            Console.CancelKeyPress += handler;
            Console.WriteLine(message);
            await semaphore.WaitAsync();
        }

        /// <summary>
        /// <see cref="TestServer"/> instance.
        /// </summary>
        static readonly TestServer Server = new();

    }

}