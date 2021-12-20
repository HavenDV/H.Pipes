using H.Pipes;
using H.Pipes.Encryption;
using H.Pipes.Extensions;
using System.Diagnostics;

namespace H.Formatters;

/// <summary>
/// Encryption <see cref="IPipeClient{T}"/> extensions.
/// </summary>
public static class PipeClientExtensions
{
    private static KeyPair? _keyPair;

    public static void EnableEncryption<T>(
        this IPipeClient<T> client)
    {
        client = client ?? throw new ArgumentNullException(nameof(client));
        client.Connected += async (o, args) =>
        {
            if (client.Formatter is not InfernoFormatter formatter)
            {
                return;
            }

            try
            {
                var pipeName = $"{args.Connection.PipeName}_Inferno";
                _keyPair = new KeyPair();

                using var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var cancellationToken = source.Token;

                var client = new SingleConnectionPipeClient<string>(pipeName);
                await client.WriteAsync(_keyPair.PublicKey, cancellationToken).ConfigureAwait(false);

                var response = await client.WaitMessageAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                var serverPublicKey = KeyPair.ValidatePublicKey(response.Message);

                formatter.Key = _keyPair.GenerateSharedKey(serverPublicKey);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"{nameof(EnableEncryption)} returns exception: {exception}");
            }
        };
        client.Disconnected += (o, args) =>
        {
            if (client.Formatter is not InfernoFormatter formatter)
            {
                return;
            }

            formatter.Key = null;
        };
    }
}
