using System.Text;
using NamedPipeWrapper.Formatters;
using Newtonsoft.Json;

namespace H.Pipes.Formatters
{
    public class JsonFormatter : IFormatter
    {
        public byte[] Serialize(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);

            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes);

            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
