using System;
using System.Threading;
using System.Threading.Tasks;
using NamedPipeWrapper.Args;

namespace NamedPipeWrapper.Threading
{
    internal class Worker
    {
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
                OnSucceeded();
            }
            catch (Exception exception)
            {
                OnError(exception);
            }
        }

        #endregion
    }
}
