namespace H.Pipes.Args;

/// <summary>
/// Handles new connections.
/// </summary>
public class ConnectionEventArgs : EventArgs
{
    /// <summary>
    /// Connection.
    /// </summary>
    public PipeConnection Connection { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionEventArgs"/> class.
    /// </summary>
    /// <param name="connection"></param>
    public ConnectionEventArgs(PipeConnection connection)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
}

/// <summary>
/// Handles new connections.
/// </summary>
/// <typeparam name="T">Reference type.</typeparam>
public class ConnectionEventArgs<T> : ConnectionEventArgs
{
    /// <summary>
    /// Connection.
    /// </summary>
    public new PipeConnection<T> Connection { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionEventArgs{T}"/> class.
    /// </summary>
    /// <param name="connection"></param>
    public ConnectionEventArgs(PipeConnection<T> connection) : base(connection)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
}
