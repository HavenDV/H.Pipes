namespace H.Pipes.Args;

/// <summary>
/// Handles exceptions thrown during read/write operations.
/// </summary>
public class ConnectionExceptionEventArgs : ConnectionEventArgs
{
    /// <summary>
    /// The exception that was thrown
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="exception"></param>
    public ConnectionExceptionEventArgs(PipeConnection connection, Exception exception) : base(connection)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }
}
