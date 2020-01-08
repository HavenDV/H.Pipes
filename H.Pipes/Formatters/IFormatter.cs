namespace H.Pipes.Formatters
{
    /// <summary>
    /// A formatter interface for serialization/deserialization
    /// </summary>
    public interface IFormatter
    {
        /// <summary>
        /// Serializes to bytes
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        byte[] Serialize(object obj);

        /// <summary>
        /// Deserializes from bytes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        T Deserialize<T>(byte[] bytes);
    }
}
