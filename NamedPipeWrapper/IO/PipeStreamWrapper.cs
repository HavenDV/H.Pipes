using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization;

namespace NamedPipeWrapper.IO
{
    /// <summary>
    /// Wraps a <see cref="PipeStream"/> object to read and write .NET CLR objects.
    /// </summary>
    /// <typeparam name="TRead">Reference type to <b>read</b> from the pipe</typeparam>
    /// <typeparam name="TWrite">Reference type to <b>write</b> to the pipe</typeparam>
    public sealed class PipeStreamWrapper<TRead, TWrite> : IDisposable
        where TRead : class
        where TWrite : class
    {
        #region Properties

        /// <summary>
        ///     Gets a value indicating whether the <see cref="BaseStream"/> object is connected or not.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the <see cref="BaseStream"/> object is connected; otherwise, <c>false</c>.
        /// </returns>
        public bool IsConnected => BaseStream.IsConnected && Reader.IsConnected;

        /// <summary>
        ///     Gets a value indicating whether the current stream supports read operations.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the stream supports read operations; otherwise, <c>false</c>.
        /// </returns>
        public bool CanRead => BaseStream.CanRead;

        /// <summary>
        ///     Gets a value indicating whether the current stream supports write operations.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the stream supports write operations; otherwise, <c>false</c>.
        /// </returns>
        public bool CanWrite => BaseStream.CanWrite;

        private PipeStream BaseStream { get; }
        private PipeStreamReader<TRead> Reader { get; }
        private PipeStreamWriter<TWrite> Writer { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new <c>PipeStreamWrapper</c> object that reads from and writes to the given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Stream to read from and write to</param>
        public PipeStreamWrapper(PipeStream stream)
        {
            BaseStream = stream ?? throw new ArgumentNullException(nameof(stream));

            Reader = new PipeStreamReader<TRead>(BaseStream);
            Writer = new PipeStreamWriter<TWrite>(BaseStream);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Reads the next object from the pipe.  This method blocks until an object is sent
        /// or the pipe is disconnected.
        /// </summary>
        /// <returns>The next object read from the pipe, or <c>null</c> if the pipe disconnected.</returns>
        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="TRead"/> is not marked as serializable.</exception>
        public TRead? ReadObject()
        {
            return Reader.ReadObject();
        }

        /// <summary>
        /// Writes an object to the pipe.  This method blocks until all data is sent.
        /// </summary>
        /// <param name="obj">Object to write to the pipe</param>
        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="TRead"/> is not marked as serializable.</exception>
        public void WriteObject(TWrite obj)
        {
            Writer.WriteObject(obj);
        }

        /// <summary>
        ///     Waits for the other end of the pipe to read all sent bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The pipe is closed.</exception>
        /// <exception cref="NotSupportedException">The pipe does not support write operations.</exception>
        /// <exception cref="IOException">The pipe is broken or another I/O error occurred.</exception>
        public void WaitForPipeDrain()
        {
            Writer.WaitForPipeDrain();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose internal <see cref="PipeStream"/>
        /// </summary>
        public void Dispose()
        {
            BaseStream.Dispose();

            // This is redundant, just to avoid mistakes and follow the general logic of Dispose
            Reader.Dispose();
            Writer.Dispose();
        }

        #endregion
    }
}
