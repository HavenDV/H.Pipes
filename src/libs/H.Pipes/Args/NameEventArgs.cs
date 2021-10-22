namespace H.Pipes.Args;

/// <summary>
/// 
/// </summary>
public class NameEventArgs : EventArgs
{
    /// <summary>
    /// 
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    public NameEventArgs(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
