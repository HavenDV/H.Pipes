using H.Pipes;
using Polly;

const string DefaultPipeName = "named_pipe_test_server";

string mode = string.Empty;
string pipeName = DefaultPipeName;
if (args.Any())
{
    mode = args.ElementAt(0);
    pipeName = args.ElementAtOrDefault(1) ?? DefaultPipeName;
}

static void OnExceptionOccurred(Exception exception)
{
    Console.Error.WriteLine($"Exception: {exception}");
}

try
{
    using var source = new CancellationTokenSource();

    switch (mode?.ToUpperInvariant())
    {
        case "SERVER":
            {
                Console.WriteLine($"Running in SERVER mode. PipeName: {pipeName}");
                Console.WriteLine("Enter 'q' to exit");

                await using var server = new PipeServer<string>(pipeName);
                server.ClientConnected += async (_, args) =>
                {
                    Console.WriteLine($"Client {args.Connection.PipeName} is now connected!");

                    try
                    {
                        await args.Connection.WriteAsync("Welcome!", source.Token).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        OnExceptionOccurred(exception);
                    }
                };
                server.ClientDisconnected += (_, args) =>
                {
                    Console.WriteLine($"Client {args.Connection.PipeName} disconnected");
                };
                server.MessageReceived += (_, args) =>
                {
                    Console.WriteLine($"Client {args.Connection.PipeName} says: {args.Message}");
                };
                server.ExceptionOccurred += (_, args) => OnExceptionOccurred(args.Exception);

                var _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1));

                            if (new Random().Next(0, 10) == 0)
                            {
                                await server.StopAsync();
                                Console.WriteLine($"Server is stopped");
                            }
                            else if (!server.IsStarted && new Random().Next(0, 10) == 0)
                            {
                                await server.StartAsync();
                                Console.WriteLine($"Server is started");
                            }

                            if (server.IsStarted)
                            {
                                await server.WriteAsync("Test message", source.Token).ConfigureAwait(false);
                            }
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
                break;
            }

        default:
            {
                Console.WriteLine($"Running in CLIENT mode. PipeName: {pipeName}");
                Console.WriteLine("Enter 'q' to exit");

                await using var client = new PipeClient<string>(pipeName)
                {
                    AutoReconnect = false,
                };
                client.MessageReceived += (o, args) => Console.WriteLine("MessageReceived: " + args.Message);
                client.Disconnected += (o, args) => Console.WriteLine("Disconnected from server");
                client.Connected += (o, args) => Console.WriteLine("Connected to server");
                client.ExceptionOccurred += (o, args) => OnExceptionOccurred(args.Exception);

                var policy = Policy.WrapAsync(
                    Policy
                        .Handle<InvalidOperationException>()
                        .RetryAsync(3, async (exception, count) =>
                        {
                            await client.ConnectAsync();
                        }),
                    Policy.TimeoutAsync(TimeSpan.FromSeconds(1)));

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

                            await policy.ExecuteAsync(async cancellationToken =>
                                await client.WriteAsync(message ?? string.Empty, cancellationToken), source.Token).ConfigureAwait(false);
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
                break;
            }
    }
}
catch (OperationCanceledException)
{
}
catch (Exception exception)
{
    OnExceptionOccurred(exception);
}