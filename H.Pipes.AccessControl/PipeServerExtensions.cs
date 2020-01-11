using System;
using System.IO.Pipes;

namespace H.Pipes.AccessControl
{
    /// <summary>
    /// Adds AccessControl extensions methods for <see cref="PipeServer{T}"/>
    /// </summary>
    public static class PipeServerExtensions
    {
        /// <summary>
        /// Sets <see cref="PipeSecurity"/>'s for each <see cref="NamedPipeServerStream"/> that will be created by <see cref="PipeServer{T}"/> <br/>
        /// Overrides <see cref="PipeServer{T}.CreatePipeStreamFunc"/>
        /// </summary>
        /// <param name="server"></param>
        /// <param name="pipeSecurity"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public static void SetPipeSecurity<T>(this PipeServer<T> server, PipeSecurity pipeSecurity)
        {
            server = server ?? throw new ArgumentNullException(nameof(server));
            pipeSecurity = pipeSecurity ?? throw new ArgumentNullException(nameof(pipeSecurity));

            server.CreatePipeStreamFunc = pipeName => NamedPipeServerStreamConstructors.New(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough, 0, 0, pipeSecurity);
        }
    }
}
