using MessagePack;

namespace H.Formatters
{
    /// <summary>
    /// A formatter that uses <see cref="MessagePackSerializer"/> inside for serialization/deserialization
    /// </summary>
    public class MessagePackFormatter : FormatterBase
    {
        /// <summary>
        /// Serializes using <see cref="MessagePackSerializer"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override byte[] SerializeInternal(object obj)
        {
            return MessagePackSerializer.Serialize(obj);
        }

        /// <summary>
        /// Deserializes using <see cref="MessagePackSerializer"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public override T DeserializeInternal<T>(byte[] bytes)
        {
            return MessagePackSerializer.Deserialize<T>(bytes);
        }
    }
}
