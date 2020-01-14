using System;
using System.IO.Pipes;
using H.Formatters;
using H.Pipes.Args;

namespace H.Pipes
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
    public interface IPipeServer<T>
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
    }
}
