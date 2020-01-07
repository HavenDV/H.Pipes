using System;
using System.IO.Pipes;
using NamedPipeWrapper.IO;
using System.Threading;
using System.Threading.Tasks;
using NamedPipeWrapper.Args;
using NamedPipeWrapper.Utilities;

namespace NamedPipeWrapper
{
    /// <summary>
    /// Represents a connection between a named pipe client and server.
    /// </summary>
    /// <typeparam name="TRead">Reference type to read from the named pipe</typeparam>
    /// <typeparam name="TWrite">Reference type to write to the named pipe</typeparam>
    public sealed class NamedPipeConnection<TRead, TWrite> : IAsyncDisposable
        where TRead : class
        where TWrite : class
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

        public bool IsStarted => ReadWorker != null;

        private PipeStreamWrapper<TRead, TWrite> PipeStreamWrapper { get; }
        private Worker? ReadWorker { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Invoked when the named pipe connection terminates.
        /// </summary>
        public event EventHandler<ConnectionEventArgs<TRead, TWrite>>? Disconnected;

        /// <summary>
        /// Invoked whenever a message is received from the other end of the pipe.
        /// </summary>
        public event EventHandler<ConnectionMessageEventArgs<TRead, TWrite>>? MessageReceived;

        /// <summary>
        /// Invoked when an exception is thrown during any read/write operation over the named pipe.
        /// </summary>
        public event EventHandler<ConnectionExceptionEventArgs<TRead, TWrite>>? ExceptionOccurred;

        private void OnDisconnected()
        {
            Disconnected?.Invoke(this, new ConnectionEventArgs<TRead, TWrite>(this));
        }

        private void OnMessageReceived(TRead message)
        {
            MessageReceived?.Invoke(this, new ConnectionMessageEventArgs<TRead, TWrite>(this, message));
        }

        private void OnExceptionOccurred(Exception exception)
        {
            ExceptionOccurred?.Invoke(this, new ConnectionExceptionEventArgs<TRead, TWrite>(this, exception));
        }

        #endregion

        #region Constructors

        internal NamedPipeConnection(int id, string name, PipeStream stream)
        {
            Id = id;
            Name = name;
            PipeStreamWrapper = new PipeStreamWrapper<TRead, TWrite>(stream);
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

            ReadWorker = new Worker(async cancellationToken =>
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected && PipeStreamWrapper.CanRead)
                {
                    try
                    {
                        var obj = await PipeStreamWrapper.ReadObjectAsync(cancellationToken).ConfigureAwait(false);
                        if (obj == null)
                        {
                            return;
                        }

                        OnMessageReceived(obj);
                    }
                    catch (TaskCanceledException)
                    {
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
        public async Task<bool> WriteAsync(TWrite value, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || !PipeStreamWrapper.CanWrite)
            {
                return false;
            }

            try
            {
                await PipeStreamWrapper.WriteObjectAsync(value, cancellationToken).ConfigureAwait(false);

                return true;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

        #endregion

        #region IDisposable

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

            PipeStreamWrapper.Dispose();
        }

        #endregion
    }
}
