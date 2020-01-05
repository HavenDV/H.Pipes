using System;
using System.IO.Pipes;
using NamedPipeWrapper.IO;
using NamedPipeWrapper.Threading;
using System.Collections.Concurrent;
using NamedPipeWrapper.Args;

namespace NamedPipeWrapper
{
    /// <summary>
    /// Represents a connection between a named pipe client and server.
    /// </summary>
    /// <typeparam name="TRead">Reference type to read from the named pipe</typeparam>
    /// <typeparam name="TWrite">Reference type to write to the named pipe</typeparam>
    public sealed class NamedPipeConnection<TRead, TWrite> : IDisposable
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

        private PipeStreamWrapper<TRead, TWrite> PipeStreamWrapper { get; }

        /// <summary>
        /// To support Multithread, we should use BlockingCollection.
        /// </summary>
        private BlockingCollection<TWrite> WriteQueue { get; } = new BlockingCollection<TWrite>();

        private bool NotifiedSucceeded { get; set; }

        private Worker? ReadWorker { get; set; }
        private Worker? WriteWorker { get; set; }

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

        internal NamedPipeConnection(int id, string name, PipeStream serverStream)
        {
            Id = id;
            Name = name;
            PipeStreamWrapper = new PipeStreamWrapper<TRead, TWrite>(serverStream);
        }

        #endregion

        /// <summary>
        /// Begins reading from and writing to the named pipe on a background thread.
        /// This method returns immediately.
        /// </summary>
        public void Open()
        {
            ReadWorker = new Worker(() =>
            {
                while (IsConnected && PipeStreamWrapper.CanRead)
                {
                    try
                    {
                        var obj = PipeStreamWrapper.ReadObject();
                        if (obj == null)
                        {
                            return;
                        }

                        OnMessageReceived(obj);
                    }
                    catch (Exception exception)
                    {
                        OnExceptionOccurred(exception);
                    }
                }

                OnSucceeded();
            }, OnExceptionOccurred);

            WriteWorker = new Worker(() =>
            {
                while (IsConnected && PipeStreamWrapper.CanWrite)
                {
                    try
                    {
                        PipeStreamWrapper.WriteObject(WriteQueue.Take());
                        PipeStreamWrapper.WaitForPipeDrain();
                    }
                    catch (Exception exception)
                    {
                        OnExceptionOccurred(exception);
                    }
                }

                OnSucceeded();
            }, OnExceptionOccurred);
        }

        /// <summary>
        /// Adds the specified <paramref name="message"/> to the write queue.
        /// The message will be written to the named pipe by the background thread
        /// at the next available opportunity.
        /// </summary>
        /// <param name="message"></param>
        public void PushMessage(TWrite message)
        {
            WriteQueue.Add(message);
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        private void OnSucceeded()
        {
            // Only notify observers once
            if (NotifiedSucceeded)
                return;

            NotifiedSucceeded = true;

            OnDisconnected();
        }

        #region IDisposable

        /// <summary>
        /// Dispose internal resources
        /// </summary>
        public void Dispose()
        {
            WriteWorker?.Dispose();
            ReadWorker?.Dispose();

            PipeStreamWrapper.Dispose();
            WriteQueue.Dispose();
        }

        #endregion
    }
}
