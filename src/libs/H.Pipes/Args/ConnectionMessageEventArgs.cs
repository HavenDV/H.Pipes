﻿namespace H.Pipes.Args;

/// <summary>
/// Handles messages received from a named pipe.
/// </summary>
/// <typeparam name="T">Reference type</typeparam>
public class ConnectionMessageEventArgs<T> : ConnectionEventArgs<T>
{
    /// <summary>
    /// Message sent by the other end of the pipe
    /// </summary>
    public T Message { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="message"></param>
    public ConnectionMessageEventArgs(PipeConnection<T> connection, T message) : base(connection)
    {
        Message = message;
    }
}
