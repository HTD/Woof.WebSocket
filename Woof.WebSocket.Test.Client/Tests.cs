using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Woof.WebSocket.Test.Api;

namespace Woof.WebSocket.Test.Client {

    /// <summary>
    /// Client-server communication tests and documentation in one.
    /// </summary>
    class Tests {

        #region Test keys

        const string validApiKey = "x5AvVKfex7b+xOPTAsKGnPqmCNj3HCPiCBUGDyg4ZJn6DHeVn8eGzGBeqLAtxKwRugsa9UEp4IMfYbCNKRrzcA==";
        const string validApiSecret = "8fRjPaT1YsN6kwGMxeZ9SZW1Za8gcN5cQFgfG+Ooie8e3QUMpZlVrN5h/6QNvATykHaADA6gSQ5qLDDd33xAlw==";
        const string invalidApiKey = "y5AvVKfex7b+xOPTAsKGnPqmCNj3HCPiCBUGDyg4ZJn6DHeVn8eGzGBeqLAtxKwRugsa9UEp4IMfYbCNKRrzcA==";
        const string invalidApiSecret = "7fRjPaT1YsN6kwGMxeZ9SZW1Za8gcN5cQFgfG+Ooie8e3QUMpZlVrN5h/6QNvATykHaADA6gSQ5qLDDd33xAlw==";

        #endregion

        #region Public API

        /// <summary>
        /// Creates tests for an initialized client.
        /// </summary>
        /// <param name="client">WS API client.</param>
        public Tests(TestClient client) {
            Client = client;
            try {
                if (client.State != ServiceState.Started) Client.StartAsync().Wait();
            }
            catch (Exception exception) {
                while (exception.InnerException != null) exception = exception.InnerException;
                Console.WriteLine(exception.Message);
            }
        }

        /// <summary>
        /// Matches command line arguments and starts appropriate tests.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Task completed when matched tests completed.</returns>
        public async Task MatchTestsFromArguments(string[] args) {
            if (Client.State != ServiceState.Started) return;
            if (IsArgMatched(args, "auth")) await AuthorizationTestAsync();
            if (IsArgMatched(args, "ping")) await PingTestAsync();
            if (IsArgMatched(args, "ping-pong", out int iterations, 128)) await PingPongTestAsync(iterations);
            if (IsArgMatched(args, "division")) await DivisionTestAsync();
            if (IsArgMatched(args, "unexpected")) await UnexpectedMessageIdTestAsync();
            if (IsArgMatched(args, "ignoring")) await MessageIgnoringTestAsync();
            if (IsArgMatched(args, "timeout")) await MessageTimeoutTestAsync();
            if (IsArgMatched(args, "time", out double seconds, 1)) await TimeNotificationTestAsync(TimeSpan.FromSeconds(seconds));
        }

        #endregion

        #region Command line arguments matching

