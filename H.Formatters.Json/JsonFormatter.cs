using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace H.Formatters
{
    /// <summary>
    /// A formatter that uses <see cref="JsonConvert"/> inside for serialization/deserialization
    /// </summary>
    public class JsonFormatter : IFormatter
    {
        /// <summary>
        /// Serializes using <see cref="JsonConvert"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<byte[]> SerializeAsync(object obj, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(obj);
            var bytes = Encoding.UTF8.GetBytes(json);

            return Task.FromResult(bytes);
        }

        /// <summary>
        /// Deserializes using <see cref="JsonConvert"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<T> DeserializeAsync<T>(byte[] bytes, CancellationToken cancellationToken = default)
        {
            var json = Encoding.UTF8.GetString(bytes);
            var obj = JsonConvert.DeserializeObject<T>(json);

            return Task.FromResult(obj);
        }
    }
}
