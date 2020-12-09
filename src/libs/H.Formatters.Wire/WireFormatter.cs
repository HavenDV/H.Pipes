using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using H.Formatters.Utilities;
using Wire;

namespace H.Formatters
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
        public Task<byte[]> SerializeAsync(object? obj, CancellationToken cancellationToken = default)
        {
            if (obj == null)
            {
                return Task.FromResult(ArrayUtilities.Empty<byte>());
            }

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
        public Task<T?> DeserializeAsync<T>(byte[]? bytes, CancellationToken cancellationToken = default)
        {
            if (bytes == null || !bytes.Any())
            {
                return Task.FromResult<T?>(default);
            }

            using var memoryStream = new MemoryStream(bytes);
            var obj = (T?)Serializer.Deserialize(memoryStream);

            return Task.FromResult(obj);
        }
    }
}
