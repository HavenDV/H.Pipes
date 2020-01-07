using System;

namespace NamedPipeWrapper.Args
{
    /// <summary>
    /// Handles new connections.
    /// </summary>
    /// <typeparam name="T">Reference type</typeparam>
    public class ConnectionEventArgs<T> : EventArgs 
        where T : class
    {
        /// <summary>
        /// Connection
        /// </summary>
        public NamedPipeConnection<T> Connection { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public ConnectionEventArgs(NamedPipeConnection<T> connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
    }
}
