using System.IO.Pipes;
using H.Pipes.Args;

namespace H.Pipes;

/// <summary>
/// Specialized version of <see cref="IPipeClient"/> for communications based
/// on a single type
/// </summary>
/// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
/// <seealso cref="H.Pipes.IPipeClient" />
public interface IPipeClient<T> : IPipeClient
{

    #region Events

    /// <summary>
    /// Invoked whenever a message is received.
    /// </summary>
    new event EventHandler<ConnectionMessageEventArgs<T?>>? MessageReceived;

    #endregion

    #region Methods

    /// <summary>
    /// Sends a message over a named pipe. <br/>
    /// </summary>
    /// <param name="value">Message to send</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidOperationException"></exception>
    Task WriteAsync(T value, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Wraps a <see cref="NamedPipeClientStream"/>.
/// </summary>
public interface IPipeClient : IPipe
{
    #region Properties

    /// <summary>
    /// Gets or sets whether the client should attempt to reconnect when the pipe breaks
    /// due to an error or the other end terminating the connection. <br/>
    /// Default value is <see langword="true"/>.
    /// </summary>
    bool AutoReconnect { get; set; }

    /// <summary>
    /// Interval of reconnection.
    /// </summary>
    TimeSpan ReconnectionInterval { get; }

    /// <summary>
    /// Checks that connection is exists.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// <see langword="true"/> if <see cref="ConnectAsync"/> in process.
    /// </summary>
    bool IsConnecting { get; }

    /// <summary>
    /// Used pipe name.
    /// </summary>
    public string PipeName { get; }

    /// <summary>
    /// Used server name.
    /// </summary>
    public string ServerName { get; }

    /// <summary>
    /// Active connection.
    /// </summary>
    public PipeConnection? Connection { get; }

    #endregion

    #region Events

    /// <summary>
    /// Invoked after each the client connect to the server (include reconnects).
    /// </summary>
    event EventHandler<ConnectionEventArgs>? Connected;

    /// <summary>
    /// Invoked when the client disconnects from the server (e.g., the pipe is closed or broken).
    /// </summary>
    event EventHandler<ConnectionEventArgs>? Disconnected;

    #endregion

    #region Methods

    /// <summary>
    /// Connects to the named pipe server asynchronously.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from server
    /// </summary>
    /// <param name="_"></param>
    /// <returns></returns>
    Task DisconnectAsync(CancellationToken _ = default);

    #endregion
}
