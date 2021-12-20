namespace H.Formatters;

internal class InfernoFormatter : FormatterBase
{
    public IFormatter Formatter { get; }

    public byte[] Key { get; set; }

    public InfernoFormatter(IFormatter formatter, byte[] key)
    {
        Formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        Key = key ?? throw new ArgumentNullException(nameof(key));
    }

    protected override byte[] SerializeInternal(object obj)
    {
        var bytes = Formatter.Serialize(obj);

        return Encryption.EncryptMessage(Key, bytes);
    }

    protected override T DeserializeInternal<T>(byte[] bytes)
    {
        bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));

        var message = Encryption.DecryptMessage(Key, bytes);

        return Formatter.Deserialize<T>(message)!;
    }
}
