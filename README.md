# Async Named Pipe Wrapper for .NET Standard 2.0

A simple, easy to use, strongly-typed, async wrapper around .NET named pipes.

# NuGet Package

Available as a [NuGet package](https://www.nuget.org/packages/H.Pipes/).
```
Install-Package H.Pipes -Version 1.0.0
```

# Features

*  Create named pipe servers that can handle multiple client connections simultaneously.
*  Send strongly-typed messages between clients and servers: any serializable .NET object can be sent over a pipe and will be automatically serialized/deserialized, including cyclical references and complex object graphs.
*  Async
*  Supports large messages - up to 300 MiB.

# Requirements

Requires .NET Standard 2.0.

# Usage

Server:

```csharp
await using var server = new PipeServer<MyMessage>(pipeName);
server.ClientConnected += async (o, args) =>
{
    Console.WriteLine($"Client {args.Connection.Id} is now connected!");

    await args.Connection.WriteAsync(new MyMessage
    {
        Id = new Random().Next(),
        Text = "Welcome!"
    });
};
server.ClientDisconnected += (o, args) =>
{
    Console.WriteLine($"Client {args.Connection.Id} disconnected");
};
server.MessageReceived += (sender, args) =>
{
    Console.WriteLine($"Client {args.Connection.Id} says: {args.Message}");
};
server.ExceptionOccurred += (o, args) => OnExceptionOccurred(args.Exception);

await server.StartAsync();

await Task.Delay(Timeout.InfiniteTimeSpan);
```

Client:

```csharp
await using var client = new PipeClient<MyMessage>(pipeName);
client.MessageReceived += (o, args) => Console.WriteLine("MessageReceived: " + args.Message);
client.Disconnected += (o, args) => Console.WriteLine("Disconnected from server");
client.Connected += (o, args) => Console.WriteLine("Connected to server");
client.ExceptionOccurred += (o, args) => OnExceptionOccurred(args.Exception);

await client.ConnectAsync();

await client.WriteAsync(new MyMessage
{
    Id = new Random().Next(),
    Text = "Hello!",
});

await Task.Delay(Timeout.InfiniteTimeSpan);
// ...
```

# MIT License

H.Pipes is licensed under the [MIT license](LICENSE.txt).
