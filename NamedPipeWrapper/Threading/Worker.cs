using System;
using System.Threading;
using System.Threading.Tasks;

namespace NamedPipeWrapper.Threading
{
    internal class Worker
    {
        #region Properties

        private TaskScheduler TaskScheduler { get; }

        #endregion

        #region Events

        public event EventHandler? Succeeded;
        public event EventHandler<ExceptionEventArgs>? Error;

        private void OnSucceeded()
        {
            Succeeded?.Invoke(this, EventArgs.Empty);
        }

        private void OnError(Exception exception)
        {
            Error?.Invoke(this, new ExceptionEventArgs(exception));
        }

        #endregion

        #region Constructors

        public Worker(TaskScheduler taskScheduler)
        {
            TaskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
        }

        /// <summary>
        /// Create worker with current task scheduler
        /// </summary>
        public Worker() : this(SynchronizationContext.Current != null
            ? TaskScheduler.FromCurrentSynchronizationContext()
            : TaskScheduler.Default)
        {
        }

        #endregion

        #region Public methods

        public void DoWork(Action action)
        {
            new Task(DoWorkImpl, action, CancellationToken.None, TaskCreationOptions.LongRunning).Start();
        }

        #endregion

        #region Private methods

        private void DoWorkImpl(object oAction)
        {
            var action = (Action) oAction;
            try
            {
                action();
                Callback(OnSucceeded);
            }
            catch (Exception exception)
            {
                Callback(() => OnError(exception));
            }
        }

        private void Callback(Action action)
        {
            Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, TaskScheduler);
        }

        #endregion
    }
}
