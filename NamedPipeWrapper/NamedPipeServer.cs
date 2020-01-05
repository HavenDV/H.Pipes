using NamedPipeWrapper.IO;
using NamedPipeWrapper.Threading;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using NamedPipeWrapper.Args;
using NamedPipeWrapper.Factories;

namespace NamedPipeWrapper
{
    /// <summary>
    /// Wraps a <see cref="NamedPipeServerStream"/> and provides multiple simultaneous client connection handling.
    /// </summary>
    /// <typeparam name="TReadWrite">Reference type to read from and write to the named pipe</typeparam>
    public class NamedPipeServer<TReadWrite> : NamedPipeServer<TReadWrite, TReadWrite> where TReadWrite : class
    {
        /// <summary>
        /// Constructs a new <c>NamedPipeServer</c> object that listens for client connections on the given <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the pipe to listen on</param>
        public NamedPipeServer(string pipeName)
            : base(pipeName)
        {
        }
    }

    /// <summary>
    /// Wraps a <see cref="NamedPipeServerStream"/> and provides multiple simultaneous client connection handling.
    /// </summary>
    /// <typeparam name="TRead">Reference type to read from the named pipe</typeparam>
    /// <typeparam name="TWrite">Reference type to write to the named pipe</typeparam>
    public class NamedPipeServer<TRead, TWrite> : IDisposable
        where TRead : class
        where TWrite : class
    {
        #region Properties

        private string PipeName { get; }
        private List<NamedPipeConnection<TRead, TWrite>> Connections { get; } = new List<NamedPipeConnection<TRead, TWrite>>();

        private int NextPipeId { get; set; }

        private volatile bool _shouldKeepRunning;

        private Worker? ListenWorker { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Invoked whenever a client connects to the server.
        /// </summary>
        public event EventHandler<ConnectionEventArgs<TRead, TWrite>>? ClientConnected;

        /// <summary>
        /// Invoked whenever a client disconnects from the server.
        /// </summary>
        public event EventHandler<ConnectionEventArgs<TRead, TWrite>>? ClientDisconnected;

        /// <summary>
        /// Invoked whenever a client sends a message to the server.
        /// </summary>
        public event EventHandler<ConnectionMessageEventArgs<TRead, TWrite>>? MessageReceived;

        /// <summary>
        /// Invoked whenever an exception is thrown during a read or write operation.
        /// </summary>
        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        private void OnClientConnected(ConnectionEventArgs<TRead, TWrite> args)
        {
            ClientConnected?.Invoke(this, args);
        }

        private void OnClientDisconnected(ConnectionEventArgs<TRead, TWrite> args)
        {
            ClientDisconnected?.Invoke(this, args);
        }

        private void OnMessageReceived(ConnectionMessageEventArgs<TRead, TWrite> args)
        {
            MessageReceived?.Invoke(this, args);
        }

        private void OnExceptionOccurred(Exception exception)
        {
            ExceptionOccurred?.Invoke(this, new ExceptionEventArgs(exception));
        }

        #endregion

        /// <summary>
        /// Constructs a new <c>NamedPipeServer</c> object that listens for client connections on the given <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the pipe to listen on</param>
        public NamedPipeServer(string pipeName)
        {
            PipeName = pipeName;
        }

        /// <summary>
        /// Begins listening for client connections in a separate background thread.
        /// This method returns immediately.
        /// </summary>
        public void Start()
        {
            _shouldKeepRunning = true;

            ListenWorker = new Worker(() =>
            {
                while (_shouldKeepRunning)
                {
                    WaitForConnection(PipeName);
                }
            }, OnExceptionOccurred);
        }

        /// <summary>
        /// Sends a message to all connected clients asynchronously.
        /// This method returns immediately, possibly before the message has been sent to all clients.
        /// </summary>
        /// <param name="message"></param>
        public void PushMessage(TWrite message)
        {
            lock (Connections)
            {
                foreach (var connection in Connections)
                {
                    connection.PushMessage(message);
                }
            }
        }

        /// <summary>
        /// push message to the given client.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="clientName"></param>
        public void PushMessage(TWrite message, string clientName)
        {
            lock (Connections)
            {
                foreach (var connection in Connections.Where(connection => connection.Name == clientName))
                {
                    connection.PushMessage(message);
                }
            }
        }

        /// <summary>
        /// Closes all open client connections and stops listening for new ones.
        /// </summary>
        public void Stop()
        {
            _shouldKeepRunning = false;

            Dispose();

            // If background thread is still listening for a client to connect,
            // initiate a dummy connection that will allow the thread to exit.
            //dummy connection will use the local server name.
            var dummyClient = new NamedPipeClient<TRead, TWrite>(PipeName);
            dummyClient.Start();
            dummyClient.WaitForConnection(TimeSpan.FromSeconds(2));
            dummyClient.Dispose();
            dummyClient.WaitForDisconnection(TimeSpan.FromSeconds(2));
        }

        #region Private methods

        private void WaitForConnection(string pipeName)
        {
            NamedPipeServerStream? handshakePipe = null;
            NamedPipeServerStream? dataPipe = null;
            NamedPipeConnection<TRead, TWrite>? connection = null;

            var connectionPipeName = GetNextConnectionPipeName(pipeName);

            try
            {
                // Send the client the name of the data pipe to use
                handshakePipe = PipeServerFactory.CreateAndConnectPipe(pipeName);
                var handshakeWrapper = new PipeStreamWrapper<string, string>(handshakePipe);
                handshakeWrapper.WriteObject(connectionPipeName);
                handshakeWrapper.WaitForPipeDrain();
                handshakeWrapper.Dispose();

                // Wait for the client to connect to the data pipe
                dataPipe = PipeServerFactory.CreatePipe(connectionPipeName);
                dataPipe.WaitForConnection();

                // Add the client's connection to the list of connections
                connection = ConnectionFactory.CreateConnection<TRead, TWrite>(dataPipe);
                connection.MessageReceived += (sender, args) => OnMessageReceived(args);
                connection.Disconnected += ClientOnDisconnected;
                connection.ExceptionOccurred += (sender, args) => OnExceptionOccurred(args.Exception);
                connection.Open();

                lock (Connections)
                {
                    Connections.Add(connection);
                }

                OnClientConnected(new ConnectionEventArgs<TRead, TWrite>(connection));
            }
            // Catch the IOException that is raised if the pipe is broken or disconnected.
            catch (Exception e)
            {
                Console.Error.WriteLine("Named pipe is broken or disconnected: {0}", e);

                handshakePipe?.Dispose();
                dataPipe?.Dispose();

                if (connection != null)
                {
                    ClientOnDisconnected(this, new ConnectionEventArgs<TRead, TWrite>(connection));
                }
            }
        }

        private void ClientOnDisconnected(object sender, ConnectionEventArgs<TRead, TWrite> args)
        {
            if (args.Connection == null)
            {
                return;
            }

            lock (Connections)
            {
                Connections.Remove(args.Connection);
            }

            OnClientDisconnected(args);
        }

        private string GetNextConnectionPipeName(string pipeName)
        {
            return $"{pipeName}_{++NextPipeId}";
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose internal resources
        /// </summary>
        public void Dispose()
        {
            ListenWorker?.Dispose();

            lock (Connections)
            {
                foreach (var connection in Connections)
                {
                    connection.Dispose();
                }
                Connections.Clear();
            }
        }

        #endregion
    }
}
