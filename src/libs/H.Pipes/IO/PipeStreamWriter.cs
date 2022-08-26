using System.IO.Pipes;
using System.Net;

namespace H.Pipes.IO;

/// <summary>
/// Wraps a <see cref="PipeStream"/> object and writes to it.
/// </summary>
public sealed class PipeStreamWriter : IDisposable
#if NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER
        , IAsyncDisposable
#elif NET461_OR_GREATER || NETSTANDARD2_0
#else
#error Target Framework is not supported
#endif
{
    #region Properties

    /// <summary>
    /// Gets the underlying <c>PipeStream</c> object.
    /// </summary>
    private PipeStream BaseStream { get; }
    private SemaphoreSlim SemaphoreSlim { get; } = new SemaphoreSlim(1, 1);

    #endregion

    #region Constructors

    /// <summary>
    /// Constructs a new <c>PipeStreamWriter</c> object that writes to given <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Pipe to write to</param>
    public PipeStreamWriter(PipeStream stream)
    {
        BaseStream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    #endregion

    #region Private stream writers

    private async Task WriteLengthAsync(int length, CancellationToken cancellationToken = default)
    {
        var buffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(length));

#if NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER
        await BaseStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
#elif NET461_OR_GREATER || NETSTANDARD2_0
        await BaseStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
#else
#error Target Framework is not supported
#endif
    }

    /// <summary>
    /// Writes an object to the pipe.
    /// </summary>
    /// <param name="buffer">Object to write to the pipe</param>
    /// <param name="cancellationToken"></param>
    public async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await SemaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            await WriteLengthAsync(buffer.Length, cancellationToken).ConfigureAwait(false);

#if NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER
            await BaseStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
#elif NET461_OR_GREATER || NETSTANDARD2_0
            await BaseStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
#else
#error Target Framework is not supported
#endif

            try
            {
                await BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // ignoring IOException according this issue: https://github.com/HavenDV/H.Pipes/issues/22#issuecomment-1144234017
            }
        }
        finally
        {
            SemaphoreSlim.Release();
        }
    }

#if NET461_OR_GREATER || NET5_0_OR_GREATER
    /// <summary>
    /// Waits for the other end of the pipe to read all sent bytes.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The pipe is closed.</exception>
    /// <exception cref="NotSupportedException">The pipe does not support write operations.</exception>
    /// <exception cref="IOException">The pipe is broken or another I/O error occurred.</exception>
    public void WaitForPipeDrain()
    {
#if NET5_0_OR_GREATER
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
#endif

        try
        {
            BaseStream.WaitForPipeDrain();
        }
        catch (IOException)
        {
            // ignoring IOException according this issue: https://github.com/HavenDV/H.Pipes/issues/22#issuecomment-1144234017
        }
    }
#elif NETSTANDARD2_0_OR_GREATER
#else
#error Target Framework is not supported
#endif

    #endregion

    #region IDisposable

    /// <summary>
    /// Dispose internal <see cref="PipeStream"/>
    /// </summary>
    public void Dispose()
    {
        BaseStream.Dispose();
        SemaphoreSlim.Dispose();
    }

#if NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Dispose internal <see cref="PipeStream"/>
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await BaseStream.DisposeAsync().ConfigureAwait(false);

        SemaphoreSlim.Dispose();
    }
#elif NET461_OR_GREATER || NETSTANDARD2_0
#else
#error Target Framework is not supported
#endif

    #endregion
}
