using System.IO.Pipes;
using H.Formatters;
using H.Pipes.Args;
using H.Pipes.Factories;
using H.Pipes.Utilities;

namespace H.Pipes;

/// <summary>
/// Wraps a <see cref="NamedPipeServerStream"/> and optimized for one connection.
/// </summary>
public class SingleConnectionPipeServer : IPipeServer
{
    #region Properties

    /// <inheritdoc/>
    public string PipeName { get; }

    /// <inheritdoc/>
    public Func<string, NamedPipeServerStream>? CreatePipeStreamFunc { get; set; }

    /// <inheritdoc/>
    public Action<NamedPipeServerStream>? PipeStreamInitializeAction { get; set; }

    /// <summary>
    /// Indicates whether to wait for a name to be released when calling StartAsync().
    /// </summary>
    public bool WaitFreePipe { get; set; }

    /// <summary>
    /// Connection.
    /// </summary>
    public PipeConnection? Connection { get; private set; }

    /// <inheritdoc/>
    public bool IsStarted => ListenWorker != null && !ListenWorker.Task.IsCompleted && !ListenWorker.Task.IsCanceled && !ListenWorker.Task.IsFaulted;

    private TaskWorker? ListenWorker { get; set; }

    private volatile bool _isDisposed;

    #endregion

    #region Events

    /// <inheritdoc/>
    public event EventHandler<ConnectionEventArgs>? ClientConnected;

    /// <inheritdoc/>
    public event EventHandler<ConnectionEventArgs>? ClientDisconnected;

    /// <inheritdoc/>
    public event EventHandler<ConnectionMessageEventArgs>? MessageReceived;

    /// <inheritdoc/>
    public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

    /// <summary>
    /// Invokes <see cref="ClientConnected"/>.
    /// </summary>
    /// <param name="args"></param>
    protected virtual void OnClientConnected(ConnectionEventArgs args)
    {
        ClientConnected?.Invoke(this, args);
    }

    /// <summary>
    /// Invokes <see cref="ClientDisconnected"/>.
    /// </summary>
    /// <param name="args"></param>
    protected virtual void OnClientDisconnected(ConnectionEventArgs args)
    {
        ClientDisconnected?.Invoke(this, args);
    }

    /// <summary>
    /// Invokes <see cref="MessageReceived"/>.
    /// </summary>
    /// <param name="args"></param>
    protected virtual void OnMessageReceived(ConnectionMessageEventArgs args)
    {
        MessageReceived?.Invoke(this, args);
    }

