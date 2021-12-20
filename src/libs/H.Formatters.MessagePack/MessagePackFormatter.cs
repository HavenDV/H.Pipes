using MessagePack;

namespace H.Formatters;

/// <summary>
/// A formatter that uses <see cref="MessagePackSerializer"/> inside for serialization/deserialization
/// </summary>
public class MessagePackFormatter : FormatterBase
{
    protected override byte[] SerializeInternal(object obj)
    {
        return MessagePackSerializer.Serialize(obj);
    }

    protected override T DeserializeInternal<T>(byte[] bytes)
    {
        return MessagePackSerializer.Deserialize<T>(bytes);
    }
}
