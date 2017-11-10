namespace ServiceControl.Plugin.Nsb6.Heartbeat
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    class AsyncTimer
    {
        public void Start(Func<Task> callback, TimeSpan interval, Action<Exception> errorCallback)
        {
            tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            task = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(interval, token).ConfigureAwait(false);
                        await callback().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // nop	 
                    }
                    catch (Exception ex)
                    {
                        errorCallback(ex);
                    }
                }
            }, CancellationToken.None);
        }

        public Task Stop()
        {
            tokenSource.Cancel();
            return task;
        }

        Task task;
        CancellationTokenSource tokenSource;
    }
}