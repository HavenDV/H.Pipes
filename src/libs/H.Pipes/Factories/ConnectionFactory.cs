using System.IO.Pipes;
using H.Formatters;

namespace H.Pipes.Factories;

/// <summary>
/// Internal usage
/// </summary>
public static class ConnectionFactory
{
    private static int LastId { get; set; }

    /// <summary>
    /// Internal usage
    /// </summary>
    public static PipeConnection<T> Create<T>(PipeStream pipeStream, IFormatter formatter)
    {
        return new PipeConnection<T>(++LastId, "Client " + LastId, pipeStream, formatter);
    }
}
