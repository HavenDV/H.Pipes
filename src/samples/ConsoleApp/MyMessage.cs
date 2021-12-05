namespace ConsoleApp;

[Serializable]
internal class MyMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Text { get; set; }

    public override string ToString()
    {
        return $"\"{Text}\" (message ID = {Id})";
    }
}
