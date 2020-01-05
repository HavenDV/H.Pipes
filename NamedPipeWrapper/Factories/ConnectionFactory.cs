using System.IO.Pipes;

namespace NamedPipeWrapper.Factories
{
    internal static class ConnectionFactory
    {
        private static int LastId { get; set; }

        public static NamedPipeConnection<TRead, TWrite> CreateConnection<TRead, TWrite>(PipeStream pipeStream)
            where TRead : class
            where TWrite : class
        {
            return new NamedPipeConnection<TRead, TWrite>(++LastId, "Client " + LastId, pipeStream);
        }
    }
}