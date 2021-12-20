﻿namespace H.Formatters;

/// <summary>
/// A formatter that uses <see cref="System.Runtime.Serialization.Formatters.Binary.BinaryFormatter"/> inside for serialization/deserialization
/// </summary>
public class BinaryFormatter : FormatterBase
{
    private System.Runtime.Serialization.Formatters.Binary.BinaryFormatter InternalFormatter { get; } = new();

    protected override byte[] SerializeInternal(object obj)
    {
        using var stream = new MemoryStream();
        InternalFormatter.Serialize(stream, obj);
        var bytes = stream.ToArray();

        return bytes;
    }

    protected override T DeserializeInternal<T>(byte[] bytes)
    {
        using var memoryStream = new MemoryStream(bytes);
        var obj = (T)InternalFormatter.Deserialize(memoryStream);

        return obj;
    }
}
