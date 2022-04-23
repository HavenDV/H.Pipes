using System.Diagnostics;
using H.Pipes;
using H.Pipes.Extensions;

namespace H.Formatters;

/// <summary>
/// Encryption <see cref="IPipeClient{T}"/> extensions.
/// </summary>
public static class PipeClientExtensions
{
    /// <summary>
    /// Enables encryption using <see cref="InfernoFormatter"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="client"></param>
    /// <param name="exceptionAction"></param>
    /// <exception cref="ArgumentNullException"></exception>
#if NET5_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#elif NETSTANDARD2_0_OR_GREATER || NET461_OR_GREATER
#else
#error Target Framework is not supported
#endif
    public static void EnableEncryption<T>(
        this IPipeClient<T> client,
        Action<Exception>? exceptionAction = null)
    {
        client = client ?? throw new ArgumentNullException(nameof(client));
        client.Connected += async (_, connArgs) =>
        {
            try
            {
                var pipeName = $"{connArgs.Connection.PipeName}_Inferno";

                using var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var cancellationToken = source.Token;

                var infClient = new SingleConnectionPipeClient(pipeName, connArgs.Connection.ServerName, formatter: connArgs.Connection.Formatter);

                infClient.ExceptionOccurred += (_, exArgs) =>
                {
                    Debug.WriteLine($"{nameof(EnableEncryption)} client returns exception: {exArgs.Exception}");

                    exceptionAction?.Invoke(exArgs.Exception);
                };

                await using (infClient.ConfigureAwait(false))
                {
                    using var keyPair = new KeyPair();
                    await infClient.WriteAsync(keyPair.PublicKey, cancellationToken).ConfigureAwait(false);

                    var response = await infClient.WaitMessageAsync<byte[]>(cancellationToken: cancellationToken).ConfigureAwait(false);
                    var serverPublicKey = response.Message;

                    connArgs.Connection.Formatter = new InfernoFormatter(
                        connArgs.Connection.Formatter,
                        keyPair.GenerateSharedKey(serverPublicKey));
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"{nameof(EnableEncryption)} returns exception: {exception}");

                await client.DisconnectAsync().ConfigureAwait(false);

                exceptionAction?.Invoke(exception);
            }
        };
        client.Disconnected += (_, args) =>
        {
            if (args.Connection.Formatter is not InfernoFormatter infernoFormatter)
            {
                return;
            }

            args.Connection.Formatter = infernoFormatter.Formatter;
        };
    }
}
