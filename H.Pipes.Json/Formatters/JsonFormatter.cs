using System.Text;
using Newtonsoft.Json;

namespace H.Pipes.Formatters
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
        /// <returns></returns>
        public byte[] Serialize(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);

            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Deserializes using <see cref="JsonConvert"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public T Deserialize<T>(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes);

            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
