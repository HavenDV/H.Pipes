﻿using System.IO.Pipes;
using System.Net;

namespace H.Pipes.IO;

/// <summary>
/// Wraps a <see cref="PipeStream"/> object and reads from it.
/// </summary>
public sealed class PipeStreamReader : IDisposable
#if NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER
        , IAsyncDisposable
#elif NET461_OR_GREATER || NETSTANDARD2_0
#else
#error Target Framework is not supported
#endif
{
    #region Properties

    /// <summary>
    /// Gets a value indicating whether the pipe is connected or not.
    /// </summary>
    public bool IsConnected { get; private set; }

    private PipeStream BaseStream { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Constructs a new <c>PipeStreamReader</c> object that reads data from the given <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Pipe to read from</param>
    public PipeStreamReader(PipeStream stream)
    {
        BaseStream = stream ?? throw new ArgumentNullException(nameof(stream));
        IsConnected = stream.IsConnected;
    }

    #endregion

    #region Private stream readers

    /// <summary>
    /// Reads the length of the next message (in bytes) from the client.
    /// </summary>
    /// <returns>Number of bytes of data the client will be sending.</returns>
    /// <exception cref="InvalidOperationException">The pipe is disconnected, waiting to connect, or the handle has not been set.</exception>
    /// <exception cref="IOException">Any I/O error occurred.</exception>
    private async Task<int> ReadLengthAsync(CancellationToken cancellationToken = default)
    {
        var buffer = new byte[sizeof(int)];
#if NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER
        var read = await BaseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
#elif NET461_OR_GREATER || NETSTANDARD2_0
        var read = await BaseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
#else
#error Target Framework is not supported
#endif
        if (read == 0)
        {
            IsConnected = false;
            return 0;
        }

        if (read != buffer.Length)
        {
            throw new IOException($"Expected {buffer.Length} bytes but read {read}");
        }

        return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
    }

    private async Task<byte[]> ReadAsync(int length, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[length];
#if NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER
        await BaseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
#elif NET461_OR_GREATER || NETSTANDARD2_0
        await BaseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
#else
#error Target Framework is not supported
#endif

        return buffer;
    }

    /// <summary>
    /// Reads the next object from the pipe.  This method waits until an object is sent
    /// or the pipe is disconnected.
    /// </summary>
    /// <returns>The next object read from the pipe, or <c>null</c> if the pipe disconnected.</returns>
    public async Task<byte[]?> ReadAsync(CancellationToken cancellationToken = default)
    {
        var length = await ReadLengthAsync(cancellationToken).ConfigureAwait(false);

        return length == 0
            ? default
            : await ReadAsync(length, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Dispose internal <see cref="PipeStream"/>
    /// </summary>
    public void Dispose()
    {
        BaseStream.Dispose();
    }

#if NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Dispose internal <see cref="PipeStream"/>
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await BaseStream.DisposeAsync().ConfigureAwait(false);
    }
#elif NET461_OR_GREATER || NETSTANDARD2_0
#else
#error Target Framework is not supported
#endif

    #endregion
}
