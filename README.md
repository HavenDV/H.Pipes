# Async Named Pipe Wrapper for .NET Standard 2.0

[![Language](https://img.shields.io/badge/language-C%23-blue.svg?style=flat-square)](https://github.com/HavenDV/H.Pipes/search?l=C%23&o=desc&s=&type=Code) 
[![License](https://img.shields.io/github/license/HavenDV/H.Pipes.svg?label=License&maxAge=86400)](LICENSE.md) 
[![Requirements](https://img.shields.io/badge/Requirements-.NET%20Standard%202.0-blue.svg)](https://github.com/dotnet/standard/blob/master/docs/versions/netstandard2.0.md)
[![Build Status](https://github.com/HavenDV/H.Pipes/workflows/.NET%20Core/badge.svg?branch=master)](https://github.com/HavenDV/H.Pipes/actions?query=workflow%3A%22.NET+Core%22)

A simple, easy to use, strongly-typed, async wrapper around .NET named pipes.

## Nuget

[![NuGet](https://img.shields.io/nuget/dt/H.Pipes.svg?style=flat-square&label=H.Pipes)](https://www.nuget.org/packages/H.Pipes/)
[![NuGet](https://img.shields.io/nuget/dt/H.Pipes.Json.svg?style=flat-square&label=H.Pipes.Json)](https://www.nuget.org/packages/H.Pipes.Json/)
[![NuGet](https://img.shields.io/nuget/dt/H.Pipes.Wire.svg?style=flat-square&label=H.Pipes.Wire)](https://www.nuget.org/packages/H.Pipes.Wire/)

## Features

*  Create named pipe servers that can handle multiple client connections simultaneously.
*  Send strongly-typed messages between clients and servers: any serializable .NET object can be sent over a pipe and will be automatically serialized/deserialized, including cyclical references and complex object graphs.
*  Async
*  Supports large messages - up to 300 MiB.
*  Server restart automatically
*  Automatically wait for the release of the pipe for the server, if it is already in use
*  Automatically waiting for a pipe creating when client connecting
*  Automatic reconnect with a given interval and at each `client.WriteAsync`, if necessary

## Usage

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
```
