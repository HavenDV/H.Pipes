﻿#if NET6_0_OR_GREATER
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;

[assembly: DoNotParallelize]

namespace H.Pipes.Tests;

// ReSharper disable AccessToDisposedClosure

[TestClass]
public class Tests
{
    [Ignore]
    [TestMethod]
    public async Task Interrupted()
    {
        using var source = new CancellationTokenSource(TimeSpan.FromSeconds(11));
        var cancellationToken = source.Token;
        var isConnected = false;
        
        var exceptions = new ConcurrentBag<Exception>();
        const string pipeName = "int";
        try
        {

            Console.WriteLine($"PipeName: {pipeName}");

            await using var server = new PipeServer<byte[]>(pipeName);
            server.ClientConnected += async (_, args) =>
            {
                Console.WriteLine($"Client {args.Connection.PipeName} is now connected!");

                try
                {
                    await args.Connection.WriteAsync(new byte[]
                    {
                        1, 2, 3, 4, 5
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
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
            server.ExceptionOccurred += (_, args) => exceptions.Add(args.Exception);

            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Console.WriteLine($"Sent to {server.ConnectedClients.Count} clients");

                        await server.WriteAsync(new byte[]
                        {
                            1, 2, 3, 4, 5
                        }, cancellationToken).ConfigureAwait(false);

                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception exception)
                    {
                        exceptions.Add(exception);
                    }
                }
            }, cancellationToken);

            Console.WriteLine("Server starting...");

            await server.StartAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            Console.WriteLine("Server is started!");

            // https://github.com/HavenDV/H.Pipes/issues/27
            {
                await using var pipeClient = new NamedPipeClientStream(pipeName);
                while (!pipeClient.IsConnected)
                {
                    await pipeClient.ConnectAsync(cancellationToken);
                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                }
            
                // read string length
                var buffer = new byte[sizeof(int)];
                _ = await pipeClient.ReadAsync(buffer.AsMemory(0, sizeof(int)), cancellationToken: cancellationToken);
                var len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
            
                // read string content
                buffer = new byte[len];
                _ = await pipeClient.ReadAsync(buffer.AsMemory(0, len), cancellationToken);
            }
            
            await using var client = new PipeClient<byte[]>(pipeName);
            await client.ConnectAsync(cancellationToken);

            isConnected = true;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            exceptions.Add(exception);
        }
        
        if (!exceptions.IsEmpty)
        {
            throw new AggregateException(exceptions);
        }

        isConnected.Should().BeTrue();
    }
    //
    // [TestMethod]
    // public async Task WriteOnlyServer()
    // {
    //     using var source = new CancellationTokenSource(TimeSpan.FromSeconds(11));
    //     var cancellationToken = source.Token;
    //     var isConnected = false;
    //     
    //     var exceptions = new ConcurrentBag<Exception>();
    //     const string pipeName = "wos";
    //     try
    //     {
    //
    //         Console.WriteLine($"PipeName: {pipeName}");
    //
    //         await using var server = new PipeServer<byte[]>(pipeName);
    //         server.CreatePipeStreamFunc = static pipeName => new NamedPipeServerStream(
    //             pipeName: pipeName,
    //             direction: PipeDirection.Out,
    //             maxNumberOfServerInstances: 1,
    //             transmissionMode: PipeTransmissionMode.Byte,
    //             options: PipeOptions.Asynchronous | PipeOptions.WriteThrough,
    //             inBufferSize: 0,
    //             outBufferSize: 0);
    //         
    //         server.ClientConnected += async (_, args) =>
    //         {
    //             Console.WriteLine($"Client {args.Connection.PipeName} is now connected!");
    //
    //             try
    //             {
    //                 await args.Connection.WriteAsync(new byte[]
    //                 {
    //                     1, 2, 3, 4, 5,
    //                 }, cancellationToken).ConfigureAwait(false);
    //             }
    //             catch (OperationCanceledException)
    //             {
    //             }
    //             catch (Exception exception)
    //             {
    //                 exceptions.Add(exception);
    //             }
    //         };
    //         server.ClientDisconnected += (_, args) =>
    //         {
    //             Console.WriteLine($"Client {args.Connection.PipeName} disconnected");
    //         };
    //         server.MessageReceived += (_, args) =>
    //         {
    //             Console.WriteLine($"Client {args.Connection.PipeName} says: {args.Message}");
    //         };
    //         server.ExceptionOccurred += (_, args) => exceptions.Add(args.Exception);
    //
    //         _ = Task.Run(async () =>
    //         {
    //             while (!cancellationToken.IsCancellationRequested)
    //             {
    //                 try
    //                 {
    //                     Console.WriteLine($"Sent to {server.ConnectedClients.Count} clients");
    //
    //                     await server.WriteAsync(new byte[]
    //                     {
    //                         1, 2, 3, 4, 5,
    //                     }, cancellationToken).ConfigureAwait(false);
    //
    //                     await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    //                 }
    //                 catch (OperationCanceledException)
    //                 {
    //                 }
    //                 catch (Exception exception)
    //                 {
    //                     exceptions.Add(exception);
    //                 }
    //             }
    //         }, cancellationToken);
    //
    //         Console.WriteLine("Server starting...");
    //
    //         await server.StartAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    //
    //         Console.WriteLine("Server is started!");
    //
    //         var isClientReceivedMessage = new TaskCompletionSource<bool>();
    //         cancellationToken.Register(() => isClientReceivedMessage.TrySetCanceled());
    //         await using var client = new PipeClient<byte[]>(pipeName);
    //         client.CreatePipeStreamForConnectionFunc = static (pipeName, serverName) => new NamedPipeClientStream(
    //             serverName: serverName,
    //             pipeName: pipeName,
    //             direction: PipeDirection.In,
    //             options: PipeOptions.Asynchronous | PipeOptions.WriteThrough);
    //         
    //         client.MessageReceived += (_, _) =>
    //         {
    //             _ = isClientReceivedMessage.TrySetResult(true);
    //         };
    //         await client.ConnectAsync(cancellationToken);
    //
    //         await isClientReceivedMessage.Task;
    //         
    //         isClientReceivedMessage.Task.Result.Should().BeTrue();
    //         
    //         isConnected = true;
    //     }
    //     catch (OperationCanceledException)
    //     {
    //     }
    //     catch (Exception exception)
    //     {
    //         exceptions.Add(exception);
    //     }
    //     
    //     if (!exceptions.IsEmpty)
    //     {
    //         throw new AggregateException(exceptions);
    //     }
    //
    //     isConnected.Should().BeTrue();
    // }
    
