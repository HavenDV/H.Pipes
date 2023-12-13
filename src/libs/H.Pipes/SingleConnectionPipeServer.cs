using System.IO.Pipes;
using H.Formatters;
using H.Pipes.Args;
using H.Pipes.Factories;
using H.Pipes.Utilities;

namespace H.Pipes;

/// <summary>
/// Wraps a <see cref="NamedPipeServerStream"/> and optimized for one connection.
/// </summary>
/// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
public sealed class SingleConnectionPipeServer<T> : IPipeServer<T>
{
    #region Properties

    /// <inheritdoc/>
    public string PipeName { get; }

    /// <inheritdoc/>
    public Func<string, NamedPipeServerStream>? CreatePipeStreamFunc { get; set; }

    /// <inheritdoc/>
    public Action<NamedPipeServerStream>? PipeStreamInitializeAction { get; set; }

    /// <inheritdoc/>
    public IFormatter Formatter { get; set; }

    /// <summary>
    /// Indicates whether to wait for a name to be released when calling StartAsync()
    /// </summary>
    public bool WaitFreePipe { get; set; }

    /// <summary>
    /// Connection
    /// </summary>
    public PipeConnection<T>? Connection { get; private set; }

    /// <inheritdoc/>
    public bool IsStarted => ListenWorker != null && !ListenWorker.Task.IsCompleted && !ListenWorker.Task.IsCanceled && !ListenWorker.Task.IsFaulted;


    private TaskWorker? ListenWorker { get; set; }

    private volatile bool _isDisposed;

    #endregion

    #region Events

    /// <inheritdoc/>
    public event EventHandler<ConnectionEventArgs<T>>? ClientConnected;

    /// <inheritdoc/>
    public event EventHandler<ConnectionEventArgs<T>>? ClientDisconnected;

    /// <inheritdoc/>
    public event EventHandler<ConnectionMessageEventArgs<T?>>? MessageReceived;

    /// <inheritdoc/>
    public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

    private void OnClientConnected(ConnectionEventArgs<T> args)
    {
        ClientConnected?.Invoke(this, args);
    }

    private void OnClientDisconnected(ConnectionEventArgs<T> args)
    {
        ClientDisconnected?.Invoke(this, args);
    }

    private void OnMessageReceived(ConnectionMessageEventArgs<T?> args)
    {
        MessageReceived?.Invoke(this, args);
    }

    private void OnExceptionOccurred(Exception exception)
    {
        ExceptionOccurred?.Invoke(this, new ExceptionEventArgs(exception));
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Constructs a new <c>NamedPipeServer</c> object that listens for client connections on the given <paramref name="pipeName"/>.
    /// </summary>
    /// <param name="pipeName">Name of the pipe to listen on</param>
    /// <param name="formatter">Default formatter - <see cref="DefaultFormatter"/></param>
    public SingleConnectionPipeServer(string pipeName, IFormatter? formatter = default)
    {
        PipeName = pipeName;
        Formatter = formatter ?? new DefaultFormatter();
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsStarted)
        {
            throw new InvalidOperationException("Server already started");
        }

        await StopAsync(cancellationToken).ConfigureAwait(false);

        var source = new TaskCompletionSource<bool>();
        using var registration = cancellationToken.Register(() => source.TrySetCanceled(cancellationToken));

        ListenWorker = new TaskWorker(async token =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (Connection != null && Connection.IsConnected)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (Connection != null)
                    {
                        await Connection.StopAsync().ConfigureAwait(false);
                    }

                    // Wait for the client to connect to the data pipe
                    var connectionStream = CreatePipeStreamFunc?.Invoke(PipeName) ?? PipeServerFactory.Create(PipeName);

                    try
                    {
                        PipeStreamInitializeAction?.Invoke(connectionStream);

                        source.TrySetResult(true);

                        await connectionStream.WaitForConnectionAsync(token).ConfigureAwait(false);
                    }
                    catch
                    {
#if NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER
                        await connectionStream.DisposeAsync().ConfigureAwait(false);
#elif NET461_OR_GREATER || NETSTANDARD2_0
                        connectionStream.Dispose();
#else
#error Target Framework is not supported
#endif

                        throw;
                    }

                    var connection = new PipeConnection<T>(connectionStream, PipeName, Formatter);
                    try
                    {
                        connection.MessageReceived += (_, args) => OnMessageReceived(args);
                        connection.Disconnected += (_, args) => OnClientDisconnected(args);
                        connection.ExceptionOccurred += (_, args) => OnExceptionOccurred(args.Exception);
                        connection.Start();
                    }
                    catch
                    {
                        await connection.StopAsync().ConfigureAwait(false);

                        throw;
                    }

                    Connection = connection;

                    OnClientConnected(new ConnectionEventArgs<T>(connection));
                }
                catch (OperationCanceledException)
                {
                    if (Connection != null)
                    {
                        await Connection.StopAsync().ConfigureAwait(false);

                        Connection = null;
                    }
                    throw;
                }
                // Catch the IOException that is raised if the pipe is broken or disconnected.
                catch (IOException exception)
                {
                    if (!WaitFreePipe)
                    {
                        source.TrySetException(exception);
                        break;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(1), token).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    OnExceptionOccurred(exception);
                    break;
                }
            }
        }, OnExceptionOccurred);

        try
        {
            await source.Task.ConfigureAwait(false);
        }
        catch (Exception)
        {
            await StopAsync(cancellationToken).ConfigureAwait(false);

            throw;
        }
    }

    /// <summary>
    /// Sends a message to all connected clients asynchronously.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    public async Task WriteAsync(T value, CancellationToken cancellationToken = default)
    {
        if (Connection is not { IsConnected: true })
        {
            return;
        }

        await Connection.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Closes all open client connections and stops listening for new ones.
    /// </summary>
    public async Task StopAsync(CancellationToken _ = default)
    {
        if (ListenWorker != null)
        {
            await ListenWorker.StopAsync().ConfigureAwait(false);

            ListenWorker = null;
        }

        if (Connection != null)
        {
            await Connection.StopAsync().ConfigureAwait(false);

            Connection = null;
        }
    }

    #endregion

    #region IDisposable

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        await StopAsync().ConfigureAwait(false);
    }

    #endregion
}
