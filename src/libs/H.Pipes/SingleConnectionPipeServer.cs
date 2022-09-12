using System.IO.Pipes;
using H.Formatters;
using H.Pipes.Args;
using H.Pipes.Factories;
using H.Pipes.Utilities;

namespace H.Pipes;


/// <summary>
/// Wraps a <see cref="NamedPipeServerStream"/> and optimized for one connection.
/// Specialized version of <see cref="SingleConnectionPipeServer"/> for communications based on a single type.
/// </summary>
/// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
/// <seealso cref="H.Pipes.SingleConnectionPipeServer" />
/// <seealso cref="H.Pipes.IPipeServer{T}" />
public class SingleConnectionPipeServer<T> : SingleConnectionPipeServer, IPipeServer<T>
{
    #region Constructors

    /// <inheritdoc />
    public SingleConnectionPipeServer(string pipeName, IFormatter? formatter = default)
        : base(pipeName, formatter) { }

    #endregion

    #region Events

    /// <inheritdoc />
    public new event EventHandler<ConnectionMessageEventArgs<T?>>? MessageReceived;

    /// <summary>
    /// Calls the <see cref="MessageReceived"/> event.
    /// </summary>
    /// <param name="args">The arguments.</param>
    protected void OnMessageReceived(ConnectionMessageEventArgs<T?> args)
    {
        MessageReceived?.Invoke(this, args);
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public Task WriteAsync(T value, CancellationToken cancellationToken = default)
    {
        return base.WriteAsync(value, cancellationToken);
    }

    /// <inheritdoc />
    protected override PipeConnection SetupPipeConnection(NamedPipeServerStream connectionStream, string connectionPipeName, IFormatter formatter)
    {
        var connection = new PipeConnection<T>(connectionStream, connectionPipeName, Formatter);

        connection.MessageReceived   += (_, args) => OnMessageReceived(args);
        connection.Disconnected      += (_, args) => OnClientDisconnected(args);
        connection.ExceptionOccurred += (_, args) => OnExceptionOccurred(args.Exception);

        return connection;
    }

    #endregion
}


/// <summary>
/// Wraps a <see cref="NamedPipeServerStream"/> and optimized for one connection.
/// </summary>
public class SingleConnectionPipeServer : IPipeServer
{
    #region Properties

    /// <summary>
    /// Name of pipe
    /// </summary>
    public string PipeName { get; }

    /// <summary>
    /// CreatePipeStreamFunc
    /// </summary>
    public Func<string, NamedPipeServerStream>? CreatePipeStreamFunc { get; set; }

    /// <summary>
    /// PipeStreamInitializeAction
    /// </summary>
    public Action<NamedPipeServerStream>? PipeStreamInitializeAction { get; set; }

    /// <summary>
    /// Used formatter
    /// </summary>
    public IFormatter Formatter { get; set; }

    /// <summary>
    /// Indicates whether to wait for a name to be released when calling StartAsync()
    /// </summary>
    public bool WaitFreePipe { get; set; }

    /// <summary>
    /// Connection
    /// </summary>
    public PipeConnection? Connection { get; private set; }

    /// <summary>
    /// IsStarted
    /// </summary>
    public bool IsStarted => ListenWorker != null && !ListenWorker.Task.IsCompleted && !ListenWorker.Task.IsCanceled && !ListenWorker.Task.IsFaulted;


    private TaskWorker? ListenWorker { get; set; }

    private volatile bool _isDisposed;

    #endregion

    #region Events

    /// <summary>
    /// Invoked whenever a client connects to the server.
    /// </summary>
    public event EventHandler<ConnectionEventArgs>? ClientConnected;

    /// <summary>
    /// Invoked whenever a client disconnects from the server.
    /// </summary>
    public event EventHandler<ConnectionEventArgs>? ClientDisconnected;

    /// <summary>
    /// Invoked whenever a client sends a message to the server.
    /// </summary>
    public event EventHandler<ConnectionMessageEventArgs<byte[]?>>? MessageReceived;

    /// <summary>
    /// Invoked whenever an exception is thrown during a read or write operation.
    /// </summary>
    public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

    /// <summary>
    /// Calls the <see cref="ClientConnected"/> event.
    /// </summary>
    /// <param name="args">The <see cref="ConnectionEventArgs"/> instance containing the event data.</param>
    protected virtual void OnClientConnected(ConnectionEventArgs args)
    {
        ClientConnected?.Invoke(this, args);
    }