    [TestMethod]
    public async Task Reconnect()
    {
        using var source = new CancellationTokenSource(TimeSpan.FromSeconds(11));
        var cancellationToken = source.Token;
        var isConnected = false;
        
        var exceptions = new ConcurrentBag<Exception>();
        const string pipeName = "rcs";
        try
        {
    
            Console.WriteLine($"PipeName: {pipeName}");
    
            await using var server = new PipeServer<byte[]>(pipeName);
            server.ClientConnected += (_, args) =>
            {
                Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: Client {args.Connection.PipeName} is now connected!");
            };
            server.ClientDisconnected += (_, args) =>
            {
                Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: Client {args.Connection.PipeName} disconnected");
            };
            server.ExceptionOccurred += (_, args) => exceptions.Add(args.Exception);
    
            Console.WriteLine("Server starting...");
    
            await server.StartAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    
            _ = Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: Waiting 1 second");

                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                    Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: Stopping server");
                    
                    await server.StopAsync(cancellationToken);
                    
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: Starting server");
                    
                    await server.StartAsync(cancellationToken);

                    Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: Waiting 3 seconds");
                    
                    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                    
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: Send data to clients");
                    
                    try
                    {
                        await server.WriteAsync(
                            [1, 2, 3, 4, 5], cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception exception)
                    {
                        exceptions.Add(exception);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            }, cancellationToken);
            
            Console.WriteLine("Server is started!");
    
            var isClientReceivedMessage = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => isClientReceivedMessage.TrySetCanceled());
            
            await using var client = new PipeClient<byte[]>(pipeName);
            
            client.MessageReceived += (_, _) =>
            {
                _ = isClientReceivedMessage.TrySetResult(true);
            };
            await client.ConnectAsync(cancellationToken);
    
            var result = await isClientReceivedMessage.Task;
            
            result.Should().BeTrue();
            
            isConnected = true;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            exceptions.Add(exception);
        }
        
        if (!exceptions.IsEmpty)
        {
            throw new AggregateException(exceptions);
        }
    
        isConnected.Should().BeTrue();
    }
}
#endif
