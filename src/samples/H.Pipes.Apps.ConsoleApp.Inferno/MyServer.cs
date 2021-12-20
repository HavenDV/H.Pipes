// ReSharper disable AccessToDisposedClosure

using H.Formatters;
using H.Pipes.Encryption;

namespace H.Pipes.Apps.ConsoleApp.Encryption;

internal static class MyServer
{
    private static readonly InfernoFormatter _formatter = new(new SystemTextJsonFormatter());
    private static readonly Dictionary<string, KeyPair> _pipeKeyStore = new();

    private static void OnExceptionOccurred(Exception exception)
    {
        Console.Error.WriteLine($"Exception: {exception}");
    }

    public static async Task RunAsync(string pipeName)
    {
        try
        {
            using var source = new CancellationTokenSource();

            Console.WriteLine($"Running in SERVER mode. PipeName: {pipeName}");
            Console.WriteLine("Enter 'q' to exit");

            await using var server = new PipeServer<MyMessage>(pipeName, formatter: _formatter);
            server.ClientConnected += async (_, args) =>
            {
                Console.WriteLine($"Client {args.Connection.PipeName} is now connected!");

                try
                {
                    if (!_pipeKeyStore.ContainsKey(args.Connection.PipeName))
                    {
                        _pipeKeyStore.Add(args.Connection.PipeName, new KeyPair());
                    }
                    await args.Connection.WriteAsync(new MyMessage
                    {
                        Text = _pipeKeyStore[args.Connection.PipeName].PublicKey
                    }, source.Token).ConfigureAwait(false);
                    Console.WriteLine("Sent the public key");
                }
                catch (Exception exception)
                {
                    OnExceptionOccurred(exception);
                }
            };
            server.MessageReceived += (_, args) =>
            {
                if (args.Message?.Text == null)
                {
                    return;
                }

                if (_formatter.Key == null)
                {
                    if (_pipeKeyStore[args.Connection.PipeName].ValidatePublicKey(args.Message.Text, out var clientPublicKey))
                    {
                        Console.WriteLine($"Received client {args.Connection.PipeName} public key");
#pragma warning disable CS8604 // Possible null reference argument.
                        _formatter.Key = _pipeKeyStore[args.Connection.PipeName].GenerateSharedKey(clientPublicKey);
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                    else
                    {
                        // Do nothing
                    }
                }
                else
                {
                    Console.WriteLine($"Client {args.Connection.PipeName} says: {args.Message}");
                }
            };

            server.ClientDisconnected += (_, args) =>
            {
                Console.WriteLine($"Client {args.Connection.PipeName} disconnected");
                _pipeKeyStore.Remove(args.Connection.PipeName);
            };

            server.ExceptionOccurred += (_, args) => OnExceptionOccurred(args.Exception);

            var _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var message = await Console.In.ReadLineAsync().ConfigureAwait(false);
                        if (message == "q")
                        {
                            source.Cancel();
                            break;
                        }

                        Console.WriteLine($"Sent to {server.ConnectedClients.Count} clients");

                        await server.WriteAsync(new MyMessage
                        {
                            Text = message,
                        }, source.Token).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        OnExceptionOccurred(exception);
                    }
                }
            }, source.Token);

            Console.WriteLine("Server starting...");

            await server.StartAsync(cancellationToken: source.Token).ConfigureAwait(false);

            Console.WriteLine("Server is started!");

            await Task.Delay(Timeout.InfiniteTimeSpan, source.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            OnExceptionOccurred(exception);
        }
    }
}
