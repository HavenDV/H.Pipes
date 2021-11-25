using Ceras;

namespace H.Formatters;

/// <summary>
/// A formatter that uses <see cref="CerasSerializer"/> inside for serialization/deserialization
/// </summary>
public class CerasFormatter : FormatterBase
{
    private CerasSerializer InternalFormatter { get; } = new CerasSerializer();

    /// <summary>
    /// Serializes using <see cref="CerasSerializer"/>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override byte[] SerializeInternal(object obj)
    {
        return InternalFormatter.Serialize(obj);
    }

    /// <summary>
    /// Deserializes using <see cref="CerasSerializer"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public override T DeserializeInternal<T>(byte[] bytes)
    {
        return InternalFormatter.Deserialize<T>(bytes);
    }
}
