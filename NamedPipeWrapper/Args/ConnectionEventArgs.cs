using System;

namespace NamedPipeWrapper.Args
{
    /// <summary>
    /// Handles new connections.
    /// </summary>
    /// <typeparam name="TRead">Reference type</typeparam>
    /// <typeparam name="TWrite">Reference type</typeparam>
    public class ConnectionEventArgs<TRead, TWrite> : EventArgs
        where TRead : class
        where TWrite : class
    {
        /// <summary>
        /// Connection
        /// </summary>
        public NamedPipeConnection<TRead, TWrite> Connection { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public ConnectionEventArgs(NamedPipeConnection<TRead, TWrite> connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
    }
}
