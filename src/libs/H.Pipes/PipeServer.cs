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
/// Specialized version of <see cref="PipeServer"/> for communications based on a single type.
/// </summary>
/// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
/// <seealso cref="H.Pipes.PipeServer" />
/// <seealso cref="H.Pipes.IPipeServer{T}" />
public class PipeServer<T> : PipeServer, IPipeServer<T>
{
    #region Constructors

    /// <inheritdoc />
    public PipeServer(string pipeName, IFormatter? formatter = default)
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
/// Wraps a <see cref="NamedPipeServerStream"/> and provides multiple simultaneous client connection handling.
/// </summary>
public class PipeServer : IPipeServer
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
    /// All connections(include disconnected clients)
    /// </summary>
    private List<PipeConnection> Connections { get; } = new();

    /// <summary>
    /// Connected clients
    /// </summary>
    public IReadOnlyCollection<PipeConnection> ConnectedClients => Connections
        .Where(connection => connection.IsConnected)
        .ToList();

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
    public PipeServer(string pipeName, IFormatter? formatter = default)
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
                    var connectionPipeName = $"{PipeName}_{Guid.NewGuid()}";

                    // Send the client the name of the data pipe to use
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

                    // Wait for the client to connect to the data pipe
                    var connectionStream = CreatePipeStreamFunc?.Invoke(connectionPipeName) ?? PipeServerFactory.Create(connectionPipeName);

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

                    // Add the client's connection to the list of connections
                    var connection = SetupPipeConnection(connectionStream, connectionPipeName, Formatter);
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
        await WriteAsync(value, predicate: null, cancellationToken).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    public async Task WriteAsync(byte[] value, string pipeName, CancellationToken cancellationToken = default)
    {
        await WriteAsync(value, connection => connection.PipeName == pipeName, cancellationToken).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    public async Task WriteAsync(byte[] value, Predicate<IPipeConnection>? predicate, CancellationToken cancellationToken = default)
    {
        var tasks = Connections
                    .Where(connection => connection.IsConnected && (predicate == null || predicate(connection)))
                    .Select(connection => connection.WriteAsync(value, cancellationToken))
                    .ToList();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    public async Task WriteAsync<T>(T value, CancellationToken cancellationToken = default)
    {
        await WriteAsync(value, predicate: null, cancellationToken).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    public async Task WriteAsync<T>(T value, string pipeName, CancellationToken cancellationToken = default)
    {
        await WriteAsync(value, connection => connection.PipeName == pipeName, cancellationToken).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    public async Task WriteAsync<T>(T value, Predicate<IPipeConnection>? predicate, CancellationToken cancellationToken = default)
    {
        var tasks = Connections
                    .Where(connection => connection.IsConnected && (predicate == null || predicate(connection)))
                    .Select(connection => connection.WriteAsync(value, cancellationToken))
                    .ToList();

        await Task.WhenAll(tasks).ConfigureAwait(false);
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

        var tasks = Connections
            .Select(connection => connection.StopAsync())
            .ToList();

        Connections.Clear();

        await Task.WhenAll(tasks).ConfigureAwait(false);
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
