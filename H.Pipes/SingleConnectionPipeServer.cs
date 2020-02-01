using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using H.Formatters;
using H.Pipes.Args;
using H.Pipes.Factories;
using H.Pipes.Utilities;

namespace H.Pipes
{
    /// <summary>
    /// Wraps a <see cref="NamedPipeServerStream"/> and optimized for one connection.
    /// </summary>
    /// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
    public class SingleConnectionPipeServer<T> : IPipeServer<T>
    {
        #region Properties

        /// <summary>
        /// Name of pipe
        /// </summary>
        public string PipeName { get; }

        /// <summary>
        /// CreatePipeStreamFunc
        /// </summary>
        public Func<string, NamedPipeServerStream>? CreatePipeStreamFunc { get; set; }

        /// <summary>
        /// PipeStreamInitializeAction
        /// </summary>
        public Action<NamedPipeServerStream>? PipeStreamInitializeAction { get; set; }

        /// <summary>
        /// Used formatter
        /// </summary>
        public IFormatter Formatter { get; set; }

        /// <summary>
        /// Indicates whether to wait for a name to be released when calling StartAsync()
        /// </summary>
        public bool WaitFreePipe { get; set; }

        /// <summary>
        /// Connection
        /// </summary>
        public PipeConnection<T>? Connection { get; private set; }

        /// <summary>
        /// IsStarted
        /// </summary>
        public bool IsStarted => ListenWorker != null && !ListenWorker.Task.IsCompleted && !ListenWorker.Task.IsCanceled && !ListenWorker.Task.IsFaulted;


        private TaskWorker? ListenWorker { get; set; }

        private volatile bool _isDisposed;

        #endregion

        #region Events

        /// <summary>
        /// Invoked whenever a client connects to the server.
        /// </summary>
        public event EventHandler<ConnectionEventArgs<T>>? ClientConnected;

        /// <summary>
        /// Invoked whenever a client disconnects from the server.
        /// </summary>
        public event EventHandler<ConnectionEventArgs<T>>? ClientDisconnected;

        /// <summary>
        /// Invoked whenever a client sends a message to the server.
        /// </summary>
        public event EventHandler<ConnectionMessageEventArgs<T>>? MessageReceived;

        /// <summary>
        /// Invoked whenever an exception is thrown during a read or write operation.
        /// </summary>
        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        private void OnClientConnected(ConnectionEventArgs<T> args)
        {
            ClientConnected?.Invoke(this, args);
        }

        private void OnClientDisconnected(ConnectionEventArgs<T> args)
        {
            ClientDisconnected?.Invoke(this, args);
        }

        private void OnMessageReceived(ConnectionMessageEventArgs<T> args)
        {
            MessageReceived?.Invoke(this, args);
        }

        private void OnExceptionOccurred(Exception exception)
        {
            ExceptionOccurred?.Invoke(this, new ExceptionEventArgs(exception));
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new <c>NamedPipeServer</c> object that listens for client connections on the given <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the pipe to listen on</param>
        /// <param name="formatter">Default formatter - <see cref="BinaryFormatter"/></param>
        public SingleConnectionPipeServer(string pipeName, IFormatter? formatter = default)
        {
            PipeName = pipeName;
            Formatter = formatter ?? new BinaryFormatter();
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Begins listening for client connections in a separate background thread.
        /// This method waits when pipe will be created(or throws exception).
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="IOException"></exception>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (IsStarted)
            {
                throw new InvalidOperationException("Server already started");
            }

            await StopAsync(cancellationToken);

            var source = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => source.TrySetCanceled(cancellationToken));

            ListenWorker = new TaskWorker(async token =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (Connection != null && Connection.IsConnected)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        if (Connection != null)
                        {
                            await Connection.DisposeAsync().ConfigureAwait(false);
                        }

                        // Wait for the client to connect to the data pipe
                        var connectionStream = CreatePipeStreamFunc?.Invoke(PipeName) ?? PipeServerFactory.Create(PipeName);

                        try
                        {
                            PipeStreamInitializeAction?.Invoke(connectionStream);

                            source.TrySetResult(true);

                            await connectionStream.WaitForConnectionAsync(token).ConfigureAwait(false);
                        }
                        catch
                        {
#if NETSTANDARD2_0
                            connectionStream.Dispose();
#else
                            await connectionStream.DisposeAsync().ConfigureAwait(false);
#endif

                            throw;
                        }

                        var connection = ConnectionFactory.Create<T>(connectionStream, Formatter);
                        try
                        {
                            connection.MessageReceived += (sender, args) => OnMessageReceived(args);
                            connection.Disconnected += (sender, args) => OnClientDisconnected(args);
                            connection.ExceptionOccurred += (sender, args) => OnExceptionOccurred(args.Exception);
                            connection.Start();
                        }
                        catch
                        {
                            await connection.DisposeAsync().ConfigureAwait(false);

                            throw;
                        }

                        Connection = connection;

                        OnClientConnected(new ConnectionEventArgs<T>(connection));
                    }
                    catch (OperationCanceledException)
                    {
                        if (Connection != null)
                        {
                            await Connection.DisposeAsync().ConfigureAwait(false);

                            Connection = null;
                        }
                        throw;
                    }
                    // Catch the IOException that is raised if the pipe is broken or disconnected.
                    catch (IOException exception)
                    {
                        if (!WaitFreePipe)
                        {
                            source.TrySetException(exception);
                            break;
                        }

                        await Task.Delay(TimeSpan.FromMilliseconds(1), token).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        OnExceptionOccurred(exception);
                        break;
                    }
                }
            }, OnExceptionOccurred);

            try
            {
                await source.Task.ConfigureAwait(false);
            }
            catch (Exception)
            {
                await StopAsync(cancellationToken);

                throw;
            }
        }

        /// <summary>
        /// Sends a message to all connected clients asynchronously.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        public async Task WriteAsync(T value, CancellationToken cancellationToken = default)
        {
            if (Connection == null || !Connection.IsConnected)
            {
                return;
            }

            await Connection.WriteAsync(value, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Closes all open client connections and stops listening for new ones.
        /// </summary>
        public async Task StopAsync(CancellationToken _ = default)
        {
            if (ListenWorker != null)
            {
                await ListenWorker.DisposeAsync().ConfigureAwait(false);

                ListenWorker = null;
            }

            if (Connection != null)
            {
                await Connection.DisposeAsync().ConfigureAwait(false);

                Connection = null;
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose internal resources
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            ListenWorker?.Dispose();
            ListenWorker = null;

            Connection?.Dispose();
            Connection = null;
        }

        /// <summary>
        /// Dispose internal resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            await StopAsync().ConfigureAwait(false);
        }

        #endregion
    }
}
