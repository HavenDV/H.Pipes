using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes.Args;

namespace H.Pipes
{
    /// <summary>
    /// Wraps a <see cref="NamedPipeClientStream"/>.
    /// </summary>
    /// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
    public interface IPipeClient<T> : IDisposable, IAsyncDisposable
    {
        #region Properties

        /// <summary>
        /// Gets or sets whether the client should attempt to reconnect when the pipe breaks
        /// due to an error or the other end terminating the connection. <br/>
        /// Default value is <see langword="true"/>.
        /// </summary>
        bool AutoReconnect { get; set; }

        /// <summary>
        /// Interval of reconnection
        /// </summary>
        TimeSpan ReconnectionInterval { get; }

        /// <summary>
        /// Checks that connection is exists
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// <see langword="true"/> if <see cref="ConnectAsync"/> in process
        /// </summary>
        bool IsConnecting { get; }

        #endregion

        #region Events

        /// <summary>
        /// Invoked whenever a message is received from the server.
        /// </summary>
        event EventHandler<ConnectionMessageEventArgs<T>>? MessageReceived;

        /// <summary>
        /// Invoked when the client disconnects from the server (e.g., the pipe is closed or broken).
        /// </summary>
        event EventHandler<ConnectionEventArgs<T>>? Disconnected;

        /// <summary>
        /// Invoked after each the client connect to the server (include reconnects).
        /// </summary>
        event EventHandler<ConnectionEventArgs<T>>? Connected;

        /// <summary>
        /// Invoked whenever an exception is thrown during a read or write operation on the named pipe.
        /// </summary>
        event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

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

        /// <summary>
        /// Sends a message to the server over a named pipe. <br/>
        /// If client is not connected, <see cref="InvalidOperationException"/> is occurred
        /// </summary>
        /// <param name="value">Message to send to the server.</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="InvalidOperationException"></exception>
        Task WriteAsync(T value, CancellationToken cancellationToken = default);

        #endregion
    }
}
