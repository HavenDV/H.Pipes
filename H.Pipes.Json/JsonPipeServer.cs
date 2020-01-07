﻿using H.Pipes.Formatters;
using NamedPipeWrapper;

namespace H.Pipes
{
    /// <summary>
    /// Wraps a <see cref="NamedPipeServer{T}"/> and provides multiple simultaneous client connection handling.
    /// </summary>
    /// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
    public class JsonPipeServer<T> : NamedPipeServer<T>
        where T : class
    {
        #region Constructors

        /// <summary>
        /// Constructs a new <c>NamedPipeServer</c> object that listens for client connections on the given <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the pipe to listen on</param>
        public JsonPipeServer(string pipeName) : base(pipeName, new JsonFormatter())
        {
        }

        #endregion
    }
}