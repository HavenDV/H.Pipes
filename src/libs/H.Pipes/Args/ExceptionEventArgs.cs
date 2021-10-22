namespace H.Pipes.Args;

/// <summary>
/// 
/// </summary>
public class ExceptionEventArgs : EventArgs
{
    /// <summary>
    /// 
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="exception"></param>
    public ExceptionEventArgs(Exception exception)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }
}
