using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes.Args;
using H.Pipes.Factories;
using H.Formatters;

namespace H.Pipes
{
    /// <summary>
    /// Wraps a <see cref="NamedPipeClientStream"/>.
    /// </summary>
    /// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
    public sealed class PipeClient<T> : IPipeClient<T>
    {
        #region Fields

        private volatile bool _isConnecting;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether the client should attempt to reconnect when the pipe breaks
        /// due to an error or the other end terminating the connection. <br/>
        /// Default value is <see langword="true"/>.
        /// </summary>
        public bool AutoReconnect { get; set; } = true;

        /// <summary>
        /// Interval of reconnection
        /// </summary>
        public TimeSpan ReconnectionInterval { get; }

        /// <summary>
        /// Checks that connection is exists
        /// </summary>
        public bool IsConnected => Connection != null;

        /// <summary>
        /// <see langword="true"/> if <see cref="ConnectAsync"/> in process
        /// </summary>
        public bool IsConnecting
        {
            get => _isConnecting;
            private set => _isConnecting = value;
        }

        private string PipeName { get; }
        private string ServerName { get; }
        private IFormatter Formatter { get; }

        private PipeConnection<T>? Connection { get; set; }
        private System.Timers.Timer ReconnectionTimer { get; }

        #endregion

        #region Events

        /// <summary>
        /// Invoked whenever a message is received from the server.
        /// </summary>
        public event EventHandler<ConnectionMessageEventArgs<T?>>? MessageReceived;

        /// <summary>
        /// Invoked when the client disconnects from the server (e.g., the pipe is closed or broken).
        /// </summary>
        public event EventHandler<ConnectionEventArgs<T>>? Disconnected;

        /// <summary>
        /// Invoked after each the client connect to the server (include reconnects).
        /// </summary>
        public event EventHandler<ConnectionEventArgs<T>>? Connected;

        /// <summary>
        /// Invoked whenever an exception is thrown during a read or write operation on the named pipe.
        /// </summary>
        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        private void OnMessageReceived(ConnectionMessageEventArgs<T?> args)
        {
            MessageReceived?.Invoke(this, args);
        }

        private void OnDisconnected(ConnectionEventArgs<T> args)
        {
            Disconnected?.Invoke(this, args);
        }

        private void OnConnected(ConnectionEventArgs<T> args)
        {
            Connected?.Invoke(this, args);
        }

        private void OnExceptionOccurred(Exception exception)
        {
            ExceptionOccurred?.Invoke(this, new ExceptionEventArgs(exception));
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new <see cref="PipeClient{T}"/> to connect to the <see cref="PipeServer{T}"/> specified by <paramref name="pipeName"/>. <br/>
        /// Default reconnection interval - <see langword="100 ms"/>
        /// </summary>
        /// <param name="pipeName">Name of the server's pipe</param>
        /// <param name="serverName">the Name of the server, default is  local machine</param>
        /// <param name="reconnectionInterval">Default reconnection interval - <see langword="100 ms"/></param>
        /// <param name="formatter">Default formatter - <see cref="BinaryFormatter"/></param>
        public PipeClient(string pipeName, string serverName = ".", TimeSpan? reconnectionInterval = default, IFormatter? formatter = default)
        {
            PipeName = pipeName;
            ServerName = serverName;

            ReconnectionInterval = reconnectionInterval ?? TimeSpan.FromMilliseconds(100);
            ReconnectionTimer = new System.Timers.Timer(ReconnectionInterval.TotalMilliseconds);
            ReconnectionTimer.Elapsed += async (_, _) =>
            {
                try
                {
                    if (!IsConnected && !IsConnecting)
                    {
                        using var cancellationTokenSource = new CancellationTokenSource(ReconnectionInterval);

                        try
                        {
                            await ConnectAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                }
                catch (Exception exception)
                {
                    ReconnectionTimer.Stop();

                    OnExceptionOccurred(exception);
                }
            };

            Formatter = formatter ?? new BinaryFormatter();
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Connects to the named pipe server asynchronously.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            while (IsConnecting)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);
            }

            if (IsConnected)
            {
                return;
            }

            try
            {
                IsConnecting = true;

                if (AutoReconnect)
                {
                    ReconnectionTimer.Start();
                }

                var connectionPipeName = await GetConnectionPipeName(cancellationToken).ConfigureAwait(false);

                // Connect to the actual data pipe
#pragma warning disable CA2000 // Dispose objects before losing scope
                var dataPipe = await PipeClientFactory
                    .CreateAndConnectAsync(connectionPipeName, ServerName, cancellationToken)
#pragma warning restore CA2000 // Dispose objects before losing scope
                    .ConfigureAwait(false);

                // Create a Connection object for the data pipe
                Connection = ConnectionFactory.Create<T>(dataPipe, Formatter);
                Connection.Disconnected += async (_, args) =>
                {
                    await DisconnectInternalAsync().ConfigureAwait(false);

                    OnDisconnected(args);
                };
                Connection.MessageReceived += (_, args) => OnMessageReceived(args);
                Connection.ExceptionOccurred += (_, args) => OnExceptionOccurred(args.Exception);
                Connection.Start();

                OnConnected(new ConnectionEventArgs<T>(Connection));
            }
            catch (Exception)
            {
                ReconnectionTimer.Stop();

                throw;
            }
            finally
            {
                IsConnecting = false;
            }
        }

        /// <summary>
        /// Disconnects from server
        /// </summary>
        /// <param name="_"></param>
        /// <returns></returns>
        public async Task DisconnectAsync(CancellationToken _ = default)
        {
            ReconnectionTimer.Stop();

            await DisconnectInternalAsync().ConfigureAwait(false);
        }

        private async Task DisconnectInternalAsync()
        {
            if (Connection == null)
            {
                return;
            }

            await Connection.DisposeAsync().ConfigureAwait(false);

            Connection = null;
        }

        /// <summary>
        /// Sends a message to the server over a named pipe. <br/>
        /// If client is not connected, <see cref="InvalidOperationException"/> is occurred
        /// </summary>
        /// <param name="value">Message to send to the server.</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task WriteAsync(T value, CancellationToken cancellationToken = default)
        {
            if (!IsConnected && AutoReconnect)
            {
                await ConnectAsync(cancellationToken).ConfigureAwait(false);
            }
            if (Connection == null)
            {
                throw new InvalidOperationException("Client is not connected");
            }

            await Connection.WriteAsync(value, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose internal resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            ReconnectionTimer.Dispose();

            await DisconnectInternalAsync().ConfigureAwait(false);
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
#if NETSTANDARD2_1
            await using var handshake = await PipeClientFactory.ConnectAsync(PipeName, ServerName, cancellationToken).ConfigureAwait(false);
#else
            using var handshake = await PipeClientFactory.ConnectAsync(PipeName, ServerName, cancellationToken).ConfigureAwait(false);
#endif

            var bytes = await handshake.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (bytes == null)
            {
                throw new InvalidOperationException("Connection failed: Returned by server pipeName is null");
            }

            return Encoding.UTF8.GetString(bytes);
        }

        #endregion
    }
}
