using H.Formatters;

namespace H.Pipes.Extensions;

/// <summary>
/// Class FormatterExtensions.
/// </summary>
public static class FormatterExtensions
{
    #region Methods

    /// <summary>
    ///     Uses the <paramref name="formatter" /> to serialize the given object into a byte
    ///     array.
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="value">The object instance.</param>
    /// <param name="formatter">The formatter</param>
    /// <param name="cancellationToken">
    ///     The cancellation token that can be used by other objects or
    ///     threads to receive notice of cancellation.
    /// </param>
    /// <returns>Serialized object.</returns>
    public static async Task<byte[]> SerializeAsync<T>(
        this T            value,
        IFormatter        formatter,
        CancellationToken cancellationToken)
    {
        if (formatter == null)
            throw new ArgumentNullException(nameof(formatter));

        return formatter is IAsyncFormatter asyncFormatter
            ? await asyncFormatter.SerializeAsync(value, cancellationToken).ConfigureAwait(false)
            : formatter.Serialize(value);
    }

    /// <summary>Deserializes the bytes into the specified type using <paramref name="formatter" />.</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes">The bytes.</param>
    /// <param name="formatter">The formatter.</param>
    /// <param name="cancellationToken">
    ///     The cancellation token that can be used by other objects or
    ///     threads to receive notice of cancellation.
    /// </param>
    /// <returns>System.Nullable&lt;T&gt;.</returns>
    public static async Task<T?> DeserializeAsync<T>(
        this byte[]       bytes,
        IFormatter        formatter,
        CancellationToken cancellationToken)
    {
        if (formatter == null)
            throw new ArgumentNullException(nameof(formatter));

        return formatter is IAsyncFormatter asyncFormatter
            ? await asyncFormatter.DeserializeAsync<T>(bytes, cancellationToken).ConfigureAwait(false)
            : formatter.Deserialize<T>(bytes);
    }

    #endregion
}
