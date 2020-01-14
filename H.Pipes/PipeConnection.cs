using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using H.Formatters;
using H.Pipes.IO;
using H.Pipes.Args;
using H.Pipes.Utilities;

namespace H.Pipes
{
    /// <summary>
    /// Represents a connection between a named pipe client and server.
    /// </summary>
    /// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
    public sealed class PipeConnection<T> : IDisposable, IAsyncDisposable
    {
        #region Properties

        /// <summary>
        /// Gets the connection's unique identifier.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the connection's name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the pipe is connected or not.
        /// </summary>
        public bool IsConnected => PipeStreamWrapper.IsConnected;

        /// <summary>
        /// <see langword="true"/> if started and not disposed
        /// </summary>
        public bool IsStarted => ReadWorker != null;

        private IFormatter Formatter { get; }
        private PipeStreamWrapper PipeStreamWrapper { get; }
        private TaskWorker? ReadWorker { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Invoked when the named pipe connection terminates.
        /// </summary>
        public event EventHandler<ConnectionEventArgs<T>>? Disconnected;

        /// <summary>
        /// Invoked whenever a message is received from the other end of the pipe.
        /// </summary>
        public event EventHandler<ConnectionMessageEventArgs<T>>? MessageReceived;

        /// <summary>
        /// Invoked when an exception is thrown during any read/write operation over the named pipe.
        /// </summary>
        public event EventHandler<ConnectionExceptionEventArgs<T>>? ExceptionOccurred;

        private void OnDisconnected()
        {
            Disconnected?.Invoke(this, new ConnectionEventArgs<T>(this));
        }

        private void OnMessageReceived(T message)
        {
            MessageReceived?.Invoke(this, new ConnectionMessageEventArgs<T>(this, message));
        }

        private void OnExceptionOccurred(Exception exception)
        {
            ExceptionOccurred?.Invoke(this, new ConnectionExceptionEventArgs<T>(this, exception));
        }

        #endregion

        #region Constructors

        internal PipeConnection(int id, string name, PipeStream stream, IFormatter formatter)
        {
            Id = id;
            Name = name;
            PipeStreamWrapper = new PipeStreamWrapper(stream);
            Formatter = formatter;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Begins reading from and writing to the named pipe on a background thread.
        /// This method returns immediately.
        /// </summary>
        public void Start()
        {
            if (IsStarted)
            {
                throw new InvalidOperationException("Connection already started");
            }

            ReadWorker = new TaskWorker(async cancellationToken =>
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected && PipeStreamWrapper.CanRead)
                {
                    try
                    {
                        var bytes = await PipeStreamWrapper.ReadAsync(cancellationToken).ConfigureAwait(false);
                        if (bytes == null)
                        {
                            break;
                        }

                        var obj = await Formatter.DeserializeAsync<T>(bytes, cancellationToken).ConfigureAwait(false);

                        OnMessageReceived(obj);
                    }
                    catch (OperationCanceledException)
                    {
                        OnDisconnected();
                        throw;
                    }
                    catch (Exception exception)
                    {
                        OnExceptionOccurred(exception);
                    }
                }

                OnDisconnected();
            }, OnExceptionOccurred);
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> and waits other end reading
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        public async Task WriteAsync(T value, CancellationToken cancellationToken = default)
        {
            value = value ?? throw new ArgumentNullException(nameof(value));

            if (!IsConnected || !PipeStreamWrapper.CanWrite)
            {
                throw new InvalidOperationException("Client is not connected");
            }

            var bytes = await Formatter.SerializeAsync(value, cancellationToken).ConfigureAwait(false);

            await PipeStreamWrapper.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose internal resources
        /// </summary>
        public void Dispose()
        {
            ReadWorker?.Dispose();
            ReadWorker = null;

            PipeStreamWrapper.Dispose();
        }

        /// <summary>
        /// Dispose internal resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (ReadWorker != null)
            {
                await ReadWorker.DisposeAsync().ConfigureAwait(false);

                ReadWorker = null;
            }

            await PipeStreamWrapper.DisposeAsync();
        }

        #endregion
    }
}
