using System;

namespace NamedPipeWrapper.Args
{
    /// <summary>
    /// Handles messages received from a named pipe.
    /// </summary>
    /// <typeparam name="TRead">Reference type</typeparam>
    /// <typeparam name="TWrite">Reference type</typeparam>
    public class ConnectionMessageEventArgs<TRead, TWrite> : ConnectionEventArgs<TRead, TWrite>
        where TRead : class
        where TWrite : class
    {
        /// <summary>
        /// Message sent by the other end of the pipe
        /// </summary>
        public TRead Message { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        public ConnectionMessageEventArgs(NamedPipeConnection<TRead, TWrite> connection, TRead message) : base(connection)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}
