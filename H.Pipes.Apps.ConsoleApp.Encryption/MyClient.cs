// ReSharper disable AccessToDisposedClosure

using H.Formatters;
using H.Pipes.Encryption;
using System.Linq;

namespace H.Pipes.Apps.ConsoleApp.Encryption;

internal static class MyClient
{
    private static readonly CryptoFormatter _formatter = new(new SystemTextJsonFormatter());
    private static KeyPair? _keyPair;
    private static void OnExceptionOccurred(Exception exception)
    {
        Console.Error.WriteLine($"Exception: {exception}");
    }

    public static async Task RunAsync(string pipeName)
    {
        try
        {
            using var source = new CancellationTokenSource();

            Console.WriteLine($"Running in CLIENT mode. PipeName: {pipeName}");
            Console.WriteLine("Enter 'q' to exit");

            await using var client = new PipeClient<MyMessage>(pipeName, formatter: _formatter);
            client.Connected += (o, args) =>
            {
                Console.WriteLine("Connected to server");
                _keyPair = new KeyPair();
                Task.Run(async () => await client.WriteAsync(new MyMessage
                {
                    Text = _keyPair.PublicKey,
                }, source.Token).ConfigureAwait(false));
                Console.WriteLine("Sent the public key");
            };

            client.MessageReceived += (o, args) =>
            {
                if(args.Message?.Text == null)
                {
                    return;
                }

                if (_formatter.Key == null)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (_keyPair.ValidatePublicKey(args.Message.Text, out var serverPublicKey))
                    {
                        Console.WriteLine("Received server public key");
#pragma warning disable CS8604 // Possible null reference argument.
                        _formatter.Key = _keyPair.GenerateSharedKey(serverPublicKey);
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                    else
                    {
                        // Do nothing
                    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                }
                else
                {
                    Console.WriteLine("Message Received: " + args.Message);
                }
            };
            client.Disconnected += (o, args) =>
            {
                Console.WriteLine("Disconnected from server");
                _formatter.Key = null;
            };

            client.ExceptionOccurred += (o, args) => OnExceptionOccurred(args.Exception);

            // Dispose is not required
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

                        await client.WriteAsync(new MyMessage
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

            Console.WriteLine("Client connecting...");

            await client.ConnectAsync(source.Token).ConfigureAwait(false);

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
