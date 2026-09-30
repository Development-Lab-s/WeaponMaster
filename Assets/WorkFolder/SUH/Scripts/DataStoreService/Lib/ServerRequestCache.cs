using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WorkFolder.SUH.Scripts.DataStoreService.Lib
{
    // Unity main-thread callers share one pending request; no queued duplicate retries.
    public sealed class ServerRequestCache<T>
    {
        private readonly double successSeconds;
        private readonly Func<double> now;
        private Task<T> request;
        private double nextRequestAt;
        private int failures;

        public ServerRequestCache(double successSeconds, Func<double> clock = null)
        {
            this.successSeconds = successSeconds;
            now = clock ?? (() => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency);
        }

        public bool CanStart => request == null || (request.IsCompleted && now() >= nextRequestAt);

        public Task<T> GetOrRun(Func<Task<T>> action)
        {
            if (!CanStart) return request;
            var completion = new TaskCompletionSource<T>();
            request = completion.Task;
            _ = Complete(action, completion);
            return completion.Task;
        }

        private async Task Complete(Func<Task<T>> action, TaskCompletionSource<T> completion)
        {
            try
            {
                T result = await action();
                failures = 0;
                nextRequestAt = now() + successSeconds;
                completion.SetResult(result);
            }
            catch (Exception ex)
            {
                failures = Math.Min(failures + 1, 5);
                nextRequestAt = now() + Math.Min(60d, 5d * Math.Pow(2, failures - 1));
                completion.SetException(ex);
            }
        }
    }
}
