using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace NamedPipeWrapper.IO
{
    /// <summary>
    /// Wraps a <see cref="PipeStream"/> object and writes to it.  Serializes .NET CLR objects specified by <typeparamref name="T"/>
    /// into binary form and sends them over the named pipe for a <see cref="PipeStreamWriter{T}"/> to read and deserialize.
    /// </summary>
    /// <typeparam name="T">Reference type to serialize</typeparam>
    public sealed class PipeStreamWriter<T> : IDisposable where T : class
    {
        #region Properties

        /// <summary>
        /// Gets the underlying <c>PipeStream</c> object.
        /// </summary>
        private PipeStream BaseStream { get; }

        private BinaryFormatter BinaryFormatter { get; } = new BinaryFormatter();

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new <c>PipeStreamWriter</c> object that writes to given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Pipe to write to</param>
        public PipeStreamWriter(PipeStream stream)
        {
            BaseStream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        #endregion

        #region Private stream writers

        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="T"/> is not marked as serializable.</exception>
        private byte[] Serialize(T obj)
        {
            using var stream = new MemoryStream();
            BinaryFormatter.Serialize(stream, obj);

            return stream.ToArray();
        }

        private async Task WriteLengthAsync(int length, CancellationToken cancellationToken = default)
        {
            var buffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(length));

            await WriteObjectAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteObjectAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            await BaseStream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes an object to the pipe.  This method blocks until all data is sent.
        /// </summary>
        /// <param name="obj">Object to write to the pipe</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="T"/> is not marked as serializable.</exception>
        public async Task WriteObjectAsync(T obj, CancellationToken cancellationToken = default)
        {
            var data = Serialize(obj);

            await WriteLengthAsync(data.Length, cancellationToken).ConfigureAwait(false);
            await WriteObjectAsync(data, cancellationToken).ConfigureAwait(false);

            await BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Waits for the other end of the pipe to read all sent bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The pipe is closed.</exception>
        /// <exception cref="NotSupportedException">The pipe does not support write operations.</exception>
        /// <exception cref="IOException">The pipe is broken or another I/O error occurred.</exception>
        public void WaitForPipeDrain()
        {
            BaseStream.WaitForPipeDrain();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose internal <see cref="PipeStream"/>
        /// </summary>
        public void Dispose()
        {
            BaseStream.Dispose();
        }

        #endregion
    }
}