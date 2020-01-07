using System;

namespace H.Pipes.Args
{
    /// <summary>
    /// Handles exceptions thrown during read/write operations.
    /// </summary>
    /// <typeparam name="T">Reference type</typeparam>
    public class ConnectionExceptionEventArgs<T> : ConnectionEventArgs<T> 
        where T : class
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
        public ConnectionExceptionEventArgs(PipeConnection<T> connection, Exception exception) : base(connection)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }
}
