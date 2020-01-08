using System;
using System.Threading;
using System.Threading.Tasks;

namespace H.Pipes.Utilities
{
    /// <summary>
    /// A class designed to run code using <see cref="Task"/> with <see cref="TaskCreationOptions.LongRunning"/> <br/>
    /// and supporting automatic cancellation after <see cref="DisposeAsync"/>
    /// <![CDATA[Version: 1.0.0.0]]>
    /// </summary>
    internal class TaskWorker : IAsyncDisposable
    {
        #region Properties

        public Task Task { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        private volatile bool _isDisposed;

        #endregion

        #region Constructors

        public TaskWorker(Func<CancellationToken, Task> action, Action<Exception>? exceptionAction = null)
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
            }, CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Cancel task(if it's not completed) and dispose internal resources
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
