using System.Diagnostics;
using System.Reflection;
using EventGenerator;
using H.Pipes.AccessControl.Utilities;

namespace H.Pipes.AccessControl;

/// <summary>
/// This class will save only one running application and passing arguments if it is already running.
/// </summary>
[Event<Exception>("ExceptionOccurred", PropertyNames = new[] { "Exception" },
    Description = "Occurs when new exception.")]
[Event<IReadOnlyCollection<string>>("ArgumentsReceived", PropertyNames = new[] { "Arguments" },
    Description = "Occurs when new arguments received.")]
public sealed partial class PipeApplication : IAsyncDisposable
{
    #region Properties

    private string ApplicationName { get; }
    private PipeServer<string[]>? PipeServer { get; set; }

    /// <summary>
    /// Maximum timeout for sending data to the server
    /// </summary>
    public TimeSpan ClientTimeout { get; set; } = TimeSpan.FromSeconds(5);

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
                client.ExceptionOccurred += (sender, args) =>
                {
                    _ = OnExceptionOccurred(args.Exception);
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
            _ = OnExceptionOccurred(exception);
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
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            PipeServer = new PipeServer<string[]>(ApplicationName);
            PipeServer.ExceptionOccurred += (_, args) =>
            {
                OnExceptionOccurred(args.Exception);
            };
#if NET5_0_OR_GREATER
            if (OperatingSystem.IsWindows())
            {
                PipeServer.AllowUsersReadWrite();
            }
#elif NET461_OR_GREATER
            PipeServer.AllowUsersReadWrite();
#elif NETSTANDARD2_0_OR_GREATER
#else
#error Target Framework is not supported
#endif

            await PipeServer.StartAsync(cancellationToken).ConfigureAwait(false);

            PipeServer.MessageReceived += (_, args) =>
            {
                OnArgumentsReceived(args.Message ?? Array.Empty<string>());
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Create new task with PipeServer. Will raise ArgumentsReceived event before start.
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(IReadOnlyCollection<string> arguments, CancellationToken cancellationToken = default)
    {
        _ = OnArgumentsReceived(arguments);
        
        return StartAsync(cancellationToken);
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
