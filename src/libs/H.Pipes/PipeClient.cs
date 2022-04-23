using System.IO.Pipes;
using System.Text;
using H.Formatters;
using H.Pipes.Args;
using H.Pipes.Factories;

namespace H.Pipes;

/// <summary>
/// Wraps a <see cref="NamedPipeClientStream"/>.
/// Specialized version of <see cref="PipeClient"/> for communications based on a single type.
/// Implements the <see cref="H.Pipes.PipeClient" />
/// Implements the <see cref="H.Pipes.IPipeClient{T}" />
/// </summary>
/// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
/// <seealso cref="H.Pipes.PipeClient" />
/// <seealso cref="H.Pipes.IPipeClient{T}" />
public class PipeClient<T> : PipeClient, IPipeClient<T>
{
    #region Constructors
    
    /// <inheritdoc />
    public PipeClient(string pipeName, string serverName = ".", TimeSpan? reconnectionInterval = default, IFormatter? formatter = default) : base(pipeName, serverName, reconnectionInterval, formatter) { }

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
    protected override PipeConnection SetupPipeConnection(
        PipeStream dataPipe, string connectionPipeName, IFormatter formatter, string serverName)
    {
        var connection = new PipeConnection<T>(dataPipe, connectionPipeName, formatter, serverName);

        connection.Disconnected += async (_, args) =>
        {
            await DisconnectInternalAsync().ConfigureAwait(false);

            OnDisconnected(args);
        };
        connection.MessageReceived   += (_, args) => OnMessageReceived(args);
        connection.ExceptionOccurred += (_, args) => OnExceptionOccurred(args.Exception);

        return connection;
    }

    #endregion
}

/// <summary>
/// Wraps a <see cref="NamedPipeClientStream"/>.
/// Implements the <see cref="H.Pipes.IPipeClient" />
/// </summary>
/// <seealso cref="H.Pipes.IPipeClient" />
public class PipeClient : IPipeClient
{
    #region Fields

    private volatile bool _isConnecting;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public bool AutoReconnect { get; set; } = true;

    /// <inheritdoc/>
    public TimeSpan ReconnectionInterval { get; }

    /// <inheritdoc/>
    public bool IsConnected => Connection != null;

    /// <inheritdoc/>
    public bool IsConnecting
    {
        get => _isConnecting;
        protected set => _isConnecting = value;
    }

    /// <inheritdoc/>
    public IFormatter Formatter { get; }

    /// <inheritdoc/>
    public string PipeName { get; }

    /// <inheritdoc/>
    public string ServerName { get; }

    /// <inheritdoc/>
    public PipeConnection? Connection { get; protected set; }

    /// <summary>
    /// Gets the reconnection timer.
    /// </summary>
    /// <value>The reconnection timer.</value>
    protected System.Timers.Timer ReconnectionTimer { get; }

    #endregion

    #region Events

    /// <summary>
    /// Invoked whenever a message is received from the server.
    /// </summary>
    public event EventHandler<ConnectionMessageEventArgs<byte[]?>>? MessageReceived;

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
    /// Calls the <see cref="Disconnected"/> event.
    /// </summary>
    /// <param name="args">The <see cref="ConnectionEventArgs"/> instance containing the event data.</param>
    protected void OnDisconnected(ConnectionEventArgs args)
    {
        Disconnected?.Invoke(this, args);
    }

    /// <summary>
    /// Calls the <see cref="Connected"/> event.
    /// </summary>
    /// <param name="args">The <see cref="ConnectionEventArgs"/> instance containing the event data.</param>
    protected void OnConnected(ConnectionEventArgs args)
    {
        Connected?.Invoke(this, args);
    }
    
    /// <summary>
    /// Calls the <see cref="MessageReceived"/> event.
    /// </summary>
    /// <param name="args">The instance containing the event data.</param>
    protected void OnMessageReceived(ConnectionMessageEventArgs<byte[]?> args)
    {
        MessageReceived?.Invoke(this, args);
    }

    /// <summary>
    /// Calls the <see cref="ExceptionOccurred"/> event.
    /// </summary>
    /// <param name="exception">The exception.</param>
    protected void OnExceptionOccurred(Exception exception)
    {
        ExceptionOccurred?.Invoke(this, new ExceptionEventArgs(exception));
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Constructs a new <see cref="PipeClient{T}"/> to connect to the <see cref="PipeServer{T}"/> specified by <paramref name="pipeName"/>. <br/>
    /// Default reconnection interval - <see langword="100 ms"/>
    /// </summary>
    /// <param name="pipeName">Name of the server's pipe</param>
    /// <param name="serverName">the Name of the server, default is  local machine</param>
    /// <param name="reconnectionInterval">Default reconnection interval - <see langword="100 ms"/></param>
    /// <param name="formatter">Default formatter - <see cref="BinaryFormatter"/></param>
    public PipeClient(string pipeName, string serverName = ".", TimeSpan? reconnectionInterval = default, IFormatter? formatter = default)
    {
        PipeName = pipeName;
        ServerName = serverName;

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

        Formatter = formatter ?? new BinaryFormatter();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Connects to the named pipe server asynchronously.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        while (IsConnecting)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);
        }

