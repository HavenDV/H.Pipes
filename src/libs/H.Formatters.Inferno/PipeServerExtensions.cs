using H.Pipes;
using H.Pipes.Extensions;
using System.Diagnostics;

namespace H.Formatters;

/// <summary>
/// Encryption <see cref="IPipeServer{T}"/> extensions.
/// </summary>
public static class PipeServerExtensions
{
    public static void EnableEncryption<T>(
        this IPipeServer<T> server)
    {
        server = server ?? throw new ArgumentNullException(nameof(server));
        server.ClientConnected += async (_, args) =>
        {
            if (server.Formatter is not InfernoFormatter formatter)
            {
                return;
            }

            try
            {
                using var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var cancellationToken = source.Token;

                var pipeName = $"{args.Connection.PipeName}_Inferno";
                var server = new SingleConnectionPipeServer<string>(pipeName);
                await using (server.ConfigureAwait(false))
                {
                    await server.StartAsync(cancellationToken).ConfigureAwait(false);

                    var response = await server.WaitMessageAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    var clientPublicKey = KeyPair.ValidatePublicKey(response.Message);

                    var keyPair = new KeyPair();
                    formatter.Key = keyPair.GenerateSharedKey(clientPublicKey);

                    await server.WriteAsync(keyPair.PublicKey, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"{nameof(EnableEncryption)} returns exception: {exception}");
            }
        };
    }
}
