using System.IO.Pipes;
using NamedPipeWrapper.IO;

namespace NamedPipeWrapper
{
    internal static class PipeClientFactory
    {
        public static PipeStreamWrapper<TRead, TWrite> Connect<TRead, TWrite>(string pipeName, string serverName)
            where TRead : class
            where TWrite : class
        {
            return new PipeStreamWrapper<TRead, TWrite>(CreateAndConnectPipe(pipeName, serverName));
        }

        public static NamedPipeClientStream CreateAndConnectPipe(string pipeName, string serverName)
        {
            var pipe = CreatePipe(pipeName, serverName);
            pipe.Connect();

            return pipe;
        }

        private static NamedPipeClientStream CreatePipe(string pipeName, string serverName)
        {
            return new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        }
    }
}