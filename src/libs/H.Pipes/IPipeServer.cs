using System.IO.Pipes;
using H.Pipes.Args;

namespace H.Pipes;

/// <summary>
/// Specialized version of <see cref="IPipeServer"/> for communications based
/// on a single type
/// </summary>
/// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
/// <seealso cref="H.Pipes.IPipeServer" />
public interface IPipeServer<T> : IPipeServer
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
/// 
/// </summary>
public interface IPipeServer : IPipe
{
    #region Properties

    /// <summary>
    /// Name of pipe
    /// </summary>
    string PipeName { get; }

    /// <summary>
    /// CreatePipeStreamFunc
    /// </summary>
    Func<string, NamedPipeServerStream>? CreatePipeStreamFunc { get; set; }

    /// <summary>
    /// PipeStreamInitializeAction
    /// </summary>
    Action<NamedPipeServerStream>? PipeStreamInitializeAction { get; set; }

    /// <summary>
    /// IsStarted
    /// </summary>
    bool IsStarted { get; }

    #endregion

    #region Events

    /// <summary>
    /// Invoked whenever a client connects to the server.
    /// </summary>
    event EventHandler<ConnectionEventArgs>? ClientConnected;

    /// <summary>
    /// Invoked whenever a client disconnects from the server.
    /// </summary>
    event EventHandler<ConnectionEventArgs>? ClientDisconnected;

    #endregion

    #region Methods

    /// <summary>
    /// Begins listening for client connections in a separate background thread.
    /// This method waits when pipe will be created(or throws exception).
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="IOException"></exception>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes all open client connections and stops listening for new ones.
    /// </summary>
    Task StopAsync(CancellationToken _ = default);

    /// <summary>
    /// Sends a message to all connected clients asynchronously.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    Task WriteAsync(byte[] value, Predicate<IPipeConnection>? predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the given client by pipe name.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="pipeName"></param>
    /// <param name="cancellationToken"></param>
    Task WriteAsync(byte[] value, string pipeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to all connected clients asynchronously.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    Task WriteAsync<T>(T value, Predicate<IPipeConnection>? predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the given client by pipe name.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="pipeName"></param>
    /// <param name="cancellationToken"></param>
    Task WriteAsync<T>(T value, string pipeName, CancellationToken cancellationToken = default);

    #endregion
}
