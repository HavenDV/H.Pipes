using System.IO.Pipes;
using System.Text;
using H.Formatters;
using H.Pipes.Args;
using H.Pipes.Factories;
using H.Pipes.IO;
using H.Pipes.Utilities;

namespace H.Pipes;

/// <summary>
/// Wraps a <see cref="NamedPipeServerStream"/> and provides multiple simultaneous client connection handling.
/// </summary>
public class PipeServer : IPipeServer
{
    #region Properties

    /// <inheritdoc />
    public string PipeName { get; }

    /// <inheritdoc />
    public Func<string, NamedPipeServerStream>? CreatePipeStreamFunc { get; set; }

    /// <summary>
    /// If set, used instead of CreatePipeStreamFunc for connections.
    /// </summary>
    public Func<string, NamedPipeServerStream>? CreatePipeStreamForConnectionFunc { get; set; }

    /// <inheritdoc />
    public Action<NamedPipeServerStream>? PipeStreamInitializeAction { get; set; }

    /// <summary>
    /// Indicates whether to wait for a name to be released when calling StartAsync().
    /// </summary>
    public bool WaitFreePipe { get; set; }

    /// <summary>
    /// All connections(include disconnected clients).
    /// </summary>
    private List<PipeConnection> Connections { get; } = new();

    /// <summary>
    /// Connected clients.
    /// </summary>
    public IReadOnlyCollection<PipeConnection> ConnectedClients => Connections
        .Where(connection => connection.IsConnected)
        .ToList();

    /// <inheritdoc />
    public bool IsStarted => ListenWorker is { Task: { IsCompleted: false, IsCanceled: false, IsFaulted: false } };

    private TaskWorker? ListenWorker { get; set; }

    private volatile bool _isDisposed;

    #endregion

    #region Events

    /// <inheritdoc />
    public event EventHandler<ConnectionEventArgs>? ClientConnected;

    /// <inheritdoc />
    public event EventHandler<ConnectionEventArgs>? ClientDisconnected;

    /// <inheritdoc />
    public event EventHandler<ConnectionMessageEventArgs>? MessageReceived;

