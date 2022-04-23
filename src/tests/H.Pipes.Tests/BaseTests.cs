using System.Diagnostics;
using System.Text;
using H.Formatters;
using H.Pipes.Extensions;

namespace H.Pipes.Tests;

public static class BaseTests
{
    public static void SetupMessageReceived<T>(
        IPipeServer                      server,
        IPipeClient                      client,
        Action<string?>                  setActualHashFunc,
        Func<TaskCompletionSource<bool>> getTcsFunc,
        Func<T?, string>?                hashFunc,
        CancellationToken                cancellationToken)
    {
        server.MessageReceived += async (_, args) =>
        {
            Trace.WriteLine($"Server_OnMessageReceived: {args.Message}");

            T? value = default;

            if (args.Message != null)
                value = await args.Message.DeserializeAsync<T?>(server.Formatter, cancellationToken);

            var actualHash = hashFunc?.Invoke(value);
            setActualHashFunc(actualHash);
            Trace.WriteLine($"ActualHash: {actualHash}");

            // ReSharper disable once AccessToModifiedClosure
            _ = getTcsFunc().TrySetResult(true);
        };

        client.MessageReceived += (_, args) => Trace.WriteLine($"Client_OnMessageReceived: {args.Message}");
    }

    public static void SetupMessageReceived<T>(
        IPipeServer<T>                   server,
        IPipeClient<T>                   client,
        Action<string?>                  setActualHashFunc,
        Func<TaskCompletionSource<bool>> getTcsFunc,
        Func<T?, string>?                hashFunc)
    {
        server.MessageReceived += (_, args) =>
        {
            Trace.WriteLine($"Server_OnMessageReceived: {args.Message}");

            var actualHash = hashFunc?.Invoke(args.Message);

            setActualHashFunc(actualHash);
            Trace.WriteLine($"ActualHash: {actualHash}");

            // ReSharper disable once AccessToModifiedClosure
            _ = getTcsFunc().TrySetResult(true);
        };

        client.MessageReceived += (_, args) => Trace.WriteLine($"Client_OnMessageReceived: {args.Message}");
    }

    public static async Task DataTestAsync<T>(
        IPipeServer       server,
        IPipeClient       client,
        List<T>           values,
        Func<T?, string>? hashFunc          = null,
        CancellationToken cancellationToken = default,
        bool              useGeneric        = false)
    {
        Trace.WriteLine("Setting up test...");

        var completionSource = new TaskCompletionSource<bool>(false);

        // ReSharper disable once AccessToModifiedClosure
        using var registration = cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));

        var actualHash         = (string?)null;
        var clientDisconnected = false;


        //
        // Shared client/server setup

        if (useGeneric)
            SetupMessageReceived(
                (IPipeServer<T>)server, (IPipeClient<T>)client,
                h => actualHash = h, () => completionSource, hashFunc);

        else
            SetupMessageReceived(
                server, client,
                h => actualHash = h, () => completionSource, hashFunc, cancellationToken);


        //
        // Setup the server

        server.ClientConnected += (_, _) =>
        {
            Trace.WriteLine("Client connected");
        };
        server.ClientDisconnected += (_, _) =>
        {
            Trace.WriteLine("Client disconnected");
            clientDisconnected = true;

            // ReSharper disable once AccessToModifiedClosure
            _ = completionSource.TrySetResult(true);
        };
        server.ExceptionOccurred += (_, args) =>
        {
            Trace.WriteLine($"Server exception occurred: {args.Exception}");

            // ReSharper disable once AccessToModifiedClosure
            _ = completionSource.TrySetException(args.Exception);
        };


        //
        // Setup the client

        client.Connected    += (_, _) => Trace.WriteLine("Client_OnConnected");
        client.Disconnected += (_, _) => Trace.WriteLine("Client_OnDisconnected");
        client.ExceptionOccurred += (_, args) =>
        {
            Trace.WriteLine($"Client exception occurred: {args.Exception}");

            // ReSharper disable once AccessToModifiedClosure
            _ = completionSource.TrySetException(args.Exception);
        };


        //
        // Setup exception handling

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                // ReSharper disable once AccessToModifiedClosure
                _ = completionSource.TrySetException(exception);
            }
        };
        server.ExceptionOccurred += (_, args) => Trace.WriteLine(args.Exception.ToString());
        client.ExceptionOccurred += (_, args) => Trace.WriteLine(args.Exception.ToString());


        //
        // Start up the server and client

        await server.StartAsync(cancellationToken).ConfigureAwait(false);
        await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

        Trace.WriteLine("Client and server started");
        Trace.WriteLine("---");


        //
        // Begin testing

        var watcher = Stopwatch.StartNew();

        foreach (var value in values)
        {
            var expectedHash = hashFunc?.Invoke(value);
            Trace.WriteLine($"ExpectedHash: {expectedHash}");

            await client.WriteAsync(value, cancellationToken).ConfigureAwait(false);

            _ = await completionSource.Task.ConfigureAwait(false);

            if (hashFunc != null)
            {
                Assert.IsNotNull(actualHash, "Server should have received a zero-byte message from the client");
            }

            Assert.AreEqual(expectedHash, actualHash, "SHA-1 hashes for zero-byte message should match");
            Assert.IsFalse(clientDisconnected, "Server should not disconnect the client for explicitly sending zero-length data");

            Trace.WriteLine("---");

            completionSource = new TaskCompletionSource<bool>(false);
        }

        Trace.WriteLine($"Test took {watcher.Elapsed}");
        Trace.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~");
    }

    private static PipeServer CreateServer<T>(
        string      pipeName,
        IFormatter? formatter,
        bool        useGeneric)
    {
        return useGeneric
            ? new PipeServer<T>(pipeName, formatter)
            : new PipeServer(pipeName, formatter ?? new BinaryFormatter());
    }

    private static SingleConnectionPipeServer CreateSingleConnectionServer<T>(
        string      pipeName,
        IFormatter? formatter,
        bool        useGeneric)
    {
        return useGeneric
            ? new SingleConnectionPipeServer<T>(pipeName, formatter)
            : new SingleConnectionPipeServer(pipeName, formatter ?? new BinaryFormatter());
    }

    private static PipeClient CreateClient<T>(
        string      pipeName,
        IFormatter? formatter,
        bool        useGeneric)
    {
        return useGeneric
            ? new PipeClient<T>(pipeName, formatter: formatter)
            : new PipeClient(pipeName, formatter: formatter);
    }

    private static SingleConnectionPipeClient CreateSingleConnectionClient<T>(
        string      pipeName,
        IFormatter? formatter,
        bool        useGeneric)
    {
        return useGeneric
            ? new SingleConnectionPipeClient<T>(pipeName, formatter: formatter)
            : new SingleConnectionPipeClient(pipeName, formatter: formatter);
    }

    public static async Task DataTestAsync<T>(
        List<T>           values,
        Func<T?, string>? hashFunc   = null,
        IFormatter?       formatter  = default,
        TimeSpan?         timeout    = default,
        bool              useGeneric = false)
    {
        formatter ??= new BinaryFormatter();

        using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromMinutes(1));

        const string pipeName = "data_test_pipe";
        await using var server = CreateServer<T>(pipeName, formatter, useGeneric);
        
