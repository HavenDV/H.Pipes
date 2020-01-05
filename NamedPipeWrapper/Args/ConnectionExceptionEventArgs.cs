using System;

namespace NamedPipeWrapper.Args
{
    /// <summary>
    /// Handles exceptions thrown during read/write operations.
    /// </summary>
    /// <typeparam name="TRead">Reference type</typeparam>
    /// <typeparam name="TWrite">Reference type</typeparam>
    public class ConnectionExceptionEventArgs<TRead, TWrite> : ConnectionEventArgs<TRead, TWrite>
        where TRead : class
        where TWrite : class
    {
        /// <summary>
        /// The exception that was thrown
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="exception"></param>
        public ConnectionExceptionEventArgs(NamedPipeConnection<TRead, TWrite> connection, Exception exception) : base(connection)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }
}
