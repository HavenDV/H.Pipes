using System;
using System.IO.Pipes;
using System.Runtime.Serialization;
using NamedPipeWrapper.IO;
using NamedPipeWrapper.Threading;
using System.Collections.Concurrent;

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

        #endregion

        #region Events

        /// <summary>
        /// Invoked when the named pipe connection terminates.
        /// </summary>
        public event ConnectionEventHandler<TRead, TWrite>? Disconnected;

        /// <summary>
        /// Invoked whenever a message is received from the other end of the pipe.
        /// </summary>
        public event ConnectionMessageEventHandler<TRead, TWrite>? ReceiveMessage;

        /// <summary>
        /// Invoked when an exception is thrown during any read/write operation over the named pipe.
        /// </summary>
        public event ConnectionExceptionEventHandler<TRead, TWrite>? Error;

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
            var readWorker = new Worker();
            readWorker.Succeeded += OnSucceeded;
            readWorker.Error += (sender, args) => OnError(args.Exception);
            readWorker.DoWork(ReadPipe);

            var writeWorker = new Worker();
            writeWorker.Succeeded += OnSucceeded;
            writeWorker.Error += (sender, args) => OnError(args.Exception);
            writeWorker.DoWork(WritePipe);
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
        private void OnSucceeded(object o, EventArgs args)
        {
            // Only notify observers once
            if (NotifiedSucceeded)
                return;

            NotifiedSucceeded = true;

            Disconnected?.Invoke(this);
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        /// <param name="exception"></param>
        private void OnError(Exception exception)
        {
            Error?.Invoke(this, exception);
        }

        /// <summary>
        ///     Invoked on the background thread.
        /// </summary>
        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="TRead"/> is not marked as serializable.</exception>
        private void ReadPipe()
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

                    ReceiveMessage?.Invoke(this, obj);
                }
                catch
                {
                    //we must igonre exception, otherwise, the namepipe wrapper will stop work.
                }
            }

        }

        /// <summary>
        ///     Invoked on the background thread.
        /// </summary>
        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="TWrite"/> is not marked as serializable.</exception>
        private void WritePipe()
        {

            while (IsConnected && PipeStreamWrapper.CanWrite)
            {
                try
                {
                    //using blockcollection, we needn't use singal to wait for result.
                    //_writeSignal.WaitOne();
                    //while (_writeQueue.Count > 0)
                    {
                        PipeStreamWrapper.WriteObject(WriteQueue.Take());
                        PipeStreamWrapper.WaitForPipeDrain();
                    }
                }
                catch
                {
                    //we must igonre exception, otherwise, the namepipe wrapper will stop work.
                }
            }

        }

        #region IDisposable

        /// <summary>
        /// Dispose internal resources
        /// </summary>
        public void Dispose()
        {
            PipeStreamWrapper.Dispose();
            WriteQueue.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Handles new connections.
    /// </summary>
    /// <param name="connection">The newly established connection</param>
    /// <typeparam name="TRead">Reference type</typeparam>
    /// <typeparam name="TWrite">Reference type</typeparam>
    public delegate void ConnectionEventHandler<TRead, TWrite>(NamedPipeConnection<TRead, TWrite> connection)
        where TRead : class
        where TWrite : class;

    /// <summary>
    /// Handles messages received from a named pipe.
    /// </summary>
    /// <typeparam name="TRead">Reference type</typeparam>
    /// <typeparam name="TWrite">Reference type</typeparam>
    /// <param name="connection">Connection that received the message</param>
    /// <param name="message">Message sent by the other end of the pipe</param>
    public delegate void ConnectionMessageEventHandler<TRead, TWrite>(NamedPipeConnection<TRead, TWrite> connection, TRead message)
        where TRead : class
        where TWrite : class;

    /// <summary>
    /// Handles exceptions thrown during read/write operations.
    /// </summary>
    /// <typeparam name="TRead">Reference type</typeparam>
    /// <typeparam name="TWrite">Reference type</typeparam>
    /// <param name="connection">Connection that threw the exception</param>
    /// <param name="exception">The exception that was thrown</param>
    public delegate void ConnectionExceptionEventHandler<TRead, TWrite>(NamedPipeConnection<TRead, TWrite> connection, Exception exception)
        where TRead : class
        where TWrite : class;
}
