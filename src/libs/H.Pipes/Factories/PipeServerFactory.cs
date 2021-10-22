using System.IO.Pipes;

namespace H.Pipes.Factories;

/// <summary>
/// Internal usage
/// </summary>
public static class PipeServerFactory
{
    /// <summary>
    /// Creates new <see cref="NamedPipeServerStream"/> and waits any connection
    /// </summary>
    /// <param name="pipeName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<NamedPipeServerStream> CreateAndWaitAsync(string pipeName, CancellationToken cancellationToken = default)
    {
        var pipe = Create(pipeName);

        try
        {
            await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

            return pipe;
        }
        catch
        {
#if NETSTANDARD2_1
            await pipe.DisposeAsync().ConfigureAwait(false);
#else
                pipe.Dispose();
#endif

            throw;
        }
    }

    /// <summary>
    /// Creates new <see cref="NamedPipeServerStream"/>
    /// </summary>
    /// <param name="pipeName"></param>
    /// <returns></returns>
    public static NamedPipeServerStream Create(string pipeName)
    {
        return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough, 0, 0);
    }
}
