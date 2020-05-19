using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes.AccessControl.Utilities;

namespace H.Pipes.AccessControl
{
    /// <summary>
    /// This class will save only one running application and passing arguments if it is already running.
    /// </summary>
    public sealed class PipeApplication : IDisposable, IAsyncDisposable
    {
        #region Properties

        private string ApplicationName { get; }
        private PipeServer<string[]>? PipeServer { get; set; }

        /// <summary>
        /// Maximum timeout for sending data to the server
        /// </summary>
        public TimeSpan ClientTimeout { get; set; } = TimeSpan.FromSeconds(5);

        #endregion

        #region Events

        /// <summary>
        /// Occurs when new exception
        /// </summary>
        public event EventHandler<Exception>? ExceptionOccurred;

        /// <summary>
        /// Occurs when new arguments received
        /// </summary>
        public event EventHandler<IList<string>>? ArgumentsReceived;

        private void OnExceptionOccurred(Exception value)
        {
            ExceptionOccurred?.Invoke(this, value);
        }

        private void OnArgumentsReceived(IList<string> value)
        {
            ArgumentsReceived?.Invoke(this, value);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Create pipe application
        /// </summary>
        /// <param name="applicationName"></param>
        public PipeApplication(string? applicationName = null)
        {
            ApplicationName = applicationName ?? Assembly.GetEntryAssembly()?.GetName().Name
                ?? throw new ArgumentException("Application name is null and is not found in entry assembly");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Try check and send to other process. <br/>
        /// Return <see langword="true"/> if other process is exists. <br/>
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public async Task<bool> TrySendAsync(string[] arguments)
        {
            if (ProcessUtilities.IsFirstProcess(ApplicationName))
            {
                return false;
            }

            try
            {
                if (!arguments.Any())
                {
                    return true;
                }

                using var source = new CancellationTokenSource(ClientTimeout);
                await using var client = new PipeClient<string[]>(ApplicationName);
                client.ExceptionOccurred += (_, args) =>
                {
                    OnExceptionOccurred(args.Exception);
                };

                await client.WriteAsync(arguments, source.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                OnExceptionOccurred(exception);
            }

            return true;
        }

        /// <summary>
        /// Create new thread with PipeServer.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
            {
                PipeServer = new PipeServer<string[]>(ApplicationName);
                PipeServer.AddAccessRules(
                    new PipeAccessRule(
                        new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
                        PipeAccessRights.ReadWrite,
                        AccessControlType.Allow));

                await PipeServer.StartAsync(cancellationToken).ConfigureAwait(false);

                PipeServer.MessageReceived += (sender, args) =>
                {
                    OnArgumentsReceived(args.Message);
                };
                PipeServer.ExceptionOccurred += (sender, args) =>
                {
                    OnExceptionOccurred(args.Exception);
                };
            }, cancellationToken);
        }

        /// <summary>
        /// Disposes pipe server
        /// </summary>
        /// <returns></returns>
        public void Dispose()
        {
            PipeServer?.Dispose();
        }

        /// <summary>
        /// Disposes pipe server
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            if (PipeServer != null)
            {
                await PipeServer.DisposeAsync();
            }
        }

        #endregion
    }
}
