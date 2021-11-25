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

    /// <summary>
    /// Serializes using <see cref="JsonConvert"/>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override byte[] SerializeInternal(object obj)
    {
        var json = JsonConvert.SerializeObject(obj);
        var bytes = Encoding.GetBytes(json);

        return bytes;
    }

    /// <summary>
    /// Deserializes using <see cref="JsonConvert"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public override T DeserializeInternal<T>(byte[] bytes)
    {
        var json = Encoding.GetString(bytes);
        var obj = JsonConvert.DeserializeObject<T>(json);

        return obj;
    }
}
