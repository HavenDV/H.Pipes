using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace H.Pipes.Factories
{
    public static class PipeServerFactory
    {
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
                pipe.Dispose();

                throw;
            }
        }

        public static NamedPipeServerStream Create(string pipeName)
        {
            return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough, 0, 0);
        }
    }
}