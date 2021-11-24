namespace H.Formatters.Utilities;

/// <summary>
/// Full Task support for net40.
/// <![CDATA[Version: 1.0.0.0]]> <br/>
/// </summary>
public static class TaskUtilities
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="result"></param>
    /// <returns></returns>
    public static Task<TResult> FromResult<TResult>(TResult result)
    {
#if NET40
        var source = new TaskCompletionSource<TResult>(result);
        source.TrySetResult(result);

        return source.Task;
#else
        return Task.FromResult(result);
#endif
    }
}
