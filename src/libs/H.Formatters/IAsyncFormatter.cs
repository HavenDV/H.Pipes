namespace H.Formatters;

/// <summary>
/// A async formatter interface for serialization/deserialization
/// </summary>
public interface IAsyncFormatter : IFormatter
{
    /// <summary>
    /// Serializes to bytes.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<byte[]> SerializeAsync(object? obj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes from bytes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<T?> DeserializeAsync<T>(byte[]? bytes, CancellationToken cancellationToken = default);
}
