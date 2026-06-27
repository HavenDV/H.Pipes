namespace H.Pipes.Args;

/// <summary>
/// Handles exceptions thrown during read/write operations.
/// </summary>
public class ConnectionExceptionEventArgs : ConnectionEventArgs
{
    /// <summary>
    /// The exception that was thrown.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionExceptionEventArgs"/> class.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="exception"></param>
    public ConnectionExceptionEventArgs(PipeConnection connection, Exception exception) : base(connection)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }
}

/// <summary>
/// Handles exceptions thrown during read/write operations.
/// </summary>
/// <typeparam name="T">Reference type.</typeparam>
public class ConnectionExceptionEventArgs<T> : ConnectionEventArgs<T>
{
    /// <summary>
    /// The exception that was thrown.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionExceptionEventArgs{T}"/> class.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="exception"></param>
    public ConnectionExceptionEventArgs(PipeConnection<T> connection, Exception exception) : base(connection)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }
}
