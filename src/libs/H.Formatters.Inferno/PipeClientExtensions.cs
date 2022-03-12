using H.Pipes;
using H.Pipes.Extensions;
using System.Diagnostics;

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
        client.Connected += async (o, args) =>
        {
            try
            {
                var pipeName = $"{args.Connection.PipeName}_Inferno";

                using var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var cancellationToken = source.Token;

                var client = new SingleConnectionPipeClient<byte[]>(pipeName, args.Connection.ServerName, formatter: args.Connection.Formatter);
                client.ExceptionOccurred += (_, args) =>
                {
                    Debug.WriteLine($"{nameof(EnableEncryption)} client returns exception: {args.Exception}");

                    exceptionAction?.Invoke(args.Exception);
                };
                await using (client.ConfigureAwait(false))
                {
                    using var _keyPair = new KeyPair();
                    await client.WriteAsync(_keyPair.PublicKey, cancellationToken).ConfigureAwait(false);

                    var response = await client.WaitMessageAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    var serverPublicKey = response.Message;

                    args.Connection.Formatter = new InfernoFormatter(
                        args.Connection.Formatter,
                        _keyPair.GenerateSharedKey(serverPublicKey));
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"{nameof(EnableEncryption)} returns exception: {exception}");

                await client.DisconnectAsync().ConfigureAwait(false);

                exceptionAction?.Invoke(exception);
            }
        };
        client.Disconnected += (o, args) =>
        {
            if (args.Connection.Formatter is not InfernoFormatter infernoFormatter)
            {
                return;
            }

            args.Connection.Formatter = infernoFormatter.Formatter;
        };
    }
}
