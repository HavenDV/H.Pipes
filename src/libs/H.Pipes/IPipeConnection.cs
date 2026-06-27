using H.Formatters;
using H.Pipes.Args;

namespace H.Pipes;

/// <summary>
/// Base interface for raw byte pipe connections.
/// </summary>
public interface IPipeConnection : IAsyncDisposable
{
    #region Events

    /// <summary>
    /// Invoked whenever a message is received.
    /// </summary>
    event EventHandler<ConnectionMessageEventArgs>? MessageReceived;

    /// <summary>
    /// Invoked whenever an exception is thrown during a read or write operation on the named pipe.
    /// </summary>
    event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

    #endregion

    #region Methods

    /// <summary>
    /// Sends a message over a named pipe.
    /// </summary>
    /// <param name="value">Message to send.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidOperationException"></exception>
    Task WriteAsync(byte[] value, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Base interface for typed pipe connections.
/// </summary>
/// <typeparam name="T">Reference type to read/write from the named pipe.</typeparam>
public interface IPipeConnection<T> : IAsyncDisposable
{
    #region Properties

    /// <summary>
    /// Used formatter.
    /// </summary>
    IFormatter Formatter { get; }

    #endregion

    #region Events

    /// <summary>
    /// Invoked whenever a message is received.
    /// </summary>
    event EventHandler<ConnectionMessageEventArgs<T?>>? MessageReceived;

    /// <summary>
    /// Invoked whenever an exception is thrown during a read or write operation on the named pipe.
    /// </summary>
    event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

    #endregion

    #region Methods

    /// <summary>
    /// Sends a message over a named pipe.
    /// </summary>
    /// <param name="value">Message to send.</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidOperationException"></exception>
    Task WriteAsync(T value, CancellationToken cancellationToken = default);

    #endregion
}