        if (IsConnected)
        {
            return;
        }

        try
        {
            IsConnecting = true;

            if (AutoReconnect)
            {
                ReconnectionTimer.Start();
            }

            var connectionPipeName = await GetConnectionPipeName(cancellationToken).ConfigureAwait(false);

            // Connect to the actual data pipe
#pragma warning disable CA2000 // Dispose objects before losing scope
            var dataPipe = await PipeClientFactory
                .CreateAndConnectAsync(connectionPipeName, ServerName, cancellationToken)
#pragma warning restore CA2000 // Dispose objects before losing scope
                    .ConfigureAwait(false);

            Connection = SetupPipeConnection(dataPipe, connectionPipeName, Formatter, ServerName);
            Connection.Start();

            OnConnected(new ConnectionEventArgs(Connection));
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

    /// <summary>
    /// Disconnects from server
    /// </summary>
    /// <param name="_"></param>
    /// <returns></returns>
    public async Task DisconnectAsync(CancellationToken _ = default)
    {
        ReconnectionTimer.Stop();

        await DisconnectInternalAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Disconnects from the server. Does not stop <see cref="ReconnectionTimer"/>.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    protected async Task DisconnectInternalAsync()
    {
        if (Connection == null)
        {
            return;
        }

        await Connection.DisposeAsync().ConfigureAwait(false);

        Connection = null;
    }

    /// <summary>
    /// Instantiates and sets up the pipe connection (event handlers, etc.).
    /// </summary>
    /// <param name="dataPipe">The pipe stream.</param>
    /// <param name="connectionPipeName">Name of the connection pipe.</param>
    /// <param name="formatter">The formatter.</param>
    /// <param name="serverName"></param>
    /// <returns>PipeConnection.</returns>
    protected virtual PipeConnection SetupPipeConnection(
        PipeStream dataPipe, string connectionPipeName, IFormatter formatter, string serverName)
    {
        var connection = new PipeConnection(dataPipe, connectionPipeName, formatter, serverName);

        connection.Disconnected += async (_, args) =>
        {
            await DisconnectInternalAsync().ConfigureAwait(false);

            OnDisconnected(args);
        };
        connection.MessageReceived   += (_, args) => OnMessageReceived(args);
        connection.ExceptionOccurred += (_, args) => OnExceptionOccurred(args.Exception);

        return connection;
    }

    /// <summary>
    /// Sends a message to the server over a named pipe. <br/>
    /// If client is not connected, <see cref="InvalidOperationException"/> is occurred
    /// </summary>
    /// <param name="value">Message to send to the server.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task WriteAsync(byte[] value, CancellationToken cancellationToken = default)
    {
        await ReconnectOrThrow(cancellationToken).ConfigureAwait(false);

        await Connection!.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to the server over a named pipe. <br/>
    /// If client is not connected, <see cref="InvalidOperationException"/> is occurred
    /// </summary>
    /// <param name="value">Message to send to the server.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task WriteAsync<T>(T value, CancellationToken cancellationToken = default)
    {
        await ReconnectOrThrow(cancellationToken).ConfigureAwait(false);

        await Connection!.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reconnects the client if needed and throws an exception when failed.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <exception cref="System.InvalidOperationException">Client is not connected</exception>
    protected async Task ReconnectOrThrow(CancellationToken cancellationToken = default)
    {
        if (!IsConnected && AutoReconnect)
        {
            await ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        if (Connection == null)
        {
            throw new InvalidOperationException("Client is not connected");
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Dispose internal resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        ReconnectionTimer.Dispose();

        await DisconnectInternalAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Get the name of the data pipe that should be used from now on by this NamedPipeClient
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    /// <returns></returns>
    private async Task<string> GetConnectionPipeName(CancellationToken cancellationToken = default)
    {
#if NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER
#pragma warning disable CA2000 // Dispose objects before losing scope
        var handshake = await PipeClientFactory.ConnectAsync(PipeName, ServerName, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA2000 // Dispose objects before losing scope
        await using (handshake.ConfigureAwait(false))
#elif NET461_OR_GREATER || NETSTANDARD2_0
        using var handshake = await PipeClientFactory.ConnectAsync(PipeName, ServerName, cancellationToken).ConfigureAwait(false);
#else
#error Target Framework is not supported
#endif
        {
            var bytes = await handshake.ReadAsync(cancellationToken).ConfigureAwait(false);

            if (bytes == null)
            {
                throw new InvalidOperationException("Connection failed: Returned by server pipeName is null");
            }

            return Encoding.UTF8.GetString(bytes);
        }
    }

    #endregion
}
