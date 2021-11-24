using System.Text;
using H.Formatters.Utilities;
using System.Text.Json;

namespace H.Formatters;

/// <summary>
/// A formatter that uses <see cref="JsonSerializer"/> inside for serialization/deserialization
/// </summary>
public class SystemTextJsonFormatter : IFormatter
{
    /// <summary>
    /// Serializes using <see cref="JsonSerializer"/>
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

        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);

        return Task.FromResult(bytes);
    }

    /// <summary>
    /// Deserializes using <see cref="JsonSerializer"/>
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

        var json = Encoding.UTF8.GetString(bytes);
        var obj = JsonSerializer.Deserialize<T?>(json);

        return Task.FromResult(obj);
    }
}
