using System.IO.Pipes;
using H.Formatters;
using H.Pipes.Args;
using H.Pipes.Extensions;
using H.Pipes.IO;
using H.Pipes.Utilities;

namespace H.Pipes;

/// <inheritdoc/>
/// <typeparam name="T">Reference type to read/write from the named pipe</typeparam>
public class PipeConnection<T> : PipeConnection
{
    #region Constructors

    /// <inheritdoc />
    internal PipeConnection(PipeStream stream, string pipeName, IFormatter formatter, string serverName = "")
        : base(stream, pipeName, formatter, serverName) { }

    #endregion

    #region Events
    
    /// <summary>
    ///     Invoked when an exception is thrown during any read/write operation over the named
    ///     pipe.
    /// </summary>
    public new event EventHandler<ConnectionMessageEventArgs<T?>>? MessageReceived;

    /// <inheritdoc />
    protected override async Task OnMessageReceived(byte[]? message, CancellationToken cancellationToken)
    {
        T? obj = default;

        if (message != null)
            obj = await message.DeserializeAsync<T>(Formatter, cancellationToken).ConfigureAwait(false);

        MessageReceived?.Invoke(this, new ConnectionMessageEventArgs<T?>(this, obj));
    }

    #endregion
}

/// <summary>
///     Represents a connection between a named pipe client and server. Implements the
///     <see cref="System.IAsyncDisposable" /> Implements the
///     <see cref="H.Pipes.IPipeConnection" />
/// </summary>
/// <seealso cref="System.IAsyncDisposable" />
/// <seealso cref="H.Pipes.IPipeConnection" />
public class PipeConnection : IPipeConnection, IAsyncDisposable
{
    #region Properties
    
    /// <inheritdoc />
    public string PipeName { get; }
    
    /// <inheritdoc />
    public string ServerName { get; }
    
    /// <inheritdoc />
    public bool IsConnected => PipeStreamWrapper.IsConnected;
    
    /// <inheritdoc />
    public bool IsStarted => ReadWorker != null;
    
    /// <inheritdoc />
    public PipeStream PipeStream { get; }

    /// <inheritdoc />
    public IFormatter Formatter { get; set; }

    private PipeStreamWrapper PipeStreamWrapper { get; }
    private TaskWorker? ReadWorker { get; set; }

    #endregion

    #region Events
    
    /// <inheritdoc />
    public event EventHandler<ConnectionEventArgs>? Disconnected;
    
    /// <inheritdoc />
    public event EventHandler<ConnectionMessageEventArgs<byte[]?>>? MessageReceived;
    
    /// <inheritdoc />
    public event EventHandler<ConnectionExceptionEventArgs>? ExceptionOccurred;

    /// <summary>
    /// Calls the <see cref="Disconnected"/> event.
    /// </summary>
    protected virtual void OnDisconnected()
    {
        Disconnected?.Invoke(this, new ConnectionEventArgs(this));
    }

    /// <summary>
    /// Calls the <see cref="MessageReceived"/> event.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken"></param>
    protected virtual Task OnMessageReceived(byte[]? message, CancellationToken cancellationToken)
    {
        MessageReceived?.Invoke(this, new ConnectionMessageEventArgs<byte[]?>(this, message));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Calls the <see cref="ExceptionOccurred"/> event.
    /// </summary>
    /// <param name="exception">The exception.</param>
    protected virtual void OnExceptionOccurred(Exception exception)
    {
        ExceptionOccurred?.Invoke(this, new ConnectionExceptionEventArgs(this, exception));
    }

    #endregion

    #region Constructors

    internal PipeConnection(PipeStream stream, string pipeName, IFormatter formatter, string serverName = "")
    {
        PipeName = pipeName;
        PipeStream = stream;
        PipeStreamWrapper = new PipeStreamWrapper(stream);
        Formatter = formatter;
        ServerName = serverName;
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public void Start()
    {
        if (IsStarted)
        {
            throw new InvalidOperationException("Connection already started");
        }

        ReadWorker = new TaskWorker(async cancellationToken =>
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                try
                {
                    var bytes = await PipeStreamWrapper.ReadAsync(cancellationToken).ConfigureAwait(false);

                    // We accept zero-length messages
                    if (bytes == null && !IsConnected)
                    {
                        break;
                    }

                    await OnMessageReceived(bytes, cancellationToken).ConfigureAwait(false);
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
    
    /// <inheritdoc />
    public async Task WriteAsync(byte[] value, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || !PipeStreamWrapper.CanWrite)
        {
            throw new InvalidOperationException("Client is not connected");
        }

        await PipeStreamWrapper.WriteAsync(value, cancellationToken).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    public async Task WriteAsync<T>(T value, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || !PipeStreamWrapper.CanWrite)
        {
            throw new InvalidOperationException("Client is not connected");
        }

        var bytes = await value.SerializeAsync(Formatter, cancellationToken).ConfigureAwait(false);

        await PipeStreamWrapper.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (ReadWorker != null)
        {
            await ReadWorker.StopAsync().ConfigureAwait(false);

            ReadWorker = null;
        }

        await PipeStreamWrapper.StopAsync().ConfigureAwait(false);
    }
    
    /// <inheritdoc />
    public string GetImpersonationUserName()
    {
        if (PipeStream is not NamedPipeServerStream serverStream)
        {
            throw new InvalidOperationException($"{nameof(PipeStream)} is not {nameof(NamedPipeServerStream)}.");
        }

        return serverStream.GetImpersonationUserName();
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Dispose internal resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }

    #endregion
}
