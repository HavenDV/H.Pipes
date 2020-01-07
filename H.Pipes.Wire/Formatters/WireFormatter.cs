using System.IO;
using NamedPipeWrapper.Formatters;
using Wire;

namespace H.Pipes.Formatters
{
    public class WireFormatter : IFormatter
    {
        private Serializer Serializer { get; } = new Serializer();

        public byte[] Serialize(object obj)
        {
            using var stream = new MemoryStream();
            Serializer.Serialize(obj, stream);

            return stream.ToArray();
        }

        public T Deserialize<T>(byte[] bytes)
        {
            using var memoryStream = new MemoryStream(bytes);

            return (T)Serializer.Deserialize(memoryStream);
        }
    }
}