    /// <summary>
    /// Calls the <see cref="ClientDisconnected"/> event.
    /// </summary>
    /// <param name="args">The <see cref="ConnectionEventArgs"/> instance containing the event data.</param>
    protected virtual void OnClientDisconnected(ConnectionEventArgs args)
    {
        ClientDisconnected?.Invoke(this, args);
    }

    /// <summary>
    /// Calls the <see cref="MessageReceived"/> event.
    /// </summary>
    /// <param name="args">The instance containing the event data.</param>
    protected virtual void OnMessageReceived(ConnectionMessageEventArgs<byte[]?> args)
    {
        MessageReceived?.Invoke(this, args);
    }

    /// <summary>
    /// Calls the <see cref="ExceptionOccurred"/> event.
    /// </summary>
    /// <param name="exception">The exception.</param>
    protected virtual void OnExceptionOccurred(Exception exception)
    {
        ExceptionOccurred?.Invoke(this, new ExceptionEventArgs(exception));
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Constructs a new <c>NamedPipeServer</c> object that listens for client connections on the given <paramref name="pipeName"/>.
    /// </summary>
    /// <param name="pipeName">Name of the pipe to listen on</param>
    /// <param name="formatter">Default formatter - <see cref="BinaryFormatter"/></param>
    public SingleConnectionPipeServer(string pipeName, IFormatter? formatter = default)
    {
        PipeName = pipeName;
        Formatter = formatter ?? new BinaryFormatter();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Begins listening for client connections in a separate background thread.
    /// This method waits when pipe will be created(or throws exception).
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="IOException"></exception>
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
                    if (Connection is { IsConnected: true })
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

                    var connection = SetupPipeConnection(connectionStream, PipeName, Formatter);

                    try
                    {
                        connection.Start();
                    }
                    catch
                    {
                        await connection.StopAsync().ConfigureAwait(false);

                        throw;
                    }

                    Connection = connection;

                    OnClientConnected(new ConnectionEventArgs(connection));
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
    /// Instantiates and sets up the pipe connection (event handlers, etc.).
    /// </summary>
    /// <param name="connectionStream">The connection stream.</param>
    /// <param name="connectionPipeName">Name of the connection pipe.</param>
    /// <param name="formatter">The formatter.</param>
    /// <returns>PipeConnection.</returns>
    protected virtual PipeConnection SetupPipeConnection(
        NamedPipeServerStream connectionStream, string connectionPipeName, IFormatter formatter)
    {
        var connection = new PipeConnection(connectionStream, connectionPipeName, Formatter);

        connection.MessageReceived   += (_, args) => OnMessageReceived(args);
        connection.Disconnected      += (_, args) => OnClientDisconnected(args);
        connection.ExceptionOccurred += (_, args) => OnExceptionOccurred(args.Exception);

        return connection;
    }

    /// <inheritdoc />
    public async Task WriteAsync(byte[] value, CancellationToken cancellationToken = default)
    {
        if (Connection is not { IsConnected: true })
        {
            return;
        }

        await Connection.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException"></exception>
    [Obsolete("Cannot filter connections on a single connection server.", true)]
    public Task WriteAsync(byte[] value, string pipeName, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Cannot filter connections on a single connection server.");
    }
    
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException"></exception>
    [Obsolete("Cannot filter connections on a single connection server.", true)]
    public Task WriteAsync(byte[] value, Predicate<IPipeConnection>? predicate, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Cannot filter connections on a single connection server.");
    }
    
    /// <inheritdoc />
    public async Task WriteAsync<T>(T value, CancellationToken cancellationToken = default)
    {
        if (Connection is not { IsConnected: true })
        {
            return;
        }

        await Connection.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException"></exception>
    [Obsolete("Cannot filter connections on a single connection server.", true)]
    public Task WriteAsync<T>(T value, string pipeName, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Cannot filter connections on a single connection server.");
    }
    
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException"></exception>
    [Obsolete("Cannot filter connections on a single connection server.", true)]
    public Task WriteAsync<T>(T value, Predicate<IPipeConnection>? predicate, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Cannot filter connections on a single connection server.");
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

        await StopAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }

    #endregion
}
