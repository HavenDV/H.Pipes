using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<byte[]> SerializeAsync(object obj, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            InternalFormatter.Serialize(stream, obj);
            var bytes = stream.ToArray();

            return Task.FromResult(bytes);
        }

        /// <summary>
        /// Deserializes using <see cref="System.Runtime.Serialization.Formatters.Binary.BinaryFormatter"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<T> DeserializeAsync<T>(byte[] bytes, CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream(bytes);
            var obj = (T) InternalFormatter.Deserialize(memoryStream);

            return Task.FromResult(obj);
        }
    }
}
