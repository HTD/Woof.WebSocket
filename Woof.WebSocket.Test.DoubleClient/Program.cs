using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Woof.WebSocket.Test.DoubleClient {
    class Program {
        
        static async Task Main(string[] args) {
            var clients = new List<TestClient>();
            foreach (var x in Config.Data.GetSection("Endpoints").GetChildren()) {
                clients.Add(new TestClient(new Uri(x.Value)));
            }
            foreach (var client in clients) {
                await client.StartAsync();
                var uri = await client.GetUriAsync();
                Console.WriteLine($"RequestUri: {uri}");
            }
            await WaitForCtrlCAsync();
            foreach (var client in clients) {
                await client.StopAsync();
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