        /// <summary>
        /// Tests if an argument is matched in command line arguments (case insensitive).
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <param name="item">Item to match.</param>
        /// <returns>True if matched.</returns>
        bool IsArgMatched(string[] args, string item)
            => args.Contains("all", StringComparer.OrdinalIgnoreCase) || args.Contains(item, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Tests if an argument is matched in command line arguments (case insensitive).
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <param name="item">Item to match.</param>
        /// <param name="parameter">Parameter string.</param>
        /// <returns>True if matched.</returns>
        bool IsArgMatched(string[] args, string item, out string parameter) {
            for (int i = 0, n = args.Length; i < n; i++)
                if (args[i].Equals(item, StringComparison.OrdinalIgnoreCase)) {
                    parameter = i <= n - 2 ? args[i + 1] : null; return true;
                }
            parameter = null;
            if (args.Contains("all", StringComparer.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// Tests if an argument is matched in command line arguments (case insensitive).
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <param name="item">Item to match.</param>
        /// <param name="parameter">Parameter number.</param>
        /// <param name="fallback">Fallback value if different from default for the type.</param>
        /// <returns>True if matched.</returns>
        bool IsArgMatched(string[] args, string item, out double parameter, double fallback = default) {
            parameter = fallback;
            if (!IsArgMatched(args, item, out string candidate)) return false;
            if (candidate is null || !Regex.IsMatch(candidate, @"^\d")) return true;
            return double.TryParse(candidate, NumberStyles.Any, CultureInfo.InvariantCulture, out parameter);
        }

        /// <summary>
        /// Tests if an argument is matched in command line arguments (case insensitive).
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <param name="item">Item to match.</param>
        /// <param name="parameter">Parameter number.</param>
        /// <param name="fallback">Fallback value if different from default for the type.</param>
        /// <returns>True if matched.</returns>
        bool IsArgMatched(string[] args, string item, out int parameter, int fallback = default) {
            parameter = fallback;
            if (!IsArgMatched(args, item, out string candidate)) return false;
            if (candidate is null || !Regex.IsMatch(candidate, @"^\d")) return true;
            return int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out parameter);
        }

        #endregion

        #region Authorization tests

        /// <summary>
        /// Performs various authorization tests.
        /// </summary>
        /// <returns>Task completed when all tests completed.</returns>
        async Task AuthorizationTestAsync() {
            await UnauthorizedOperationTestAsync();
            bool bResult;
            DescribeTest("Invalid key sign in");
            bResult = await Client.SingInAsync(invalidApiKey, validApiSecret);
            DescribeResult(!bResult);
            DescribeTest("Invalid secret sign in");
            bResult = await Client.SingInAsync(validApiKey, invalidApiSecret);
            DescribeResult(!bResult);
            DescribeTest("Invalid both key and secret sign in");
            bResult = await Client.SingInAsync(invalidApiKey, invalidApiSecret);
            DescribeResult(!bResult);
            DescribeTest("Valid credentials sign in");
            bResult = await Client.SingInAsync(validApiKey, validApiSecret);
            DescribeResult(bResult);
            await AuthorizedOperationTestAsync();
            DescribeTest("Sign out");
            try {
                await Client.SignOutAsync();
                DescribeResult(true);
            }
            catch (Exception x) {
                DescribeResult(false, x.Message);
            }
            await UnauthorizedOperationTestAsync();
        }

        /// <summary>
        /// Performs authorized operation that should not fail.<br/>
        /// Call when authorized.
        /// </summary>
        /// <returns>Task completed when the test is completed.</returns>
        async Task AuthorizedOperationTestAsync() {
            DescribeTest("Authorized operation");
            try {
                var a1 = await Client.CheckAuthorizedAsync();
                var a2 = await Client.CheckAuthorizedAsync();
                DescribeResult(a1 == "AUTHORIZED" && a2 == "AUTHORIZED");
            }
            catch (Exception x) {
                DescribeResult(false, x.Message);
            }
        }

        /// <summary>
        /// Performs authorized operation that SHOULD fail.<br/>
        /// Call when unauthorized.
        /// </summary>
        /// <returns>Task completed when the test is completed.</returns>
        async Task UnauthorizedOperationTestAsync() {
            DescribeTest("Unauthorized operation");
            try {
                await Client.CheckAuthorizedAsync();
                DescribeResult(false, "GRANTED!");
            }
            catch (UnexpectedMessageException x) {
                if (x.Message is AccessDeniedResponse) DescribeResult(true, "Denied.");
                else DescribeResult(false, "Really unexpected response");
            }
        }

        #endregion

        #region Ping-pong tests

        /// <summary>
        /// Performs a ping test with a minimal request-response sequence.
        /// </summary>
        /// <returns>Task completed when the test is completed.</returns>
        async Task PingTestAsync() {
            DescribeTest("Testing ping");
            try {
                await Client.PingAsync();
                DescribeResult(true);
            }
            catch (Exception exception) {
                DescribeResult(false, exception.Message);
            }
        }

        /// <summary>
        /// Performs a ping-pong test with an iterated minimal request-response sequence.
        /// </summary>
        /// <param name="count">Number of iterations.</param>
        /// <returns>Task completed when all iteratios completed.</returns>
        public async Task PingPongTestAsync(int count) {
            DescribeTest($"Ping-pong test (n={count})");
            for (int i = 0; i < count; i++) await Client.PingAsync();
            DescribeResult(true);
        }

        #endregion

        #region Error handling tests

        /// <summary>
        /// Tests the API error handling capability with an asynchronous method expected to fail returning an <see cref="ErrorResponse"/> message.
        /// </summary>
        /// <returns>Task completed when the test is completed.</returns>
        public async Task DivisionTestAsync() {
            DescribeTest("Valid division");
            try {
                var result = await Client.DivideAsync(1m, 2m);
                DescribeResult(result == 0.5m);
            }
            catch (Exception x) {
                DescribeResult(false, x.Message);
            }
            DescribeTest("Invalid division");
            try {
                await Client.DivideAsync(1m, 0m);
                DescribeResult(false);
            }
            catch (UnexpectedMessageException x) {
                if (x.Message is ErrorResponse error) {
                    DescribeResult(true, error.Description);
                }
                else DescribeResult(false);
            }
        }

        /// <summary>
        /// Tests the unexpected message id behavior.
        /// </summary>
        /// <returns>Task completed when the test is completed.</returns>
        public async Task UnexpectedMessageIdTestAsync() {
            const int typeId = 666;
            byte[] data = Guid.NewGuid().ToByteArray();
            DescribeTest("Unexpected message id");
            try {
                var result = await Client.TestUnexpectedMessageTypeAsync(typeId, data);
                DescribeResult(result.TypeId == typeId && result.Data.SequenceEqual(data));
            }
            catch (Exception x) {
                DescribeResult(false, x.Message);
            }
        }

        public async Task MessageIgnoringTestAsync() {
            DescribeTest("Message ignoring");
            await Client.IgnoreRequestsAsync(2);
            var timeOutCount = 0;
            for (var i = 0; i < 3; i++) {
                try {
                    await Client.PingAsync();
                } catch (TimeoutException) {
                    timeOutCount++;
                }
            }
            DescribeResult(timeOutCount == 2);
        }

        public async Task MessageTimeoutTestAsync() {
            DescribeTest("Message timeout");
            var timeOutCount = 0;
            await Client.IntroduceLagAsync(TimeSpan.FromSeconds(2.5));
            await Task.Delay(TimeSpan.FromSeconds(5));
            try {
                await Client.PingAsync();
            }
            catch (TimeoutException) {
                timeOutCount++;
            }
            await Client.IntroduceLagAsync(TimeSpan.FromSeconds(0));
            DescribeResult(timeOutCount == 1);
        }

        #endregion

        #region Notifications tests

        /// <summary>
        /// Subscribes to server time notification.
        /// </summary>
        /// <param name="period">Delay period between notifications.</param>
        /// <returns>Task completed as soon as the subscription request is sent.</returns>
        public async Task TimeNotificationTestAsync(TimeSpan period) {
            DescribeTest($"Subscribe server time ({period:ss\\.fff}s)");
            await Client.TimeSubscribeAsync(period);
            DescribeResult(true);
        }

        #endregion

        #region Test descriptions

        /// <summary>
        /// Describes the test and leaves the carret in the same line.
        /// </summary>
        /// <param name="description">Test description.</param>
        private void DescribeTest(string description) => Console.Write($"{description}...");

        /// <summary>
        /// Describes the test result and ends the text line.
        /// </summary>
        /// <param name="result"><see cref="true"/>: success, <see cref="false"/>: fail.</param>
        /// <param name="remarks">Optional remarks.</param>
        private void DescribeResult(bool result, string remarks = null) {
            Console.Write(result ? "OK." : "FAIL!");
            if (remarks is null) Console.WriteLine();
            else Console.WriteLine($" ({remarks})");
        }

        #endregion

        #region Private data

        /// <summary>
        /// Tested client.
        /// </summary>
        private readonly TestClient Client;

        #endregion

    }

}