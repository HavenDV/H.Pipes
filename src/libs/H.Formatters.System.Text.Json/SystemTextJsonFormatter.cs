using System.Text;
using System.Text.Json;

namespace H.Formatters;

/// <summary>
/// A formatter that uses <see cref="JsonSerializer"/> inside for serialization/deserialization
/// </summary>
#if NET6_0_OR_GREATER
[System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
[System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
#endif
public class SystemTextJsonFormatter : FormatterBase
{
    /// <summary>
    /// Default: UTF8.
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <summary>
    /// Default: default JsonSerializerOptions.
    /// </summary>
    public JsonSerializerOptions Options { get; set; } = new();

    protected override byte[] SerializeInternal(object? obj)
    {
        var json = JsonSerializer.Serialize(obj, Options);
        var bytes = Encoding.GetBytes(json);

        return bytes;
    }

    protected override T DeserializeInternal<T>(byte[] bytes)
    {
        var json = Encoding.GetString(bytes);
        var obj = JsonSerializer.Deserialize<T>(json, Options);

        return obj ?? throw new InvalidOperationException("obj is null.");
    }
}
