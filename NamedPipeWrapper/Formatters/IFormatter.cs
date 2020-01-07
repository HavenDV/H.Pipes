namespace NamedPipeWrapper.Formatters
{
    public interface IFormatter
    {
        byte[] Serialize(object obj);
        T Deserialize<T>(byte[] bytes);
    }
}
