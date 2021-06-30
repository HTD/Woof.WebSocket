using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Woof.WebSocket.Test.DoubleServer {
    
    class Program {
        
        static async Task Main(string[] args) {
            var servers = new List<TestServer>();
            foreach (var x in Config.Data.GetSection("Endpoints").GetChildren()) {
                servers.Add(new TestServer(new Uri(x.Value)));
            }
            foreach (var server in servers) {
                await server.StartAsync();
            }
            await WaitForCtrlCAsync();
            foreach (var server in servers) {
                await server.StopAsync();
            }
        }

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
