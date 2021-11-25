using H.Formatters.Utilities;

namespace H.Formatters;

/// <summary>
/// A base formatter class.
/// </summary>
public abstract class AsyncFormatterBase : FormatterBase, IAsyncFormatter
{
    /// <summary>
    /// Serializes to bytes.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public abstract Task<byte[]> SerializeInternalAsync(object? obj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes from bytes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public abstract Task<T?> DeserializeInternalAsync<T>(byte[]? bytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<byte[]> SerializeAsync(object? obj, CancellationToken cancellationToken = default)
    {
        if (obj == null)
        {
            return ArrayUtilities.Empty<byte>();
        }

        return await SerializeInternalAsync(obj, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deserializes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<T?> DeserializeAsync<T>(byte[]? bytes, CancellationToken cancellationToken = default)
    {
        if (bytes == null || !bytes.Any())
        {
            return default;
        }

        return await DeserializeInternalAsync<T?>(bytes, cancellationToken).ConfigureAwait(false);
    }
}
