using System;
using H.Pipes.Formatters;
using NamedPipeWrapper;

namespace H.Pipes
{
    /// <summary>
    /// Wraps a <see cref="NamedPipeClient{T}"/>.
    /// </summary>
    /// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
    public class WirePipeClient<T> : NamedPipeClient<T>
        where T : class
    {
        #region Constructors

        /// <summary>
        /// Constructs a new <see cref="WirePipeClient{T}"/> to connect to the <see cref="WirePipeClient{T}"/> specified by <paramref name="pipeName"/>. <br/>
        /// Default reconnection interval - <see langword="100 ms"/>
        /// </summary>
        /// <param name="pipeName">Name of the server's pipe</param>
        /// <param name="serverName">the Name of the server, default is  local machine</param>
        /// <param name="reconnectionInterval">Default reconnection interval - <see langword="100 ms"/></param>
        public WirePipeClient(string pipeName, string serverName = ".", TimeSpan? reconnectionInterval = default) : base(pipeName, serverName, reconnectionInterval, new WireFormatter())
        {
        }

        #endregion
    }
}
