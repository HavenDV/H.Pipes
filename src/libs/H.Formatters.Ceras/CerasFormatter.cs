using Ceras;
using H.Formatters.Utilities;

namespace H.Formatters;

/// <summary>
/// A formatter that uses <see cref="CerasSerializer"/> inside for serialization/deserialization
/// </summary>
public class CerasFormatter : IFormatter
{
    private CerasSerializer InternalFormatter { get; } = new CerasSerializer();

    /// <summary>
    /// Serializes using <see cref="CerasSerializer"/>
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

        var bytes = InternalFormatter.Serialize(obj);

        return Task.FromResult(bytes);
    }

    /// <summary>
    /// Deserializes using <see cref="CerasSerializer"/>
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

        var obj = InternalFormatter.Deserialize<T?>(bytes);

        return Task.FromResult(obj);
    }
}
