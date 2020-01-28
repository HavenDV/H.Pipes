using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                return Task.FromResult(Array.Empty<byte>());
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
        public Task<T> DeserializeAsync<T>(byte[]? bytes, CancellationToken cancellationToken = default)
        {
            if (bytes == null || !bytes.Any())
            {
#pragma warning disable CS8653 // A default expression introduces a null value for a type parameter.
                return Task.FromResult<T>(default);
#pragma warning restore CS8653 // A default expression introduces a null value for a type parameter.
            }

            using var memoryStream = new MemoryStream(bytes);
            var obj = (T)Serializer.Deserialize(memoryStream);

            return Task.FromResult(obj);
        }
    }
}
