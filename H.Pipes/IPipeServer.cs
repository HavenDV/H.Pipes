using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using H.Formatters;
using H.Pipes.Args;

namespace H.Pipes
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
    public interface IPipeServer<T> : IDisposable, IAsyncDisposable
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
        /// Used formatter
        /// </summary>
        IFormatter Formatter { get; set; }

        /// <summary>
        /// IsStarted
        /// </summary>
        bool IsStarted { get; }

        #endregion

        #region Events

        /// <summary>
        /// Invoked whenever a client connects to the server.
        /// </summary>
        event EventHandler<ConnectionEventArgs<T>>? ClientConnected;

        /// <summary>
        /// Invoked whenever a client disconnects from the server.
        /// </summary>
        event EventHandler<ConnectionEventArgs<T>>? ClientDisconnected;

        /// <summary>
        /// Invoked whenever a client sends a message to the server.
        /// </summary>
        event EventHandler<ConnectionMessageEventArgs<T>>? MessageReceived;

        /// <summary>
        /// Invoked whenever an exception is thrown during a read or write operation.
        /// </summary>
        event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        #endregion

        #region Methods

        /// <summary>
        /// Begins listening for client connections in a separate background thread.
        /// This method waits when pipe will be created(or throws exception).
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="IOException"></exception>
        Task StartAsync(bool waitFreePipe = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message to all connected clients asynchronously.
        /// This method returns immediately, possibly before the message has been sent to all clients.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        Task WriteAsync(T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Closes all open client connections and stops listening for new ones.
        /// </summary>
        Task StopAsync(CancellationToken _ = default);

        #endregion
    }
}
