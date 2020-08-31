using System;
using System.Threading;
using System.Threading.Tasks;

namespace Woof.WebSocket {

    /// <summary>
    /// Asynchronous loop runner.
    /// </summary>
    public static class AsyncLoop {

        /// <summary>
        /// Creates a new task with an asynchronous loop using iteration asynchronous function.
        /// </summary>
        /// <param name="iteration">A function called on each loop iteration.</param>
        /// <param name="token">Cancellation token used to end the loop.</param>
        /// <param name="exceptionHandler">Exception handler for the iteration.</param>
        /// <param name="condition">Optional condition that must evaluate true for the loop to continue or start.</param>
        /// <param name="breakOnException">If set true, exceptions in iteration should break the loop.</param>
        /// <returns>The started <see cref="Task{TResult}"/>.</returns>
        public static async Task FromIterationAsync(
            Func<Task> iteration,
            CancellationToken token,
            Action<Exception> exceptionHandler = null,
            Func<bool> condition = null,
            bool breakOnException = false
        ) => await Task.Factory.StartNew(async () => {
            try {
                while (!token.IsCancellationRequested && (condition is null || condition())) {
                    try {
                        await iteration();
                    }
                    catch (Exception exception) {
                        exceptionHandler?.Invoke(exception);
                        if (breakOnException) break;
                    }
                }
            } catch (TaskCanceledException) { }
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    }

}