using System.IO.Pipes;
using NamedPipeWrapper.Formatters;

namespace NamedPipeWrapper.Factories
{
    internal static class ConnectionFactory
    {
        private static int LastId { get; set; }

        public static NamedPipeConnection<T> Create<T>(PipeStream pipeStream, IFormatter formatter)
            where T : class
        {
            return new NamedPipeConnection<T>(++LastId, "Client " + LastId, pipeStream, formatter);
        }
    }
}