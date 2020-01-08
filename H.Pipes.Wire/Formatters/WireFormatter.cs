using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<byte[]> SerializeAsync(object obj, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            Serializer.Serialize(obj, stream);
            var bytes = stream.ToArray();

            return Task.FromResult(bytes);
        }

        /// <summary>
        /// Deserializes using <see langword="Wire"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<T> DeserializeAsync<T>(byte[] bytes, CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream(bytes);
            var obj = (T)Serializer.Deserialize(memoryStream);

            return Task.FromResult(obj);
        }
    }
}
