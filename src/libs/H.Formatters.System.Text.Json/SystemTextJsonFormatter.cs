using System.Text;
using System.Text.Json;

namespace H.Formatters;

/// <summary>
/// A formatter that uses <see cref="JsonSerializer"/> inside for serialization/deserialization
/// </summary>
public class SystemTextJsonFormatter : FormatterBase
{
    /// <summary>
    /// Default: UTF8.
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <summary>
    /// Serializes using <see cref="JsonSerializer"/>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override byte[] SerializeInternal(object? obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.GetBytes(json);

        return bytes;
    }

    /// <summary>
    /// Deserializes using <see cref="JsonSerializer"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public override T DeserializeInternal<T>(byte[] bytes)
    {
        var json = Encoding.GetString(bytes);
        var obj = JsonSerializer.Deserialize<T>(json);

        return obj ?? throw new InvalidOperationException("obj is null.");
    }
}
