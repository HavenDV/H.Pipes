using System;

namespace H.Pipes.Args
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
        public PipeConnection<T> Connection { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public ConnectionEventArgs(PipeConnection<T> connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
    }
}
