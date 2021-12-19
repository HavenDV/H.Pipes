using MessagePack;

namespace H.Pipes.Apps.ConsoleApp.MessagePack;

[MessagePackObject]
[Serializable]
public class MyMessage
{
    [Key(0)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Key(1)]
    public string? Text { get; set; }

    public override string ToString()
    {
        return $"\"{Text}\" (message ID = {Id})";
    }
}
