using System.IO.Pipes;
using H.Formatters;
using H.Pipes.Args;
using H.Pipes.Factories;

namespace H.Pipes;

/// <summary>
/// Wraps a <see cref="NamedPipeClientStream"/>.
/// </summary>
public class SingleConnectionPipeClient : IPipeClient
{
    #region Fields

    private volatile bool _isConnecting;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public bool AutoReconnect { get; set; }

    /// <inheritdoc/>
    public TimeSpan ReconnectionInterval { get; }

    /// <inheritdoc/>
    public bool IsConnected => Connection != null;

    /// <inheritdoc/>
    public bool IsConnecting
    {
        get => _isConnecting;
        private set => _isConnecting = value;
    }

    /// <inheritdoc/>
    public Func<string, string, NamedPipeClientStream>? CreatePipeStreamFunc { get; set; }

    /// <inheritdoc/>
    public string PipeName { get; }

    /// <inheritdoc/>
    public string ServerName { get; }

    /// <inheritdoc/>
    public PipeConnection? Connection { get; private set; }

    private System.Timers.Timer ReconnectionTimer { get; }

    #endregion

    #region Events

    /// <summary>
    /// Invoked whenever a message is received from the server.
    /// </summary>
    public event EventHandler<ConnectionMessageEventArgs>? MessageReceived;

    /// <summary>
    /// Invoked when the client disconnects from the server (e.g., the pipe is closed or broken).
    /// </summary>
    public event EventHandler<ConnectionEventArgs>? Disconnected;

    /// <summary>
    /// Invoked after each the client connect to the server (include reconnects).
    /// </summary>
    public event EventHandler<ConnectionEventArgs>? Connected;

    /// <summary>
    /// Invoked whenever an exception is thrown during a read or write operation on the named pipe.
    /// </summary>
    public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

    /// <summary>
    /// Invokes <see cref="MessageReceived"/>.
    /// </summary>
    /// <param name="args"></param>
    protected virtual void OnMessageReceived(ConnectionMessageEventArgs args)
    {
        MessageReceived?.Invoke(this, args);
    }

    /// <summary>
    /// Invokes <see cref="Disconnected"/>.
    /// </summary>
    /// <param name="args"></param>
    protected virtual void OnDisconnected(ConnectionEventArgs args)
    {
        Disconnected?.Invoke(this, args);
    }

