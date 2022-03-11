using System.Diagnostics;
using System.Reflection;
using H.Pipes.AccessControl.Utilities;

namespace H.Pipes.AccessControl;

/// <summary>
/// This class will save only one running application and passing arguments if it is already running.
/// </summary>
public sealed class PipeApplication : IAsyncDisposable
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

#pragma warning disable CA2000 // Dispose objects before losing scope
            var client = new PipeClient<string[]>(ApplicationName);
#pragma warning restore CA2000 // Dispose objects before losing scope
            await using (client.ConfigureAwait(false))
            {
                client.ExceptionOccurred += (_, args) =>
                {
                    OnExceptionOccurred(args.Exception);
                };

                await client.WriteAsync(arguments, source.Token).ConfigureAwait(false);
            }

            return true;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            OnExceptionOccurred(exception);
        }

        try
        {
            var processes = Process.GetProcessesByName(ApplicationName);
            using var currentProcess = Process.GetCurrentProcess();
            foreach (var process in processes.Where(i => i.Id != currentProcess.Id))
            {
                process.Kill();
            }

            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
        catch (Exception)
        {
            // ignored.
        }

        return false;
    }

    /// <summary>
    /// Create new task with PipeServer.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
#if NET5_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#elif NETSTANDARD2_0_OR_GREATER || NET461_OR_GREATER
#else
#error Target Framework is not supported
#endif
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            PipeServer = new PipeServer<string[]>(ApplicationName);
            PipeServer.ExceptionOccurred += (sender, args) =>
            {
                OnExceptionOccurred(args.Exception);
            };
            PipeServer.AllowUsersReadWrite();

            await PipeServer.StartAsync(cancellationToken).ConfigureAwait(false);

            PipeServer.MessageReceived += (_, args) =>
            {
                OnArgumentsReceived(args.Message ?? Array.Empty<string>());
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Disposes pipe server
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        if (PipeServer != null)
        {
            await PipeServer.DisposeAsync().ConfigureAwait(false);
        }
    }

    #endregion
}
