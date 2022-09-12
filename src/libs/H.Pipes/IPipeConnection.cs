using System.IO.Pipes;
using H.Formatters;
using H.Pipes.Args;

namespace H.Pipes;

/// <summary>
/// Represents a connection between a named pipe client and server.
/// </summary>
public interface IPipeConnection
{
    #region Properties

    /// <summary>Used formatter</summary>
    IFormatter Formatter { get; set; }

    /// <summary>Gets a value indicating whether the pipe is connected or not.</summary>
    bool IsConnected { get; }

    /// <summary><see langword="true" /> if started and not disposed.</summary>
    bool IsStarted { get; }

    /// <summary>Gets the connection's pipe name.</summary>
    string PipeName { get; }

    /// <summary>
    ///     Raw pipe stream. You can cast it to <see cref="NamedPipeClientStream" /> or
    ///     <see cref="NamedPipeServerStream" />.
    /// </summary>
    PipeStream PipeStream { get; }

    /// <summary>Gets the connection's server name. Only for client connections.</summary>
    string ServerName { get; }

    #endregion

    #region Events

    /// <summary>Invoked when the named pipe connection terminates.</summary>
    event EventHandler<ConnectionEventArgs>? Disconnected;

    /// <summary>Invoked whenever a message is received from the other end of the pipe.</summary>
    event EventHandler<ConnectionExceptionEventArgs>? ExceptionOccurred;

    /// <summary>
    ///     Invoked when an exception is thrown during any read/write operation over the named
    ///     pipe.
    /// </summary>
    event EventHandler<ConnectionMessageEventArgs<byte[]?>>? MessageReceived;

    #endregion

    #region Methods

    /// <summary>
    ///     Begins reading from and writing to the named pipe on a background thread. This method
    ///     returns immediately.
    /// </summary>
    void Start();

    /// <summary>Dispose internal resources</summary>
    Task StopAsync();

    /// <summary>Writes the specified <paramref name="value" /> and waits other end reading</summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    Task WriteAsync(byte[] value, CancellationToken cancellationToken = default);

    /// <summary>Writes the specified <paramref name="value" /> and waits other end reading</summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    Task WriteAsync<T>(T value, CancellationToken cancellationToken = default);

    /// <summary>Gets the user name of the client on the other end of the pipe.</summary>
    /// <returns>The user name of the client on the other end of the pipe.</returns>
    /// <exception cref="InvalidOperationException">
    ///     <see cref="PipeStream" /> is not
    ///     <see cref="NamedPipeServerStream" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">No pipe connections have been made yet.</exception>
    /// <exception cref="InvalidOperationException">The connected pipe has already disconnected.</exception>
    /// <exception cref="InvalidOperationException">The pipe handle has not been set.</exception>
    /// <exception cref="ObjectDisposedException">The pipe is closed.</exception>
    /// <exception cref="IOException">The pipe connection has been broken.</exception>
    /// <exception cref="IOException">The user name of the client is longer than 19 characters.</exception>
    string GetImpersonationUserName();

    #endregion
}