    /// <summary>
    /// Invokes <see cref="ExceptionOccurred"/>.
    /// </summary>
    /// <param name="exception"></param>
    protected virtual void OnExceptionOccurred(Exception exception)
    {
        ExceptionOccurred?.Invoke(this, new ExceptionEventArgs(exception));
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Constructs a new <c>NamedPipeServer</c> object that listens for client connections on the given <paramref name="pipeName"/>.
    /// </summary>
    /// <param name="pipeName">Name of the pipe to listen on.</param>
    public SingleConnectionPipeServer(string pipeName)
    {
        PipeName = pipeName;
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
                        await Task.Delay(TimeSpan.FromMilliseconds(1), token).ConfigureAwait(false);
                        continue;
                    }

                    if (Connection != null)
                    {
                        await Connection.StopAsync().ConfigureAwait(false);
                    }

                    // Wait for the client to connect to the data pipe.
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

                    var connection = CreateConnection(connectionStream, PipeName);
                    try
                    {
                        ConfigureConnection(connection);
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
    /// Sends a message to all connected clients asynchronously.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    public async Task WriteAsync(byte[] value, CancellationToken cancellationToken = default)
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

        GC.SuppressFinalize(this);
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Creates a connection for a connected client stream.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="pipeName"></param>
    /// <returns></returns>
    protected virtual PipeConnection CreateConnection(PipeStream stream, string pipeName)
    {
        return new PipeConnection(stream, pipeName);
    }

    /// <summary>
    /// Subscribes to connection events.
    /// </summary>
    /// <param name="connection"></param>
    protected virtual void ConfigureConnection(PipeConnection connection)
    {
        connection = connection ?? throw new ArgumentNullException(nameof(connection));

        connection.MessageReceived += (_, args) => OnMessageReceived(args);
        connection.Disconnected += (_, args) => OnClientDisconnected(args);
        connection.ExceptionOccurred += (_, args) => OnExceptionOccurred(args.Exception);
    }

    #endregion
}

/// <summary>
/// Wraps a <see cref="NamedPipeServerStream"/> and optimized for one connection.
/// </summary>
/// <typeparam name="T">Reference type to read/write from the named pipe.</typeparam>
public class SingleConnectionPipeServer<T> : SingleConnectionPipeServer, IPipeServer<T>
{
    #region Properties

    /// <inheritdoc/>
    public IFormatter Formatter { get; set; }

    /// <summary>
    /// Connection.
    /// </summary>
    public new PipeConnection<T>? Connection => base.Connection as PipeConnection<T>;

    #endregion

    #region Events

    /// <inheritdoc/>
    public new event EventHandler<ConnectionEventArgs<T>>? ClientConnected;

    /// <inheritdoc/>
    public new event EventHandler<ConnectionEventArgs<T>>? ClientDisconnected;

    /// <inheritdoc/>
    public new event EventHandler<ConnectionMessageEventArgs<T?>>? MessageReceived;

    /// <summary>
    /// Invokes <see cref="MessageReceived"/>.
    /// </summary>
    /// <param name="args"></param>
    protected virtual void OnMessageReceived(ConnectionMessageEventArgs<T?> args)
    {
        MessageReceived?.Invoke(this, args);
    }

    /// <inheritdoc/>
    protected override void OnClientConnected(ConnectionEventArgs args)
    {
        args = args ?? throw new ArgumentNullException(nameof(args));

        base.OnClientConnected(args);

        if (args.Connection is PipeConnection<T> connection)
        {
            ClientConnected?.Invoke(this, new ConnectionEventArgs<T>(connection));
        }
    }

    /// <inheritdoc/>
    protected override void OnClientDisconnected(ConnectionEventArgs args)
    {
        args = args ?? throw new ArgumentNullException(nameof(args));

        base.OnClientDisconnected(args);

        if (args.Connection is PipeConnection<T> connection)
        {
            ClientDisconnected?.Invoke(this, new ConnectionEventArgs<T>(connection));
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Constructs a new <c>NamedPipeServer</c> object that listens for client connections on the given <paramref name="pipeName"/>.
    /// </summary>
    /// <param name="pipeName">Name of the pipe to listen on.</param>
    /// <param name="formatter">Default formatter - <see cref="DefaultFormatter"/>.</param>
    public SingleConnectionPipeServer(string pipeName, IFormatter formatter) : base(pipeName)
    {
        Formatter = formatter;
    }

    /// <summary>
    /// Constructs a new <c>NamedPipeServer</c> object that listens for client connections on the given <paramref name="pipeName"/>.
    /// </summary>
    /// <param name="pipeName">Name of the pipe to listen on.</param>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
#endif
    public SingleConnectionPipeServer(string pipeName) : this(pipeName, new DefaultFormatter())
    {
    }

    #endregion

    #region Public methods

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

    #endregion

    #region Protected methods

    /// <inheritdoc/>
    protected override PipeConnection CreateConnection(PipeStream stream, string pipeName)
    {
        return new PipeConnection<T>(stream, pipeName, Formatter);
    }

    /// <inheritdoc/>
    protected override void ConfigureConnection(PipeConnection connection)
    {
        base.ConfigureConnection(connection);

        if (connection is PipeConnection<T> typedConnection)
        {
            typedConnection.MessageReceived += (_, args) => OnMessageReceived(args);
        }
    }

    #endregion
}
