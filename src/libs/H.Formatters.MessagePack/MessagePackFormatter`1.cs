using MessagePack;

namespace H.Formatters;

/// <summary>
/// A formatter that uses <see cref="MessagePackSerializer"/> inside for serialization/deserialization. <br/>
/// This formatter is needed to avoid the following error: <br/>
/// https://github.com/HavenDV/H.Pipes/issues/48
/// </summary>
public class MessagePackFormatter<T1> : FormatterBase
{
    protected override byte[] SerializeInternal(object obj)
    {
        return MessagePackSerializer.Serialize<T1>((T1)obj);
    }

    protected override T DeserializeInternal<T>(byte[] bytes)
    {
        return MessagePackSerializer.Deserialize<T>(bytes);
    }
}
