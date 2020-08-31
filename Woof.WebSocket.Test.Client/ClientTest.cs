using System;
using System.Threading;
using System.Threading.Tasks;

using Woof.WebSocket.Test.Api;

namespace Woof.WebSocket.Test.Client {

    /// <summary>
    /// Command line test for the client.
    /// </summary>
    class ClientTest {


        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Task completed when the program is completed.</returns>
        static async Task Main(string[] args) {
            using var client = new TestClient();
            client.StateChanged += Client_StateChanged;
            client.ReceiveException += Client_OnReceiveException;
            client.MessageReceived += Client_MessageReceived;
            var tests = new Tests(client);
            await tests.MatchTestsFromArguments(args);
            await WaitForCtrlCAsync();
            await client.StopAsync();
        }


        /// <summary>
        /// Handles MessageReceived event.
        /// </summary>
        /// <param name="sender">Client.</param>
        /// <param name="e">Message event data.</param>
        static void Client_MessageReceived(object sender, MessageReceivedEventArgs e) {
            switch (e.DecodeResult.Message) {
                case TimeNotification timeNotification: Console.WriteLine($"SERVER TIME: {timeNotification.Time}"); break;
            }
        }

        /// <summary>
        /// Handles client state changed events.
        /// </summary>
        /// <param name="sender">Client.</param>
        /// <param name="e">State event data.</param>
        static void Client_StateChanged(object sender, StateChangedEventArgs e) => Console.WriteLine($"CLIENT STATE CHANGED: {e.State}");

        /// <summary>
        /// Handles client receive exceptions.
        /// </summary>
        /// <param name="sender">Client.</param>
        /// <param name="e">Exception event data.</param>
        static void Client_OnReceiveException(object sender, ExceptionEventArgs e) => Console.WriteLine($"CLIENT EXCEPTION: {e.Exception.Message}.");

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

    }

}