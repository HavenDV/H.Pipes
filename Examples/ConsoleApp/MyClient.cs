using System;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes;

// ReSharper disable AccessToDisposedClosure

namespace ConsoleApp
{
    internal static class MyClient
    {
        private static void OnExceptionOccurred(Exception exception)
        {
            Console.Error.WriteLine($"Exception: {exception}");
        }

        public static async Task RunAsync(string pipeName)
        {
            try
            {
                using var source = new CancellationTokenSource();

                Console.WriteLine("Running in CLIENT mode");
                Console.WriteLine("Enter 'q' to exit");

                await using var client = new PipeClient<MyMessage>(pipeName);
                client.MessageReceived += (o, args) => Console.WriteLine("MessageReceived: " + args.Message);
                client.Disconnected += (o, args) => Console.WriteLine("Disconnected from server");
                client.Connected += (o, args) => Console.WriteLine("Connected to server");
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
                                Id = new Random().Next(),
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
            catch (TaskCanceledException)
            {
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
}
