using System;
using System.Threading;
using System.Threading.Tasks;

namespace NamedPipeWrapper.Threading
{
    internal class Worker : IDisposable
    {
        #region Properties

        public Task Task { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        #endregion

        #region Constructors

        public Worker(Action action, Action<Exception>? exceptionAction = null)
        {
            Task = Task.Factory.StartNew(() =>
            {
                try
                {
                    action?.Invoke();
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
        public void Dispose()
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();

            Task.Dispose();
        }

        #endregion
    }
}
