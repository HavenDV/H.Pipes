using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace H.Formatters;

/// <summary>
/// A formatter that uses <see cref="JsonSerializer"/> inside for serialization/deserialization
/// </summary>
public class SystemTextJsonNativeAotFormatter(JsonSerializerContext context) : FormatterBase
{
    /// <summary>
    /// Default: UTF8.
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <summary>
    /// Set this to enable trimming/NativeAOT support.
    /// </summary>
    public JsonSerializerContext Context { get; set; } = context;

    protected override byte[] SerializeInternal(object? obj)
    {
        if (obj == null)
        {
            return [];
        }
        
        var json = JsonSerializer.Serialize(obj, obj.GetType(), Context);
        var bytes = Encoding.GetBytes(json);

        return bytes;
    }

    protected override T DeserializeInternal<T>(byte[] bytes)
    {
        var json = Encoding.GetString(bytes);
        var obj = (T?)JsonSerializer.Deserialize(json, typeof(T), Context);

        return obj ?? throw new InvalidOperationException("obj is null.");
    }
}
