using System;
using System.Threading;
using System.Threading.Tasks;

namespace H.Pipes.Utilities
{
    internal class Worker : IAsyncDisposable
    {
        #region Properties

        public Task Task { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        private volatile bool _isDisposed;

        #endregion

        #region Constructors

        public Worker(Func<CancellationToken, Task> action, Action<Exception>? exceptionAction = null)
        {
            Task = Task.Factory.StartNew(async () =>
            {
                try
                {
                    await action(CancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    exceptionAction?.Invoke(exception);
                }
            }, CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose internal resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            CancellationTokenSource.Cancel();
            
            await Task.ConfigureAwait(false);

            CancellationTokenSource.Dispose();
            Task.Dispose();
        }

        #endregion
    }
}
