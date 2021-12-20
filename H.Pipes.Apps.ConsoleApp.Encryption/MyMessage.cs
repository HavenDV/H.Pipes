namespace H.Pipes.Apps.ConsoleApp.Encryption;

[Serializable]
public class MyMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? Text { get; set; }

    public override string ToString()
    {
        return $"\"{Text}\" (message ID = {Id})";
    }
}
