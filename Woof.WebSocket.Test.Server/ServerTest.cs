using System;
using System.Threading;
using System.Threading.Tasks;

namespace Woof.WebSocket.Test.Server {
    
    class ServerTest {
        
        static async Task Main() {
            Server.StateChanged += Server_StateChanged;
            Server.ClientConnected += Server_ClientConnected;
            Server.ClientDisconnecting += Server_ClientDisconnecting;
            Server.ReceiveException += Server_ReceiveException;
            await Server.StartAsync();
            await WaitForCtrlCAsync();
            await Server.StopAsync();
        }

        private static void Server_ClientConnected(object sender, WebSocketEventArgs e)
            => Console.WriteLine("CLIENT CONNECTED.");

        private static void Server_ClientDisconnecting(object sender, WebSocketEventArgs e)
            => Console.WriteLine("CLIENT DISCONNECTED.");

        static void Server_StateChanged(object sender, StateChangedEventArgs e)
            => Console.WriteLine($"SERVER STATE CHANGED: {e.State}");

        static void Server_ReceiveException(object sender, ExceptionEventArgs e)
            => Console.WriteLine($"SERVER EXCEPTION: {e.Exception.Message}");

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