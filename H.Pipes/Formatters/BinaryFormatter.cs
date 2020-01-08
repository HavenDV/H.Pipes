using System.IO;

namespace H.Pipes.Formatters
{
    /// <summary>
    /// A formatter that uses <see cref="System.Runtime.Serialization.Formatters.Binary.BinaryFormatter"/> inside for serialization/deserialization
    /// </summary>
    public class BinaryFormatter : IFormatter
    {
        private System.Runtime.Serialization.Formatters.Binary.BinaryFormatter InternalFormatter { get; } = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

        /// <summary>
        /// Serializes using <see cref="System.Runtime.Serialization.Formatters.Binary.BinaryFormatter"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public byte[] Serialize(object obj)
        {
            using var stream = new MemoryStream();
            InternalFormatter.Serialize(stream, obj);

            return stream.ToArray();
        }

        /// <summary>
        /// Deserializes using <see cref="System.Runtime.Serialization.Formatters.Binary.BinaryFormatter"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public T Deserialize<T>(byte[] bytes)
        {
            using var memoryStream = new MemoryStream(bytes);

            return (T)InternalFormatter.Deserialize(memoryStream);
        }
    }
}
