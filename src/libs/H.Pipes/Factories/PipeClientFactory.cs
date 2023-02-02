using System.IO.Pipes;
using H.Pipes.IO;

namespace H.Pipes.Factories;

/// <summary>
/// Internal usage
/// </summary>
public static class PipeClientFactory
{
    /// <summary>
    /// Internal usage
    /// </summary>
    public static async Task<PipeStreamWrapper> ConnectAsync(
        string pipeName,
        string serverName,
        Func<string, string, NamedPipeClientStream>? func = null,
        CancellationToken cancellationToken = default)
    {
        var pipe = await CreateAndConnectAsync(pipeName, serverName, func, cancellationToken).ConfigureAwait(false);

        return new PipeStreamWrapper(pipe);
    }

    /// <summary>
    /// Internal usage
    /// </summary>
    public static async Task<NamedPipeClientStream> CreateAndConnectAsync(
        string pipeName,
        string serverName,
        Func<string, string, NamedPipeClientStream>? func = null,
        CancellationToken cancellationToken = default)
    {
        var pipe = func != null
            ? func(pipeName, serverName)
#pragma warning disable CA2000
            : Create(pipeName, serverName);
#pragma warning restore CA2000

        try
        {
            await pipe.ConnectAsync(cancellationToken).ConfigureAwait(false);

            return pipe;
        }
        catch
        {
#if NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER
            await pipe.DisposeAsync().ConfigureAwait(false);
#elif NET461_OR_GREATER || NETSTANDARD2_0
            pipe.Dispose();
#else
#error Target Framework is not supported
#endif

            throw;
        }
    }

    /// <summary>
    /// Internal usage
    /// </summary>
    public static NamedPipeClientStream Create(string pipeName, string serverName)
    {
        return new NamedPipeClientStream(
            serverName,
            pipeName,
            direction: PipeDirection.InOut,
            options: PipeOptions.Asynchronous | PipeOptions.WriteThrough);
    }
}
