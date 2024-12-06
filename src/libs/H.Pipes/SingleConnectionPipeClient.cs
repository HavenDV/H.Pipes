using System.IO.Pipes;
using H.Formatters;
using H.Pipes.Args;
using H.Pipes.Factories;

namespace H.Pipes;

/// <summary>
/// Wraps a <see cref="NamedPipeClientStream"/>.
/// </summary>
/// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
public sealed class SingleConnectionPipeClient<T> : IPipeClient<T>
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
    public IFormatter Formatter { get; }
    
    /// <inheritdoc/>
    public Func<string, string, NamedPipeClientStream>? CreatePipeStreamFunc { get; set; }

    /// <inheritdoc/>
    public string PipeName { get; }

    /// <inheritdoc/>
    public string ServerName { get; }

    /// <inheritdoc/>
    public PipeConnection<T>? Connection { get; private set; }

    private System.Timers.Timer ReconnectionTimer { get; }

    #endregion

    #region Events

    /// <summary>
    /// Invoked whenever a message is received from the server.
    /// </summary>
    public event EventHandler<ConnectionMessageEventArgs<T?>>? MessageReceived;

    /// <summary>
    /// Invoked when the client disconnects from the server (e.g., the pipe is closed or broken).
    /// </summary>
    public event EventHandler<ConnectionEventArgs<T>>? Disconnected;

    /// <summary>
    /// Invoked after each the client connect to the server (include reconnects).
    /// </summary>
    public event EventHandler<ConnectionEventArgs<T>>? Connected;

    /// <summary>
    /// Invoked whenever an exception is thrown during a read or write operation on the named pipe.
    /// </summary>
    public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

    private void OnMessageReceived(ConnectionMessageEventArgs<T?> args)
    {
        MessageReceived?.Invoke(this, args);
    }

    private void OnDisconnected(ConnectionEventArgs<T> args)
    {
        Disconnected?.Invoke(this, args);
    }

    private void OnConnected(ConnectionEventArgs<T> args)
    {
        Connected?.Invoke(this, args);
    }

    private void OnExceptionOccurred(Exception exception)
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
    /// <param name="formatter">Default formatter - <see cref="DefaultFormatter"/></param>
    public SingleConnectionPipeClient(string pipeName, IFormatter formatter, string serverName = ".", TimeSpan? reconnectionInterval = default)
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

        Formatter = formatter;
    }
    
    /// <summary>
    /// Constructs a new <see cref="PipeClient{T}"/> to connect to the <see cref="PipeServer{T}"/> specified by <paramref name="pipeName"/>. <br/>
    /// Default reconnection interval - <see langword="100 ms"/>
    /// </summary>
    /// <param name="pipeName">Name of the server's pipe</param>
    /// <param name="serverName">the Name of the server, default is  local machine</param>
    /// <param name="reconnectionInterval">Default reconnection interval - <see langword="100 ms"/></param>
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

            Connection = new PipeConnection<T>(dataPipe, PipeName, Formatter, ServerName);
            Connection.Disconnected += async (_, args) =>
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
            Connection.MessageReceived += (_, args) => OnMessageReceived(args);
            Connection.ExceptionOccurred += (_, args) => OnExceptionOccurred(args.Exception);
            Connection.Start();

            OnConnected(new ConnectionEventArgs<T>(Connection));
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

    private async Task DisconnectInternalAsync()
    {
        if (Connection == null)
        {
            return;
        }

        await Connection.StopAsync().ConfigureAwait(false);

        Connection = null;
    }

    /// <summary>
    /// Sends a message to the server over a named pipe. <br/>
    /// If client is not connected, <see cref="InvalidOperationException"/> is occurred
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
        if (Connection == null) // nullable detection system is not very smart
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
    }

    #endregion
}
