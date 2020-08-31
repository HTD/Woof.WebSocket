using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Woof.WebSocket.Test.Api;

namespace Woof.WebSocket.Test.Client {

    class ClientTest {

        #region Test keys

        const string validApiKey = "x5AvVKfex7b+xOPTAsKGnPqmCNj3HCPiCBUGDyg4ZJn6DHeVn8eGzGBeqLAtxKwRugsa9UEp4IMfYbCNKRrzcA==";
        const string validApiSecret = "8fRjPaT1YsN6kwGMxeZ9SZW1Za8gcN5cQFgfG+Ooie8e3QUMpZlVrN5h/6QNvATykHaADA6gSQ5qLDDd33xAlw==";
        const string invalidApiKey = "y5AvVKfex7b+xOPTAsKGnPqmCNj3HCPiCBUGDyg4ZJn6DHeVn8eGzGBeqLAtxKwRugsa9UEp4IMfYbCNKRrzcA==";
        const string invalidApiSecret = "7fRjPaT1YsN6kwGMxeZ9SZW1Za8gcN5cQFgfG+Ooie8e3QUMpZlVrN5h/6QNvATykHaADA6gSQ5qLDDd33xAlw==";

        #endregion

        static async Task Main(string[] args) {
            Client.StateChanged += Client_StateChanged;
            Client.ReceiveException += Client_OnReceiveException;
            await Client.StartAsync();
            #region PING test
            Console.Write("Sending PING...");
            await Client.PingAsync();
            Console.WriteLine("Received PONG.");
            #endregion
            #region Hello world test
            if (args.Contains("hello")) {
                var responseText = await Client.HelloAsync("world");
                Console.WriteLine(responseText);
            }
            #endregion
            #region Sign in test
            if (args.Contains("sign-in")) {
                Console.Write("Trying unauthorized operation...");
                try {
                    await Client.AskServerAsync("Show user accounts");
                    Console.WriteLine("Granted. FAIL!");
                } catch (UnexpectedMessageException x) {
                    if (x.Message is AccessDeniedResponse) Console.WriteLine("Denied. OK.");
                }
                var invalidKeySignInResult = await Client.SingInAsync(invalidApiKey, validApiSecret);
                Console.WriteLine($"Invalid key sign in test: {(invalidKeySignInResult ? "FAIL" : "OK")}.");
                var invalidSecretSignInResult = await Client.SingInAsync(validApiKey, invalidApiSecret);
                Console.WriteLine($"Invalid secret sign in test: {(invalidSecretSignInResult ? "FAIL" : "OK")}.");
                var invalidBothSignInResult = await Client.SingInAsync(invalidApiKey, invalidApiSecret);
                Console.WriteLine($"Invalid both key and secret sign in test: {(invalidBothSignInResult ? "FAIL" : "OK")}.");
                var validSignInResult = await Client.SingInAsync(validApiKey, validApiSecret);
                Console.WriteLine($"Valid credentials sign in test: {(validSignInResult ? "OK" : "FAIL")}.");
                var signedAnswer1 = await Client.AskServerAsync("21 + 21?");
                Console.WriteLine($"SIGNED1: {signedAnswer1}");
                var signedAnswer2 = await Client.AskServerAsync("What is the meaning of life?");
                Console.WriteLine($"SIGNED2: {signedAnswer2}");
                await Client.SignOutAsync();
                Console.Write("Trying unauthorized operation...");
                try {
                    await Client.AskServerAsync("Show user accounts");
                    Console.WriteLine("Granted. FAIL!");
                }
                catch (UnexpectedMessageException x) {
                    if (x.Message is AccessDeniedResponse) Console.WriteLine("Denied. OK.");
                }
                invalidSecretSignInResult = await Client.SingInAsync(validApiKey, invalidApiSecret);
                Console.WriteLine($"Invalid secret sign in test: {(invalidSecretSignInResult ? "FAIL" : "OK")}.");
            }
            #endregion
            #region Subscription test
            var subscribeIndex = Array.IndexOf(args, "subscribe");
            if (subscribeIndex >= 0) {
                var name = args.Length >= subscribeIndex + 2 ? args[subscribeIndex + 1] : null;
                var periodString = args.Length >= subscribeIndex + 3 ? args[subscribeIndex + 2] : null;
                if (name != null) {
                    if (periodString is null) await Client.SubscribeAsync(name);
                    else {
                        if (double.TryParse(periodString, NumberStyles.Any, CultureInfo.InvariantCulture, out var period)) {
                            await Client.SubscribeAsync(name, TimeSpan.FromSeconds(period));
                        }
                    }
                }
            }
            #endregion
            await WaitForCtrlCAsync();
            await Client.StopAsync();
        }

        static void Client_StateChanged(object sender, StateChangedEventArgs e) => Console.WriteLine($"CLIENT STATE CHANGED: {e.State}");

        static void Client_OnReceiveException(object sender, ExceptionEventArgs e) => Console.WriteLine($"CLIENT EXCEPTION: {e.Exception.Message}.");


        public static async Task WaitForCtrlCAsync(string message = "Press Ctrl+C to exit.") {
            using var semaphore = new SemaphoreSlim(0, 1);
            void handler(object s, ConsoleCancelEventArgs e) { Console.CancelKeyPress -= handler; e.Cancel = true; semaphore.Release(); }
            Console.CancelKeyPress += handler;
            Console.WriteLine(message);
            await semaphore.WaitAsync();
        }

        static readonly TestClient Client = new TestClient();

    }

}