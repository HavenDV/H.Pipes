using H.Formatters.Utilities;

namespace H.Formatters;

/// <summary>
/// A base formatter class.
/// </summary>
public abstract class FormatterBase : IFormatter
{
    /// <inheritdoc/>
    public FormatterContext Context { get; } = new();

    /// <summary>
    /// Serializes to bytes.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public virtual byte[] SerializeInternal(object obj)
    {
        return ArrayUtilities.Empty<byte>();
    }

    /// <summary>
    /// Deserializes from bytes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public virtual T? DeserializeInternal<T>(byte[] bytes)
    {
        return default;
    }

    /// <inheritdoc/>
    public byte[] Serialize(object? obj)
    {
        if (obj == null)
        {
            return ArrayUtilities.Empty<byte>();
        }

        return SerializeInternal(obj);
    }

    /// <inheritdoc/>
    public T? Deserialize<T>(byte[]? bytes)
    {
        if (bytes == null || !bytes.Any())
        {
            return default;
        }

        return DeserializeInternal<T?>(bytes);
    }
}
