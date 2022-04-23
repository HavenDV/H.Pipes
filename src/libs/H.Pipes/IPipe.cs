using H.Formatters;
using H.Pipes.Args;

namespace H.Pipes;

/// <summary>
/// Base class of all connections
/// </summary>
public interface IPipe : IAsyncDisposable
{
    #region Properties

    /// <summary>
    /// Used formatter
    /// </summary>
    public IFormatter Formatter { get; }

    #endregion

    #region Events

    /// <summary>
    /// Invoked whenever a message is received.
    /// </summary>
    event EventHandler<ConnectionMessageEventArgs<byte[]?>>? MessageReceived;

    /// <summary>
    /// Invoked whenever an exception is thrown during a read or write operation on the named pipe.
    /// </summary>
    event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

    #endregion

    #region Methods

    /// <summary>
    /// Sends a message over a named pipe. <br/>
    /// </summary>
    /// <param name="value">Message to send</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidOperationException"></exception>
    Task WriteAsync(byte[] value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to all connected clients asynchronously.
    /// This method returns immediately, possibly before the message has been sent to all clients.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    Task WriteAsync<T>(T value, CancellationToken cancellationToken = default);

    #endregion
}
