using H.Formatters.Utilities;

namespace H.Formatters;

/// <summary>
/// A base async formatter class.
/// </summary>
public abstract class AsyncFormatterBase : FormatterBase, IAsyncFormatter
{
    protected abstract Task<byte[]> SerializeInternalAsync(object? obj, CancellationToken cancellationToken = default);

    protected abstract Task<T?> DeserializeInternalAsync<T>(byte[]? bytes, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public async Task<byte[]> SerializeAsync(object? obj, CancellationToken cancellationToken = default)
    {
        if (obj == null)
        {
            return ArrayUtilities.Empty<byte>();
        }

        return await SerializeInternalAsync(obj, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<T?> DeserializeAsync<T>(byte[]? bytes, CancellationToken cancellationToken = default)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return default;
        }

        return await DeserializeInternalAsync<T?>(bytes, cancellationToken).ConfigureAwait(false);
    }
}
