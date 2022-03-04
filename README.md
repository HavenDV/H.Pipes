## Async Named Pipe Wrapper for .NET Standard 2.0

[![Language](https://img.shields.io/badge/language-C%23-blue.svg?style=flat-square)](https://github.com/HavenDV/H.Pipes/search?l=C%23&o=desc&s=&type=Code) 
[![License](https://img.shields.io/github/license/HavenDV/H.Pipes.svg?label=License&maxAge=86400)](LICENSE.txt) 
[![Requirements](https://img.shields.io/badge/Requirements-.NET%20Standard%202.0-blue.svg)](https://github.com/dotnet/standard/blob/master/docs/versions/netstandard2.0.md)
[![Build Status](https://github.com/HavenDV/H.Pipes/workflows/.NET%20Core/badge.svg?branch=master)](https://github.com/HavenDV/H.Pipes/actions?query=workflow%3A%22.NET+Core%22)

A simple, easy to use, strongly-typed, async wrapper around .NET named pipes.

### Features

*  Create named pipe servers that can handle multiple client connections simultaneously.
*  Send strongly-typed messages between clients and servers: any serializable .NET object can be sent over a pipe and will be automatically serialized/deserialized, including cyclical references and complex object graphs.
*  Async
*  Requires .NET Standard 2.0
*  Supports large messages - up to 300 MiB.
*  Server restart automatically
*  Automatically wait for the release of the pipe for the server, if it is already in use
*  Automatically waiting for a server pipe creating when client connecting
*  Automatic reconnect with a given interval and at each `client.WriteAsync`, if necessary
*  Supports variable formatters, default - BinaryFormatter which uses System.Runtime.Serialization.BinaryFormatter inside
*  Also available ready formatters in separate nuget packages: H.Formatters.Newtonsoft.Json, H.Formatters.System.Text.Json and H.Formatters.Ceras
*  Supports `PipeAccessRule`'s(see `H.Pipes.AccessControl` nuget package) or more complex code to access using the `PipeServer.PipeStreamInitializeAction` property

### Nuget

[![NuGet](https://img.shields.io/nuget/dt/H.Pipes.svg?style=flat-square&label=H.Pipes)](https://www.nuget.org/packages/H.Pipes/)
[![NuGet](https://img.shields.io/nuget/dt/H.Pipes.AccessControl.svg?style=flat-square&label=H.Pipes.AccessControl)](https://www.nuget.org/packages/H.Pipes.AccessControl/)
[![NuGet](https://img.shields.io/nuget/dt/H.Formatters.Newtonsoft.Json.svg?style=flat-square&label=H.Formatters.Newtonsoft.Json)](https://www.nuget.org/packages/H.Formatters.Newtonsoft.Json/)
[![NuGet](https://img.shields.io/nuget/dt/H.Formatters.System.Text.Json.svg?style=flat-square&label=H.Formatters.System.Text.Json)](https://www.nuget.org/packages/H.Formatters.System.Text.Json/)
[![NuGet](https://img.shields.io/nuget/dt/H.Formatters.Ceras.svg?style=flat-square&label=H.Formatters.Ceras)](https://www.nuget.org/packages/H.Formatters.Ceras/)
```
// All clients and servers that do not need support AccessControl.
Install-Package H.Pipes

// Servers that need support AccessControl.
Install-Package H.Pipes.AccessControl

// If you want to transfer any data that can be serialized/deserialized in json using Newtonsoft.Json.
Install-Package H.Formatters.Newtonsoft.Json

// If you want to transfer any data that can be serialized/deserialized in json using System.Text.Json.
Install-Package H.Formatters.System.Text.Json

// If you want to transfer any data that can be serialized/deserialized in binary using Ceras.
Install-Package H.Formatters.Ceras
```

### Usage

Server:

```csharp
await using var server = new PipeServer<MyMessage>(pipeName);
server.ClientConnected += async (o, args) =>
{
    Console.WriteLine($"Client {args.Connection.PipeName} is now connected!");

    await args.Connection.WriteAsync(new MyMessage
    {
        Text = "Welcome!"
    });
};
server.ClientDisconnected += (o, args) =>
{
    Console.WriteLine($"Client {args.Connection.PipeName} disconnected");
};
server.MessageReceived += (sender, args) =>
{
    Console.WriteLine($"Client {args.Connection.PipeName} says: {args.Message}");
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
    Text = "Hello!",
});

await Task.Delay(Timeout.InfiniteTimeSpan);
```

Notes:
- To use the server inside the WinForms/WPF/Other UI application, use Task.Run() or any alternative.
- Be careful and call `Dispose` before closing the program/after the end of use. 
Pipes are system resources and you might have problems restarting the server if you don't properly clean up the resources.

### Custom Formatters
Since BinaryFormatter is used by default, you should check out this article:
https://docs.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide
```
Install-Package H.Formatters.Newtonsoft.Json
Install-Package H.Formatters.System.Text.Json
Install-Package H.Formatters.Ceras
```

```csharp
using H.Formatters;

await using var server = new PipeServer<MyMessage>(pipeName, formatter: new NewtonsoftJsonFormatter());
await using var client = new PipeClient<MyMessage>(pipeName, formatter: new NewtonsoftJsonFormatter());
```

### Access Control
```
Install-Package H.Pipes.AccessControl
```

```csharp
using System.IO.Pipes;
using H.Pipes.AccessControl;

await using var server = new PipeServer<string>(pipeName);

// You can set PipeSecurity
var pipeSecurity = new PipeSecurity();
pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));

server.SetPipeSecurity(pipeSecurity);

// or just add AccessRule's (Please be careful, the server will only consider AccessRules from the last call AddAccessRules())
server.AddAccessRules(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));

// or just
server.AllowUsersReadWrite();
```

### Encryption
```
Install-Package H.Formatters.Inferno
```

```csharp
using H.Formatters;

await using var server = new PipeServer<MyMessage>(pipeName, formatter: new SystemTextJsonFormatter());
server.EnableEncryption();

await using var client = new PipeClient<MyMessage>(pipeName, formatter: new SystemTextJsonFormatter());
client.EnableEncryption();

await client.ConnectAsync(source.Token).ConfigureAwait(false);
// Waits for key exchange.
await client.Connection!.WaitExchangeAsync();

server.ClientConnected += async (_, args) =>
{
    // Waits for key exchange.
    await args.Connection.WaitExchangeAsync();

    await args.Connection.WriteAsync(new MyMessage
    {
        Text = "Welcome!"
    }, source.Token).ConfigureAwait(false);
};
```

### GetImpersonationUserName
```csharp
server.ClientConnected += async (o, args) =>
{
    var name = args.Connection.GetImpersonationUserName();

    Console.WriteLine($"Client {name} is now connected!");
};
```

### Inter-process communication
I recommend that you take a look at [my other library](https://github.com/HavenDV/H.ProxyFactory) if you plan on doing IPC.
It is based on this library, but provides an IPC implementation based on C# interfaces.
It supports remote method invocation, asynchronous methods, cancellation via `CancellationToken`, events, and so on.

### Contacts
* [mail](mailto:havendv@gmail.com)