    /// <inheritdoc />
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
    public PipeServer(string pipeName)
    {
        PipeName = pipeName;
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
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
                    var connectionPipeName = $"{PipeName}_{Guid.NewGuid()}";

                    // Send the client the name of the data pipe to use.
                    try
                    {
#if NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER
                        var serverStream = CreatePipeStreamFunc?.Invoke(PipeName) ?? PipeServerFactory.Create(PipeName);
                        await using (serverStream.ConfigureAwait(false))
#elif NET461_OR_GREATER || NETSTANDARD2_0
                        using var serverStream = CreatePipeStreamFunc?.Invoke(PipeName) ?? PipeServerFactory.Create(PipeName);
#else
#error Target Framework is not supported
#endif
                        {
                            PipeStreamInitializeAction?.Invoke(serverStream);

                            source.TrySetResult(true);

                            await serverStream.WaitForConnectionAsync(token).ConfigureAwait(false);

#if NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER
                            using var handshakeWrapper = new PipeStreamWrapper(serverStream);
                            await using (handshakeWrapper.ConfigureAwait(false))
#elif NET461_OR_GREATER || NETSTANDARD2_0
                            using var handshakeWrapper = new PipeStreamWrapper(serverStream);
#else
#error Target Framework is not supported
#endif
                            {
                                await handshakeWrapper.WriteAsync(Encoding.UTF8.GetBytes(connectionPipeName), token)
                                    .ConfigureAwait(false);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (WaitFreePipe)
                        {
                            throw;
                        }

                        source.TrySetException(exception);
                        break;
                    }

                    // Wait for the client to connect to the data pipe.
                    var connectionStream = (CreatePipeStreamForConnectionFunc ?? CreatePipeStreamFunc)?
                        .Invoke(connectionPipeName) ?? PipeServerFactory.Create(connectionPipeName);

                    PipeStreamInitializeAction?.Invoke(connectionStream);

                    try
                    {
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

                    // Add the client's connection to the list of connections.
                    var connection = CreateConnection(connectionStream, connectionPipeName);
                    ConfigureConnection(connection);
                    connection.Start();

                    Connections.Add(connection);

                    OnClientConnected(new ConnectionEventArgs(connection));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                // Catch the IOException that is raised if the pipe is broken or disconnected.
                catch (IOException)
                {
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
    /// This method returns immediately, possibly before the message has been sent to all clients.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    public async Task WriteAsync(byte[] value, CancellationToken cancellationToken = default)
    {
        await WriteAsync(value, predicate: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to all connected clients asynchronously.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    public async Task WriteAsync(byte[] value, Predicate<PipeConnection>? predicate, CancellationToken cancellationToken = default)
    {
        var tasks = Connections
            .Where(connection => connection.IsConnected && (predicate == null || predicate(connection)))
            .Select(connection => connection.WriteAsync(value, cancellationToken))
            .ToList();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to the given client by pipe name.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="pipeName"></param>
    /// <param name="cancellationToken"></param>
    public async Task WriteAsync(byte[] value, string pipeName, CancellationToken cancellationToken = default)
    {
        await WriteAsync(value, connection => connection.PipeName == pipeName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken _ = default)
    {
        if (ListenWorker != null)
        {
            await ListenWorker.StopAsync().ConfigureAwait(false);

            ListenWorker = null;
        }

        var tasks = Connections
            .Select(connection => connection.StopAsync())
            .ToList();

        Connections.Clear();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
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
/// Wraps a <see cref="NamedPipeServerStream"/> and provides multiple simultaneous client connection handling.
/// </summary>
/// <typeparam name="T">Reference type to read/write from the named pipe.</typeparam>
public class PipeServer<T> : PipeServer, IPipeServer<T>
{
    #region Properties

    /// <inheritdoc />
    public IFormatter Formatter { get; set; }

    /// <summary>
    /// Connected clients.
    /// </summary>
    public new IReadOnlyCollection<PipeConnection<T>> ConnectedClients => base.ConnectedClients
        .OfType<PipeConnection<T>>()
        .ToList();

    #endregion

    #region Events

    /// <inheritdoc />
    public new event EventHandler<ConnectionEventArgs<T>>? ClientConnected;

    /// <inheritdoc />
    public new event EventHandler<ConnectionEventArgs<T>>? ClientDisconnected;

    /// <inheritdoc />
    public new event EventHandler<ConnectionMessageEventArgs<T?>>? MessageReceived;

    /// <summary>
    /// Invokes <see cref="MessageReceived"/>.
    /// </summary>
    /// <param name="args"></param>
    protected virtual void OnMessageReceived(ConnectionMessageEventArgs<T?> args)
    {
        MessageReceived?.Invoke(this, args);
    }

    /// <inheritdoc />
    protected override void OnClientConnected(ConnectionEventArgs args)
    {
        args = args ?? throw new ArgumentNullException(nameof(args));

        base.OnClientConnected(args);

        if (args.Connection is PipeConnection<T> connection)
        {
            ClientConnected?.Invoke(this, new ConnectionEventArgs<T>(connection));
        }
    }

    /// <inheritdoc />
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
    public PipeServer(string pipeName, IFormatter formatter) : base(pipeName)
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
    public PipeServer(string pipeName) : this(pipeName, new DefaultFormatter())
    {
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Sends a message to all connected clients asynchronously.
    /// This method returns immediately, possibly before the message has been sent to all clients.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    public async Task WriteAsync(T value, CancellationToken cancellationToken = default)
    {
        await WriteAsync(value, predicate: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to all connected clients asynchronously.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    public async Task WriteAsync(T value, Predicate<PipeConnection<T>>? predicate, CancellationToken cancellationToken = default)
    {
        var tasks = ConnectedClients
            .Where(connection => predicate == null || predicate(connection))
            .Select(connection => connection.WriteAsync(value, cancellationToken))
            .ToList();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to the given client by pipe name.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="pipeName"></param>
    /// <param name="cancellationToken"></param>
    public async Task WriteAsync(T value, string pipeName, CancellationToken cancellationToken = default)
    {
        await WriteAsync(value, connection => connection.PipeName == pipeName, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Protected methods

    /// <inheritdoc />
    protected override PipeConnection CreateConnection(PipeStream stream, string pipeName)
    {
        return new PipeConnection<T>(stream, pipeName, Formatter);
    }

    /// <inheritdoc />
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
