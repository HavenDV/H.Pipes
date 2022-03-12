using H.Pipes;
using H.Pipes.Extensions;
using System.Diagnostics;

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
        server.ClientConnected += async (_, args) =>
        {
            try
            {
                using var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var cancellationToken = source.Token;

                var pipeName = $"{args.Connection.PipeName}_Inferno";
                var server = new SingleConnectionPipeServer<byte[]>(pipeName, args.Connection.Formatter);
                server.ExceptionOccurred += (_, args) =>
                {
                    Debug.WriteLine($"{nameof(EnableEncryption)} server returns exception: {args.Exception}");

                    exceptionAction?.Invoke(args.Exception);
                };
                await using (server.ConfigureAwait(false))
                {
                    await server.StartAsync(cancellationToken).ConfigureAwait(false);

                    var response = await server.WaitMessageAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    var clientPublicKey = response.Message;

                    using var keyPair = new KeyPair();

                    args.Connection.Formatter = new InfernoFormatter(
                        args.Connection.Formatter,
                        keyPair.GenerateSharedKey(clientPublicKey));

                    await server.WriteAsync(keyPair.PublicKey, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"{nameof(EnableEncryption)} returns exception: {exception}");

                await args.Connection.StopAsync().ConfigureAwait(false);

                exceptionAction?.Invoke(exception);
            }
        };
    }
}
