namespace H.Pipes.Formatters
{
    public interface IFormatter
    {
        byte[] Serialize(object obj);
        T Deserialize<T>(byte[] bytes);
    }
}
