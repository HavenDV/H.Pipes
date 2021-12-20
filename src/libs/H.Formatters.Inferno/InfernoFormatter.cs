using H.Pipes.Encryption;

namespace H.Formatters;

/// <summary>
/// A formatter that uses <see cref="Encryption"/> inside for serialization/deserialization
/// </summary>
public class InfernoFormatter : FormatterBase
{
    private IFormatter Formatter { get; }

    /// <summary>
    /// Public key
    /// </summary>
    public byte[]? Key { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="formatter"></param>
    public InfernoFormatter(IFormatter formatter)
    {
        Formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    protected override byte[] SerializeInternal(object obj)
    {
        var bytes = Formatter.Serialize(obj);
        if (Key == null)
        {
            return bytes;
        }

        return Encryption.EncryptMessage(Key, bytes);
    }

    protected override T DeserializeInternal<T>(byte[] bytes)
    {
        bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));

        var message = Key != null
            ? Encryption.DecryptMessage(Key, bytes)
            : bytes;

        return Formatter.Deserialize<T>(message)!;
    }
}
