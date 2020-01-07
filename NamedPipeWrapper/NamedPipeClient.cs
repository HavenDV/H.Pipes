using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using NamedPipeWrapper.Args;
using NamedPipeWrapper.Factories;

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
    public class NamedPipeClient<TRead, TWrite> : IAsyncDisposable
        where TRead : class
        where TWrite : class
    {
        /// <summary>
        /// Gets or sets whether the client should attempt to reconnect when the pipe breaks
        /// due to an error or the other end terminating the connection. <br/>
        /// Default value is <see langword="true"/>.
        /// </summary>
        public bool AutoReconnect { get; set; }

        public bool IsConnected => Connection != null;

        private string PipeName { get; }
        private string ServerName { get; }

        private NamedPipeConnection<TRead, TWrite>? Connection { get; set; }

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

        #region Constructors

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

        #endregion

        /// <summary>
        /// Connects to the named pipe server asynchronously.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("Already connected");
            }

            var connectionPipeName = await GetConnectionPipeName(cancellationToken).ConfigureAwait(false);

            // Connect to the actual data pipe
            var dataPipe = await PipeClientFactory.CreateAndConnectAsync(connectionPipeName, ServerName, cancellationToken).ConfigureAwait(false);

            // Create a Connection object for the data pipe
            Connection = ConnectionFactory.Create<TRead, TWrite>(dataPipe);
            Connection.Disconnected += (sender, args) => OnDisconnected(args);
            Connection.MessageReceived += (sender, args) => OnMessageReceived(args);
            Connection.ExceptionOccurred += (sender, args) => OnExceptionOccurred(args.Exception);
            Connection.Start();
        }

        public async Task DisconnectAsync()
        {
            if (Connection == null) // nullable detection system is not very smart
            {
                return;
            }

            await Connection.DisposeAsync().ConfigureAwait(false);

            Connection = null;
        }

        /// <summary>
        ///     Sends a message to the server over a named pipe.
        /// </summary>
        /// <param name="value">Message to send to the server.</param>
        /// <param name="cancellationToken"></param>
        public async Task<bool> WriteAsync(TWrite value, CancellationToken cancellationToken = default)
        {
            if (Connection == null)
            {
                return false;
            }

            return await Connection.WriteAsync(value, cancellationToken).ConfigureAwait(false);
        }

        #region IDisposable

        /// <summary>
        /// Dispose internal resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (Connection != null)
            {
                await Connection.DisposeAsync().ConfigureAwait(false);

                Connection = null;
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Get the name of the data pipe that should be used from now on by this NamedPipeClient
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <returns></returns>
        private async Task<string> GetConnectionPipeName(CancellationToken cancellationToken = default)
        {
            using var handshake = await PipeClientFactory.ConnectAsync<string, string>(PipeName, ServerName, cancellationToken).ConfigureAwait(false);
            var pipeName = await handshake.ReadObjectAsync(cancellationToken).ConfigureAwait(false);

            return pipeName ?? throw new InvalidOperationException("Returned by server pipeName is null");
        }

        #endregion
    }
}
