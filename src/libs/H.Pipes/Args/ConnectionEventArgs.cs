namespace H.Pipes.Args;

/// <summary>
/// Handles new connections.
/// </summary>
public class ConnectionEventArgs : EventArgs
{
    /// <summary>
    /// Connection
    /// </summary>
    public PipeConnection Connection { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connection"></param>
    public ConnectionEventArgs(PipeConnection connection)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
}
