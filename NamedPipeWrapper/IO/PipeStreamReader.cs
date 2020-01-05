using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NamedPipeWrapper.IO
{
    /// <summary>
    /// Wraps a <see cref="PipeStream"/> object and reads from it.  Deserializes binary data sent by a <see cref="PipeStreamWriter{T}"/>
    /// into a .NET CLR object specified by <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Reference type to deserialize data to</typeparam>
    public sealed class PipeStreamReader<T> : IDisposable where T : class
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether the pipe is connected or not.
        /// </summary>
        public bool IsConnected { get; private set; }

        private PipeStream BaseStream { get; }
        private BinaryFormatter BinaryFormatter { get; } = new BinaryFormatter();

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
        private int ReadLength()
        {
            var buffer = new byte[sizeof(int)];
            var bytesRead = BaseStream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                IsConnected = false;
                return 0;
            }

            if (bytesRead != buffer.Length)
            {
                throw new IOException($"Expected {buffer.Length} bytes but read {bytesRead}");
            }

            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
        }

        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="T"/> is not marked as serializable.</exception>
        private T ReadObject(int length)
        {
            var data = new byte[length];
            BaseStream.Read(data, 0, length);

            using var memoryStream = new MemoryStream(data);

            return (T)BinaryFormatter.Deserialize(memoryStream);
        }

        /// <summary>
        /// Reads the next object from the pipe.  This method blocks until an object is sent
        /// or the pipe is disconnected.
        /// </summary>
        /// <returns>The next object read from the pipe, or <c>null</c> if the pipe disconnected.</returns>
        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="T"/> is not marked as serializable.</exception>
        public T? ReadObject()
        {
            var length = ReadLength();

            return length == 0
                ? default
                : ReadObject(length);
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
