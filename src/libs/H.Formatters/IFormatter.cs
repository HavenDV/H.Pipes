namespace H.Formatters;

/// <summary>
/// A formatter interface for serialization/deserialization
/// </summary>
public interface IFormatter
{
    /// <summary>
    /// Serializes to bytes
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<byte[]> SerializeAsync(object? obj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes from bytes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<T?> DeserializeAsync<T>(byte[]? bytes, CancellationToken cancellationToken = default);
}
