namespace H.Pipes.Args;

/// <summary>
/// Handles messages received from a named pipe.
/// </summary>
public class ConnectionMessageEventArgs : ConnectionEventArgs
{
    /// <summary>
    /// Message sent by the other end of the pipe.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "The non-generic pipe API exposes raw byte messages.")]
    public byte[]? Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionMessageEventArgs"/> class.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="message"></param>
    public ConnectionMessageEventArgs(PipeConnection connection, byte[]? message) : base(connection)
    {
        Message = message;
    }
}

/// <summary>
/// Handles messages received from a named pipe.
/// </summary>
/// <typeparam name="T">Reference type.</typeparam>
public class ConnectionMessageEventArgs<T> : ConnectionEventArgs<T>
{
    /// <summary>
    /// Message sent by the other end of the pipe.
    /// </summary>
    public T Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionMessageEventArgs{T}"/> class.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="message"></param>
    public ConnectionMessageEventArgs(PipeConnection<T> connection, T message) : base(connection)
    {
        Message = message;
    }
}
