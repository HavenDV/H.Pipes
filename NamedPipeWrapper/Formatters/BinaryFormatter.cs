using System.IO;

namespace NamedPipeWrapper.Formatters
{
    public class BinaryFormatter : IFormatter
    {
        private System.Runtime.Serialization.Formatters.Binary.BinaryFormatter InternalFormatter { get; } = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

        public byte[] Serialize(object obj)
        {
            using var stream = new MemoryStream();
            InternalFormatter.Serialize(stream, obj);

            return stream.ToArray();
        }

        public T Deserialize<T>(byte[] bytes)
        {
            using var memoryStream = new MemoryStream(bytes);

            return (T)InternalFormatter.Deserialize(memoryStream);
        }
    }
}
