using H.Pipes.Encryption;

namespace H.Formatters;

/// <summary>
/// 
/// </summary>
public class InfernoFormatter : FormatterBase
{
    private IFormatter Formatter { get; }

    /// <summary>
    /// 
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

    /// <summary>
    /// Encrypts and serializes using any <see cref="FormatterBase"/>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override byte[] SerializeInternal(object obj)
    {
        var bytes = Formatter.Serialize(obj);
        if (Key == null)
        {
            return bytes;
        }

        return Encryption.EncryptMessage(Key, bytes);
    }

    /// <summary>
    /// Decrypts and deserializes using <see cref="FormatterBase"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public override T DeserializeInternal<T>(byte[] bytes)
    {
        bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));

        var message = Key != null
            ? Encryption.DecryptMessage(Key, bytes)
            : bytes;

        return Formatter.Deserialize<T>(message)!;
    }
}
