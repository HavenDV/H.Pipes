using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace H.Pipes.IO
{
    /// <summary>
    /// Wraps a <see cref="PipeStream"/> object to read and write .NET CLR objects.
    /// </summary>
    public sealed class PipeStreamWrapper : IDisposable, IAsyncDisposable
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
        private PipeStreamReader Reader { get; }
        private PipeStreamWriter Writer { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new <c>PipeStreamWrapper</c> object that reads from and writes to the given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Stream to read from and write to</param>
        /// <exception cref="ArgumentNullException"></exception>
        public PipeStreamWrapper(PipeStream stream)
        {
            BaseStream = stream ?? throw new ArgumentNullException(nameof(stream));

            Reader = new PipeStreamReader(BaseStream);
            Writer = new PipeStreamWriter(BaseStream);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Reads the next object from the pipe. 
        /// </summary>
        /// <returns>The next object read from the pipe, or <c>null</c> if the pipe disconnected.</returns>
        public async Task<byte[]?> ReadAsync(CancellationToken cancellationToken = default)
        {
            return await Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes an object to the pipe.  This method blocks until all data is sent.
        /// </summary>
        /// <param name="buffer">Object to write to the pipe</param>
        /// <param name="cancellationToken"></param>
        public async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
        {
            await Writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

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

        /// <summary>
        /// Dispose internal <see cref="PipeStream"/>
        /// </summary>
        public async ValueTask DisposeAsync()
        {
#if NETSTANDARD2_0
            BaseStream.Dispose();
#else
            await BaseStream.DisposeAsync().ConfigureAwait(false);
#endif

            // This is redundant, just to avoid mistakes and follow the general logic of Dispose
            await Reader.DisposeAsync().ConfigureAwait(false);
            await Writer.DisposeAsync().ConfigureAwait(false);
        }

        #endregion
    }
}
