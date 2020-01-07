using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using NamedPipeWrapper.IO;

namespace NamedPipeWrapper.Factories
{
    public static class PipeClientFactory
    {
        public static async Task<PipeStreamWrapper<TRead, TWrite>> ConnectAsync<TRead, TWrite>(string pipeName, string serverName, CancellationToken cancellationToken = default)
            where TRead : class
            where TWrite : class
        {
            var pipe = await CreateAndConnectAsync(pipeName, serverName, cancellationToken).ConfigureAwait(false);

            return new PipeStreamWrapper<TRead, TWrite>(pipe);
        }

        public static async Task<NamedPipeClientStream> CreateAndConnectAsync(string pipeName, string serverName, CancellationToken cancellationToken = default)
        {
            var pipe = Create(pipeName, serverName);

            try
            {
                await pipe.ConnectAsync(cancellationToken).ConfigureAwait(false);

                return pipe;
            }
            catch
            {
                pipe.Dispose();

                throw;
            }
        }

        private static NamedPipeClientStream Create(string pipeName, string serverName)
        {
            return new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        }
    }
}