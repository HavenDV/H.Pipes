using System.IO.Pipes;
using H.Pipes.Formatters;

namespace H.Pipes.Factories
{
    internal static class ConnectionFactory
    {
        private static int LastId { get; set; }

        public static PipeConnection<T> Create<T>(PipeStream pipeStream, IFormatter formatter)
        {
            return new PipeConnection<T>(++LastId, "Client " + LastId, pipeStream, formatter);
        }
    }
}