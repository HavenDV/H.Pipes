using System;
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
    public sealed class PipeApplication
    {
        #region Properties

        private string ApplicationName { get; }

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

        private void OnExceptionOccurred(Exception value)
        {
            ExceptionOccurred?.Invoke(this, value);
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
        /// <param name="func"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(Func<string[], CancellationToken, Task> func, CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
            {
                await using var server = new PipeServer<string[]>(ApplicationName);
                server.AddAccessRules(
                    new PipeAccessRule(
                        new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
                        PipeAccessRights.ReadWrite,
                        AccessControlType.Allow));

                await server.StartAsync(cancellationToken).ConfigureAwait(false);

                server.MessageReceived += async (sender, args) =>
                {
                    try
                    {
                        await func(args.Message, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception exception)
                    {
                        OnExceptionOccurred(exception);
                    }
                };
                server.ExceptionOccurred += (sender, args) =>
                {
                    OnExceptionOccurred(args.Exception);
                };

                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        #endregion
    }
}
