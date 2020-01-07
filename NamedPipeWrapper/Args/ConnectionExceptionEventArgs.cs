using System;

namespace NamedPipeWrapper.Args
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
        public ConnectionExceptionEventArgs(NamedPipeConnection<T> connection, Exception exception) : base(connection)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }
}
