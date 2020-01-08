using System.IO;
using Wire;

namespace H.Pipes.Formatters
{
    /// <summary>
    /// A formatter that uses <see langword="Wire"/> inside for serialization/deserialization
    /// </summary>
    public class WireFormatter : IFormatter
    {
        private Serializer Serializer { get; } = new Serializer();

        /// <summary>
        /// Serializes using <see langword="Wire"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public byte[] Serialize(object obj)
        {
            using var stream = new MemoryStream();
            Serializer.Serialize(obj, stream);

            return stream.ToArray();
        }

        /// <summary>
        /// Deserializes using <see langword="Wire"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public T Deserialize<T>(byte[] bytes)
        {
            using var memoryStream = new MemoryStream(bytes);

            return (T)Serializer.Deserialize(memoryStream);
        }
    }
}
