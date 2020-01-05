using System;
using System.IO.Pipes;
using System.Threading;
using NamedPipeWrapper.Args;
using NamedPipeWrapper.Factories;
using NamedPipeWrapper.Threading;

namespace NamedPipeWrapper
{
    /// <summary>
    /// Wraps a <see cref="NamedPipeClientStream"/>.
    /// </summary>
    /// <typeparam name="TReadWrite">Reference type to read from and write to the named pipe</typeparam>
    public sealed class NamedPipeClient<TReadWrite> : NamedPipeClient<TReadWrite, TReadWrite> where TReadWrite : class
    {
        /// <summary>
        /// Constructs a new <c>NamedPipeClient</c> to connect to the <see cref="NamedPipeServer{TReadWrite}"/> specified by <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the server's pipe</param>
        /// <param name="serverName">server name default is local.</param>
        public NamedPipeClient(string pipeName, string serverName = ".") : base(pipeName, serverName)
        {
        }
    }

    /// <summary>
    /// Wraps a <see cref="NamedPipeClientStream"/>.
    /// </summary>
    /// <typeparam name="TRead">Reference type to read from the named pipe</typeparam>
    /// <typeparam name="TWrite">Reference type to write to the named pipe</typeparam>
    public class NamedPipeClient<TRead, TWrite> : IDisposable
        where TRead : class
        where TWrite : class
    {
        /// <summary>
        /// Gets or sets whether the client should attempt to reconnect when the pipe breaks
        /// due to an error or the other end terminating the connection. <br/>
        /// Default value is <see langword="true"/>.
        /// </summary>
        public bool AutoReconnect { get; set; }

        private string PipeName { get; }
        private string ServerName { get; }

        private NamedPipeConnection<TRead, TWrite>? Connection { get; set; }

        private AutoResetEvent ConnectedEvent { get; } = new AutoResetEvent(false);
        private AutoResetEvent DisconnectedEvent { get; } = new AutoResetEvent(false);

        private Worker? ListenWorker { get; set; }

        private bool IsDisposed { get; set; }

        #region Events

        /// <summary>
        /// Invoked whenever a message is received from the server.
        /// </summary>
        public event EventHandler<ConnectionMessageEventArgs<TRead, TWrite>>? MessageReceived;

        /// <summary>
        /// Invoked when the client disconnects from the server (e.g., the pipe is closed or broken).
        /// </summary>
        public event EventHandler<ConnectionEventArgs<TRead, TWrite>>? Disconnected;

        /// <summary>
        /// Invoked whenever an exception is thrown during a read or write operation on the named pipe.
        /// </summary>
        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        private void OnMessageReceived(ConnectionMessageEventArgs<TRead, TWrite> args)
        {
            MessageReceived?.Invoke(this, args);
        }

        private void OnDisconnected(ConnectionEventArgs<TRead, TWrite> args)
        {
            Disconnected?.Invoke(this, args);
        }

        private void OnExceptionOccurred(Exception exception)
        {
            ExceptionOccurred?.Invoke(this, new ExceptionEventArgs(exception));
        }

        #endregion



        /// <summary>
        /// Constructs a new <c>NamedPipeClient</c> to connect to the <see cref="NamedPipeServer{TRead, TWrite}"/> specified by <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the server's pipe</param>
        /// <param name="serverName">the Name of the server, default is  local machine</param>
        public NamedPipeClient(string pipeName, string serverName = ".")
        {
            PipeName = pipeName;
            ServerName = serverName;
            AutoReconnect = true;
        }

        /// <summary>
        /// Connects to the named pipe server asynchronously.
        /// This method returns immediately, possibly before the connection has been established.
        /// </summary>
        public void Start()
        {
            ListenWorker = new Worker(() =>
            {
                // Get the name of the data pipe that should be used from now on by this NamedPipeClient
                var handshake = PipeClientFactory.Connect<string, string>(PipeName, ServerName);
                var dataPipeName = handshake.ReadObject();
                handshake.Dispose();

                if (dataPipeName == null)
                {
                    throw new InvalidOperationException("dataPipeName is null");
                }

                // Connect to the actual data pipe
                var dataPipe = PipeClientFactory.CreateAndConnectPipe(dataPipeName, ServerName);

                // Create a Connection object for the data pipe
                Connection = ConnectionFactory.CreateConnection<TRead, TWrite>(dataPipe);
                Connection.Disconnected += ConnectionOnDisconnected;
                Connection.MessageReceived += (sender, args) => OnMessageReceived(args);
                Connection.ExceptionOccurred += (sender, args) => OnExceptionOccurred(args.Exception);
                Connection.Open();

                ConnectedEvent.Set();
            }, OnExceptionOccurred);
        }

        /// <summary>
        ///     Sends a message to the server over a named pipe.
        /// </summary>
        /// <param name="message">Message to send to the server.</param>
        public void PushMessage(TWrite message)
        {
            Connection?.PushMessage(message);
        }

        #region Wait for connection/disconnection

        /// <summary>
        /// 
        /// </summary>
        public void WaitForConnection()
        {
            ConnectedEvent.WaitOne();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        public void WaitForConnection(int millisecondsTimeout)
        {
            ConnectedEvent.WaitOne(millisecondsTimeout);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        public void WaitForConnection(TimeSpan timeout)
        {
            ConnectedEvent.WaitOne(timeout);
        }

        /// <summary>
        /// 
        /// </summary>
        public void WaitForDisconnection()
        {
            DisconnectedEvent.WaitOne();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        public void WaitForDisconnection(int millisecondsTimeout)
        {
            DisconnectedEvent.WaitOne(millisecondsTimeout);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        public void WaitForDisconnection(TimeSpan timeout)
        {
            DisconnectedEvent.WaitOne(timeout);
        }

        #endregion

        #region Private methods

        private void ConnectionOnDisconnected(object sender, ConnectionEventArgs<TRead, TWrite> args)
        {
            OnDisconnected(args);

            DisconnectedEvent.Set();

            // Reconnect
            if (AutoReconnect && !IsDisposed)
            {
                Start();
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose internal resources
        /// </summary>
        public void Dispose()
        {
            IsDisposed = true;

            ListenWorker?.Dispose();

            Connection?.Dispose();
            ConnectedEvent.Dispose();
            DisconnectedEvent.Dispose();
        }

        #endregion
    }
}