    /// <summary>
    /// Invokes <see cref="Connected"/>.
    /// </summary>
    /// <param name="args"></param>
    protected virtual void OnConnected(ConnectionEventArgs args)
    {
        Connected?.Invoke(this, args);
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
    /// Constructs a new <see cref="SingleConnectionPipeClient"/> to connect to the <see cref="SingleConnectionPipeServer"/> specified by <paramref name="pipeName"/>. <br/>
    /// Default reconnection interval - <see langword="100 ms"/>.
    /// </summary>
    /// <param name="pipeName">Name of the server's pipe.</param>
    /// <param name="serverName">The name of the server, default is local machine.</param>
    /// <param name="reconnectionInterval">Default reconnection interval - <see langword="100 ms"/>.</param>
    public SingleConnectionPipeClient(string pipeName, string serverName = ".", TimeSpan? reconnectionInterval = default)
    {
        PipeName = pipeName;
        ServerName = serverName;
        AutoReconnect = true;

        ReconnectionInterval = reconnectionInterval ?? TimeSpan.FromMilliseconds(100);
        ReconnectionTimer = new System.Timers.Timer(ReconnectionInterval.TotalMilliseconds);
        ReconnectionTimer.Elapsed += async (_, _) =>
        {
            try
            {
                if (!IsConnected && !IsConnecting)
                {
                    using var cancellationTokenSource = new CancellationTokenSource(ReconnectionInterval);

                    try
                    {
                        await ConnectAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }
            catch (Exception exception)
            {
                ReconnectionTimer.Stop();

                OnExceptionOccurred(exception);
            }
        };
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsConnecting = true;

            if (AutoReconnect)
            {
                ReconnectionTimer.Start();
            }
            if (IsConnected)
            {
                throw new InvalidOperationException("Already connected");
            }

#pragma warning disable CA2000 // Dispose objects before losing scope
            var dataPipe = await PipeClientFactory
                .CreateAndConnectAsync(PipeName, ServerName, CreatePipeStreamFunc, cancellationToken)
#pragma warning restore CA2000 // Dispose objects before losing scope
                    .ConfigureAwait(false);

            var connection = CreateConnection(dataPipe, PipeName, ServerName);
            Connection = connection;
            ConfigureConnection(connection);
            connection.Start();

            OnConnected(new ConnectionEventArgs(connection));
        }
        catch (Exception)
        {
            ReconnectionTimer.Stop();

            throw;
        }
        finally
        {
            IsConnecting = false;
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(CancellationToken _ = default)
    {
        ReconnectionTimer.Stop();

        await DisconnectInternalAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to the server over a named pipe. <br/>
    /// If client is not connected, <see cref="InvalidOperationException"/> is occurred.
    /// </summary>
    /// <param name="value">Message to send to the server.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task WriteAsync(byte[] value, CancellationToken cancellationToken = default)
    {
        if (!IsConnected && AutoReconnect && !IsConnecting)
        {
            await ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        while (IsConnecting)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);
        }
        if (Connection == null)
        {
            throw new InvalidOperationException("Client is not connected");
        }

        await Connection.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region IDisposable

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        ReconnectionTimer.Dispose();

        await DisconnectInternalAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Creates the data connection used by this client.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="pipeName"></param>
    /// <param name="serverName"></param>
    /// <returns></returns>
    protected virtual PipeConnection CreateConnection(PipeStream stream, string pipeName, string serverName)
    {
        return new PipeConnection(stream, pipeName, serverName);
    }

    /// <summary>
    /// Subscribes to connection events.
    /// </summary>
    /// <param name="connection"></param>
    protected virtual void ConfigureConnection(PipeConnection connection)
    {
        connection = connection ?? throw new ArgumentNullException(nameof(connection));

        connection.Disconnected += async (_, args) =>
        {
            try
            {
                await DisconnectInternalAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                OnExceptionOccurred(exception);
            }

            OnDisconnected(args);
        };
        connection.MessageReceived += (_, args) => OnMessageReceived(args);
        connection.ExceptionOccurred += (_, args) => OnExceptionOccurred(args.Exception);
    }

    #endregion

    #region Private methods

    private async Task DisconnectInternalAsync()
    {
        if (Connection == null)
        {
            return;
        }

        await Connection.StopAsync().ConfigureAwait(false);

        Connection = null;
    }

    #endregion
}

/// <summary>
/// Wraps a <see cref="NamedPipeClientStream"/>.
/// </summary>
/// <typeparam name="T">Reference type to read/write from the named pipe.</typeparam>
public class SingleConnectionPipeClient<T> : SingleConnectionPipeClient, IPipeClient<T>
{
    #region Properties

    /// <inheritdoc/>
    public IFormatter Formatter { get; }

    /// <inheritdoc/>
    public new PipeConnection<T>? Connection => base.Connection as PipeConnection<T>;

    #endregion

    #region Events

    /// <summary>
    /// Invoked whenever a message is received from the server.
    /// </summary>
    public new event EventHandler<ConnectionMessageEventArgs<T?>>? MessageReceived;

    /// <summary>
    /// Invoked when the client disconnects from the server (e.g., the pipe is closed or broken).
    /// </summary>
    public new event EventHandler<ConnectionEventArgs<T>>? Disconnected;

    /// <summary>
    /// Invoked after each the client connect to the server (include reconnects).
    /// </summary>
    public new event EventHandler<ConnectionEventArgs<T>>? Connected;

    /// <summary>
    /// Invokes <see cref="MessageReceived"/>.
    /// </summary>
    /// <param name="args"></param>
    protected virtual void OnMessageReceived(ConnectionMessageEventArgs<T?> args)
    {
        MessageReceived?.Invoke(this, args);
    }

    /// <inheritdoc/>
    protected override void OnDisconnected(ConnectionEventArgs args)
    {
        args = args ?? throw new ArgumentNullException(nameof(args));

        base.OnDisconnected(args);

        if (args.Connection is PipeConnection<T> connection)
        {
            Disconnected?.Invoke(this, new ConnectionEventArgs<T>(connection));
        }
    }

    /// <inheritdoc/>
    protected override void OnConnected(ConnectionEventArgs args)
    {
        args = args ?? throw new ArgumentNullException(nameof(args));

        base.OnConnected(args);

        if (args.Connection is PipeConnection<T> connection)
        {
            Connected?.Invoke(this, new ConnectionEventArgs<T>(connection));
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Constructs a new <see cref="PipeClient{T}"/> to connect to the <see cref="PipeServer{T}"/> specified by <paramref name="pipeName"/>. <br/>
    /// Default reconnection interval - <see langword="100 ms"/>.
    /// </summary>
    /// <param name="pipeName">Name of the server's pipe.</param>
    /// <param name="serverName">The name of the server, default is local machine.</param>
    /// <param name="reconnectionInterval">Default reconnection interval - <see langword="100 ms"/>.</param>
    /// <param name="formatter">Default formatter - <see cref="DefaultFormatter"/>.</param>
    public SingleConnectionPipeClient(string pipeName, IFormatter formatter, string serverName = ".", TimeSpan? reconnectionInterval = default)
        : base(pipeName, serverName, reconnectionInterval)
    {
        Formatter = formatter;
    }

    /// <summary>
    /// Constructs a new <see cref="PipeClient{T}"/> to connect to the <see cref="PipeServer{T}"/> specified by <paramref name="pipeName"/>. <br/>
    /// Default reconnection interval - <see langword="100 ms"/>.
    /// </summary>
    /// <param name="pipeName">Name of the server's pipe.</param>
    /// <param name="serverName">The name of the server, default is local machine.</param>
    /// <param name="reconnectionInterval">Default reconnection interval - <see langword="100 ms"/>.</param>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
#endif
    public SingleConnectionPipeClient(string pipeName, string serverName = ".", TimeSpan? reconnectionInterval = default)
        : this(pipeName, new DefaultFormatter(), serverName, reconnectionInterval)
    {
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Sends a message to the server over a named pipe. <br/>
    /// If client is not connected, <see cref="InvalidOperationException"/> is occurred.
    /// </summary>
    /// <param name="value">Message to send to the server.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task WriteAsync(T value, CancellationToken cancellationToken = default)
    {
        if (!IsConnected && AutoReconnect && !IsConnecting)
        {
            await ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        while (IsConnecting)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);
        }
        if (Connection == null)
        {
            throw new InvalidOperationException("Client is not connected");
        }

        await Connection.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Protected methods

    /// <inheritdoc/>
    protected override PipeConnection CreateConnection(PipeStream stream, string pipeName, string serverName)
    {
        return new PipeConnection<T>(stream, pipeName, Formatter, serverName);
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
