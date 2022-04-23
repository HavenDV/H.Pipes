using System.Diagnostics;
using H.Pipes;
using H.Pipes.Extensions;

namespace H.Formatters;

/// <summary>
/// Encryption <see cref="IPipeServer{T}"/> extensions.
/// </summary>
public static class PipeServerExtensions
{
    /// <summary>
    /// Enables encryption using <see cref="InfernoFormatter"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="server"></param>
    /// <param name="exceptionAction"></param>
    /// <exception cref="ArgumentNullException"></exception>
#if NET5_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#elif NETSTANDARD2_0_OR_GREATER || NET461_OR_GREATER
#else
#error Target Framework is not supported
#endif
    public static void EnableEncryption<T>(
        this IPipeServer<T> server,
        Action<Exception>? exceptionAction = null)
    {
        server = server ?? throw new ArgumentNullException(nameof(server));
        server.ClientConnected += async (_, connArgs) =>
        {
            try
            {
                using var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var cancellationToken = source.Token;

                var pipeName = $"{connArgs.Connection.PipeName}_Inferno";
                var infServer = new SingleConnectionPipeServer(pipeName, connArgs.Connection.Formatter);

                infServer.ExceptionOccurred += (_, exArgs) =>
                {
                    Debug.WriteLine($"{nameof(EnableEncryption)} server returns exception: {exArgs.Exception}");

                    exceptionAction?.Invoke(exArgs.Exception);
                };

                await using (infServer.ConfigureAwait(false))
                {
                    await infServer.StartAsync(cancellationToken).ConfigureAwait(false);

                    var response = await infServer.WaitMessageAsync<byte[]>(cancellationToken: cancellationToken).ConfigureAwait(false);
                    var clientPublicKey = response.Message;

                    using var keyPair = new KeyPair();

                    connArgs.Connection.Formatter = new InfernoFormatter(
                        connArgs.Connection.Formatter,
                        keyPair.GenerateSharedKey(clientPublicKey));

                    await infServer.WriteAsync(keyPair.PublicKey, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"{nameof(EnableEncryption)} returns exception: {exception}");

                await connArgs.Connection.StopAsync().ConfigureAwait(false);

                exceptionAction?.Invoke(exception);
            }
        };
    }
}
