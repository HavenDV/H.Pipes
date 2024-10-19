using System.Diagnostics;
using System.IO.Pipes;
using H.Pipes;
using H.Pipes.AccessControl;
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
    /// <param name="pipeSecurity"></param>
    /// <exception cref="ArgumentNullException"></exception>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void EnableEncryption<T>(
        this IPipeServer<T> server,
        Action<Exception>? exceptionAction = null, 
        PipeSecurity? pipeSecurity = null)
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

                if (pipeSecurity != null) 
                {
                    server.SetPipeSecurity(pipeSecurity);
                }
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