#if NET48
        // https://github.com/HavenDV/H.Pipes/issues/6
        server.WaitFreePipe = true;
#endif

        await using var client = CreateClient<T>(pipeName, formatter, useGeneric);

        await DataTestAsync(server, client, values, hashFunc, cancellationTokenSource.Token, useGeneric);
    }

    public static async Task DataSingleTestAsync<T>(
        List<T>           values,
        Func<T?, string>? hashFunc   = null,
        IFormatter?       formatter  = default,
        TimeSpan?         timeout    = default,
        bool              useGeneric = false)
    {
        formatter ??= new BinaryFormatter();

        using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromMinutes(1));

        const string    pipeName = "data_test_pipe";
        await using var server   = CreateSingleConnectionServer<T>(pipeName, formatter, useGeneric);
        
#if NET48
        // https://github.com/HavenDV/H.Pipes/issues/6
        //server.WaitFreePipe = true;
#endif

        await using var client = CreateSingleConnectionClient<T>(pipeName, formatter, useGeneric);

        await DataTestAsync(server, client, values, hashFunc, cancellationTokenSource.Token, useGeneric);
    }

    public static async Task BinaryDataTestAsync(
        int         numBytes,
        int         count      = 1,
        IFormatter? formatter  = default,
        TimeSpan?   timeout    = default,
        bool        useGeneric = false)
    {
        await DataTestAsync(GenerateData(numBytes, count), Hash, formatter, timeout, useGeneric);
    }

    public static async Task BinaryDataSingleTestAsync(
        int         numBytes,
        int         count      = 1,
        IFormatter? formatter  = default,
        TimeSpan?   timeout    = default,
        bool        useGeneric = false)
    {
        await DataSingleTestAsync(GenerateData(numBytes, count), Hash, formatter, timeout, useGeneric);
    }

    #region Helper methods

    public static List<byte[]> GenerateData(int numBytes, int count = 1)
    {
        var values = new List<byte[]>();

        for (var i = 0; i < count; i++)
        {
            var value = new byte[numBytes];
            new Random().NextBytes(value);

            values.Add(value);
        }

        return values;
    }

    /// <summary>
    /// Computes the SHA-1 hash (lowercase) of the specified byte array.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    private static string Hash(byte[]? bytes)
    {
        if (bytes == null)
        {
            return "null";
        }

        using var sha1 = System.Security.Cryptography.SHA1.Create();

        var hash = sha1.ComputeHash(bytes);
        var sb = new StringBuilder();
        foreach (var @byte in hash)
        {
            sb.Append(@byte.ToString("x2"));
        }
        return sb.ToString();
    }

    #endregion
}
