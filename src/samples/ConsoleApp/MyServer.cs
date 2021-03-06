﻿using System;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes;

// ReSharper disable AccessToDisposedClosure

namespace ConsoleApp
{
    internal static class MyServer
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

                Console.WriteLine($"Running in SERVER mode. PipeName: {pipeName}");
                Console.WriteLine("Enter 'q' to exit");

                await using var server = new PipeServer<MyMessage>(pipeName);
                server.ClientConnected += async (_, args) =>
                {
                    Console.WriteLine($"Client {args.Connection.Id} is now connected!");

                    try
                    {
                        await args.Connection.WriteAsync(new MyMessage
                        {
                            Id = new Random().Next(),
                            Text = "Welcome!"
                        }, source.Token).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        OnExceptionOccurred(exception);
                    }
                };
                server.ClientDisconnected += (_, args) =>
                {
                    Console.WriteLine($"Client {args.Connection.Id} disconnected");
                };
                server.MessageReceived += (_, args) =>
                {
                    Console.WriteLine($"Client {args.Connection.Id} says: {args.Message}");
                };
                server.ExceptionOccurred += (_, args) => OnExceptionOccurred(args.Exception);

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

                            Console.WriteLine($"Sent to {server.ConnectedClients.Count} clients");

                            await server.WriteAsync(new MyMessage
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
}