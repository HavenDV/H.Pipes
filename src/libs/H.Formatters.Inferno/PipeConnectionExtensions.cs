using H.Pipes;

namespace H.Formatters;

/// <summary>
/// Encryption <see cref="PipeConnection{T}"/> extensions.
/// </summary>
public static class PipeConnectionExtensions
{
    /// <summary>
    /// Waits key exchange.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task WaitExchangeAsync(
        this IPipeConnection connection,
        CancellationToken cancellationToken = default)
    {
        connection = connection ?? throw new ArgumentNullException(nameof(connection));

        while (connection.Formatter is not InfernoFormatter)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);
        }
    }
}
