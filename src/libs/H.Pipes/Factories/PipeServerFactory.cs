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
    /// Creates new <see cref="NamedPipeServerStream"/>
    /// </summary>
    /// <param name="pipeName"></param>
    /// <returns></returns>
    public static NamedPipeServerStream Create(string pipeName)
    {
        return new NamedPipeServerStream(
            pipeName: pipeName,
            direction: PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            transmissionMode: PipeTransmissionMode.Byte,
            options: PipeOptions.Asynchronous | PipeOptions.WriteThrough,
            inBufferSize: 0,
            outBufferSize: 0);
    }
}
