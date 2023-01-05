using System.Text;
using Newtonsoft.Json;

namespace H.Formatters;

/// <summary>
/// A formatter that uses <see cref="JsonConvert"/> inside for serialization/deserialization
/// </summary>
public class NewtonsoftJsonFormatter : FormatterBase
{
    /// <summary>
    /// Default: UTF8.
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    protected override byte[] SerializeInternal(object obj)
    {
        var json = JsonConvert.SerializeObject(obj);
        var bytes = Encoding.GetBytes(json);

        return bytes;
    }

    protected override T DeserializeInternal<T>(byte[] bytes)
    {
        var json = Encoding.GetString(bytes);
        var obj =
            JsonConvert.DeserializeObject<T>(json) ??
            throw new InvalidOperationException("Deserialized object is null.");

        return obj;
    }
}
