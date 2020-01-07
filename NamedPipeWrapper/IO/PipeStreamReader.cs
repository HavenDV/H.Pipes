using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NamedPipeWrapper.IO
{
    /// <summary>
    /// Wraps a <see cref="PipeStream"/> object and reads from it.
    /// </summary>
    public sealed class PipeStreamReader : IDisposable
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether the pipe is connected or not.
        /// </summary>
        public bool IsConnected { get; private set; }

        private PipeStream BaseStream { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new <c>PipeStreamReader</c> object that reads data from the given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Pipe to read from</param>
        public PipeStreamReader(PipeStream stream)
        {
            BaseStream = stream ?? throw new ArgumentNullException(nameof(stream));
            IsConnected = stream.IsConnected;
        }

        #endregion

        #region Private stream readers

        /// <summary>
        /// Reads the length of the next message (in bytes) from the client.
        /// </summary>
        /// <returns>Number of bytes of data the client will be sending.</returns>
        /// <exception cref="InvalidOperationException">The pipe is disconnected, waiting to connect, or the handle has not been set.</exception>
        /// <exception cref="IOException">Any I/O error occurred.</exception>
        private async Task<int> ReadLengthAsync(CancellationToken cancellationToken = default)
        {
            var buffer = new byte[sizeof(int)];
            var read = await BaseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                IsConnected = false;
                return 0;
            }

            if (read != buffer.Length)
            {
                throw new IOException($"Expected {buffer.Length} bytes but read {read}");
            }

            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
        }

        private async Task<byte[]> ReadAsync(int length, CancellationToken cancellationToken = default)
        {
            var buffer = new byte[length];
            await BaseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

            return buffer;
        }

        /// <summary>
        /// Reads the next object from the pipe.  This method waits until an object is sent
        /// or the pipe is disconnected.
        /// </summary>
        /// <returns>The next object read from the pipe, or <c>null</c> if the pipe disconnected.</returns>
        public async Task<byte[]?> ReadAsync(CancellationToken cancellationToken = default)
        {
            var length = await ReadLengthAsync(cancellationToken).ConfigureAwait(false);

            return length == 0
                ? default
                : await ReadAsync(length, cancellationToken).ConfigureAwait(false);
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
