using NamedPipeWrapper.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NamedPipeWrapper.Args;
using NamedPipeWrapper.Factories;
using NamedPipeWrapper.Utilities;

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
    public class NamedPipeServer<TRead, TWrite> : IAsyncDisposable
        where TRead : class
        where TWrite : class
    {
        #region Properties

        private string PipeName { get; }
        private List<NamedPipeConnection<TRead, TWrite>> Connections { get; } = new List<NamedPipeConnection<TRead, TWrite>>();

        private int NextPipeId { get; set; }

        private Worker? ListenWorker { get; set; }

        private volatile bool _isDisposed;

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

        #region Constructors

        /// <summary>
        /// Constructs a new <c>NamedPipeServer</c> object that listens for client connections on the given <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the pipe to listen on</param>
        public NamedPipeServer(string pipeName)
        {
            PipeName = pipeName;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Begins listening for client connections in a separate background thread.
        /// This method waits when pipe will be created(or throws exception).
        /// </summary>
        /// <exception cref="IOException"></exception>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            var source = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => source.TrySetCanceled(cancellationToken));

            ListenWorker = new Worker(async token =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var connectionPipeName = $"{PipeName}_{++NextPipeId}";

                        // Send the client the name of the data pipe to use
                        try
                        {
                            using var handshakePipe = PipeServerFactory.Create(PipeName);

                            source.TrySetResult(true);

                            await handshakePipe.WaitForConnectionAsync(token).ConfigureAwait(false);

                            using var handshakeWrapper = new PipeStreamWrapper<string, string>(handshakePipe);

                            await handshakeWrapper.WriteObjectAsync(connectionPipeName, token).ConfigureAwait(false);
                        }
                        catch (Exception exception)
                        {
                            source.TrySetException(exception);
                            throw;
                        }

                        // Wait for the client to connect to the data pipe
                        var dataPipe = await PipeServerFactory.CreateAndWaitAsync(connectionPipeName, token).ConfigureAwait(false);

                        // Add the client's connection to the list of connections
                        var connection = ConnectionFactory.Create<TRead, TWrite>(dataPipe);
                        connection.MessageReceived += (sender, args) => OnMessageReceived(args);
                        connection.Disconnected += (sender, args) => OnClientDisconnected(args);
                        connection.ExceptionOccurred += (sender, args) => OnExceptionOccurred(args.Exception);
                        connection.Start();

                        Connections.Add(connection);

                        OnClientConnected(new ConnectionEventArgs<TRead, TWrite>(connection));
                    }
                    catch (TaskCanceledException)
                    {
                        throw;
                    }
                    // Catch the IOException that is raised if the pipe is broken or disconnected.
                    catch (IOException)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(1), token).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        OnExceptionOccurred(exception);
                    }
                }
            }, OnExceptionOccurred);

            await source.Task;
        }

        /// <summary>
        /// Sends a message to all connected clients asynchronously.
        /// This method returns immediately, possibly before the message has been sent to all clients.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="predicate"></param>
        /// <param name="cancellationToken"></param>
        public async Task WriteAsync(TWrite value, Predicate<NamedPipeConnection<TRead, TWrite>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var tasks = Connections
                .Where(connection => predicate == null || predicate(connection))
                .Select(connection => connection.WriteAsync(value, cancellationToken))
                .ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// push message to the given client.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="clientName"></param>
        /// <param name="cancellationToken"></param>
        public async Task WriteAsync(TWrite value, string clientName, CancellationToken cancellationToken = default)
        {
            await WriteAsync(value, connection => connection.Name == clientName, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Closes all open client connections and stops listening for new ones.
        /// </summary>
        public async Task StopAsync(CancellationToken _ = default)
        {
            if (ListenWorker != null)
            {
                await ListenWorker.DisposeAsync().ConfigureAwait(false);
            }

            var tasks = Connections
                .Select(connection => connection.DisposeAsync().AsTask())
                .ToList();

            Connections.Clear();

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        #endregion

        #region IDisposable

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

            if (ListenWorker != null)
            {
                await ListenWorker.DisposeAsync().ConfigureAwait(false);
            }

            var tasks = Connections
                .Select(connection => connection.DisposeAsync().AsTask())
                .ToList();

            Connections.Clear();

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        #endregion
    }
}
