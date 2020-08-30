using System;
using System.Threading;
using System.Threading.Tasks;

namespace Woof.WebSocket.Test.Server {
    
    class ServerTest {
        
        static async Task Main() {
            await Server.StartAsync();
            await WaitForCtrlCAsync();
            await Server.StopAsync();
        }

        public static async Task WaitForCtrlCAsync(string message = "Press Ctrl+C to exit.") {
            using var semaphore = new SemaphoreSlim(0, 1);
            void handler(object s, ConsoleCancelEventArgs e) { Console.CancelKeyPress -= handler; e.Cancel = true; semaphore.Release(); }
            Console.CancelKeyPress += handler;
            Console.WriteLine(message);
            await semaphore.WaitAsync();
        }

        static readonly TestServer Server = new TestServer();

    }

}