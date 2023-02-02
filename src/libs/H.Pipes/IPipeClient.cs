using System.IO.Pipes;
using H.Pipes.Args;

namespace H.Pipes;

/// <summary>
/// Wraps a <see cref="NamedPipeClientStream"/>.
/// </summary>
/// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
public interface IPipeClient<T> : IPipeConnection<T>
{
    #region Properties

    /// <summary>
    /// Used pipe name.
    /// </summary>
    public string PipeName { get; }

    /// <summary>
    /// First argument: pipeName, Second argument: serverName.
    /// </summary>
    Func<string, string, NamedPipeClientStream>? CreatePipeStreamFunc { get; set; }

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
    /// Used server name.
    /// </summary>
    public string ServerName { get; }

    /// <summary>
    /// Active connection.
    /// </summary>
    public PipeConnection<T>? Connection { get; }

    #endregion

    #region Events

    /// <summary>
    /// Invoked after each the client connect to the server (include reconnects).
    /// </summary>
    event EventHandler<ConnectionEventArgs<T>>? Connected;

    /// <summary>
    /// Invoked when the client disconnects from the server (e.g., the pipe is closed or broken).
    /// </summary>
    event EventHandler<ConnectionEventArgs<T>>? Disconnected;

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
