using H.Formatters;

namespace H.Pipes.Tests;

[TestClass]
public class DataTests
{
    [TestMethod]
    public async Task NullTest()
    {
        var values = new List<string?> { null };
        static string HashFunc(string? value) => value ?? "null";

        await BaseTests.DataSingleTestAsync(values, HashFunc);
        await BaseTests.DataSingleTestAsync(values, HashFunc, new NewtonsoftJsonFormatter());
        await BaseTests.DataSingleTestAsync(values, HashFunc, new SystemTextJsonFormatter());
        await BaseTests.DataSingleTestAsync(values, HashFunc, new CerasFormatter());
    }

    [TestMethod]
    public async Task EmptyArrayTest()
    {
        var values = new List<byte[]?> { Array.Empty<byte>() };
        static string HashFunc(byte[]? value) => value?.Length.ToString() ?? "null";

        await BaseTests.DataSingleTestAsync(values, HashFunc);
        await BaseTests.DataSingleTestAsync(values, HashFunc, new NewtonsoftJsonFormatter());
        await BaseTests.DataSingleTestAsync(values, HashFunc, new SystemTextJsonFormatter());
        await BaseTests.DataSingleTestAsync(values, HashFunc, new CerasFormatter());

        values = new List<byte[]?> { null };

        await BaseTests.DataSingleTestAsync(values, HashFunc);
        await BaseTests.DataSingleTestAsync(values, HashFunc, new NewtonsoftJsonFormatter());
        await BaseTests.DataSingleTestAsync(values, HashFunc, new SystemTextJsonFormatter());
        await BaseTests.DataSingleTestAsync(values, HashFunc, new CerasFormatter());
    }

    [TestMethod]
    public async Task EmptyArrayParallelTest()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var cancellationToken = cancellationTokenSource.Token;

        var pipeName = BaseTests.CreatePipeName();
        await using var server = new SingleConnectionPipeServer<string?>(pipeName)
        {
            WaitFreePipe = true
        };
        await using var client = new SingleConnectionPipeClient<string?>(pipeName);

        await server.StartAsync(cancellationToken).ConfigureAwait(false);
        await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

        var tasks = Enumerable
            .Range(0, 10000)
            .Select(async _ => await client.WriteAsync(null, cancellationTokenSource.Token))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    [TestMethod]
    public async Task TestEmptyMessageDoesNotDisconnectClient()
    {
        await BaseTests.BinaryDataTestAsync(0);
    }

    [TestMethod]
    public async Task TestMessageSize1B()
    {
        await BaseTests.BinaryDataTestAsync(1);
    }

    [TestMethod]
    public async Task TestMessageSize2B()
    {
        await BaseTests.BinaryDataTestAsync(2);
    }

    [TestMethod]
    public async Task TestMessageSize3B()
    {
        await BaseTests.BinaryDataTestAsync(3);
    }

    [TestMethod]
    public async Task TestMessageSize9B()
    {
        await BaseTests.BinaryDataTestAsync(9);
    }

    [TestMethod]
    public async Task TestMessageSize33B()
    {
        await BaseTests.BinaryDataTestAsync(33);
    }

    [TestMethod]
    public async Task TestMessageSize1Kx3_NewtonsoftJson()
    {
        await BaseTests.BinaryDataTestAsync(1025, 3, new NewtonsoftJsonFormatter());
    }

    [TestMethod]
    public async Task TestMessageSize1Kx3_SystemTextJson()
    {
        await BaseTests.BinaryDataTestAsync(1025, 3, new SystemTextJsonFormatter());
    }

    //[TestMethod]
    //public async Task TestMessageSize1Kx3_Ceras()
    //{
    //    await BaseTests.BinaryDataTestAsync(1025, 3, new CerasFormatter());
    //}

    [TestMethod]
    public async Task TestMessageSize129B()
    {
        await BaseTests.BinaryDataTestAsync(129);
    }

    [TestMethod]
    public async Task TestMessageSize1K()
    {
        await BaseTests.BinaryDataTestAsync(1025);
    }

    [TestMethod]
    public async Task TestMessageSize1M()
    {
        await BaseTests.BinaryDataTestAsync(1024 * 1024 + 1);
    }

    [TestMethod]
    public async Task Single_TestEmptyMessageDoesNotDisconnectClient()
    {
        await BaseTests.BinaryDataSingleTestAsync(0);
    }

    [TestMethod]
    public async Task Single_TestMessageSize1B()
    {
        await BaseTests.BinaryDataSingleTestAsync(1);
    }

    [TestMethod]
    public async Task Single_TestMessageSize1Kx3_NewtonsoftJson()
    {
        await BaseTests.BinaryDataSingleTestAsync(1025, 3, new NewtonsoftJsonFormatter());
    }

    [TestMethod]
    public async Task Single_TestMessageSize1Kx3_SystemTextJson()
    {
        await BaseTests.BinaryDataSingleTestAsync(1025, 3, new SystemTextJsonFormatter());
    }

    //[TestMethod]
    //public async Task Single_TestMessageSize1Kx3_Ceras()
    //{
    //    await BaseTests.BinaryDataSingleTestAsync(1025, 3, new CerasFormatter());
    //}

    [TestMethod]
    public async Task NonGenericPipeServerRoundTripsRawBytes()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var pipeName = BaseTests.CreatePipeName("raw");
        await using var server = new PipeServer(pipeName)
        {
            WaitFreePipe = true,
        };
        await using var client = new PipeClient(pipeName);

        await RawDataTestAsync(server, client, cancellationTokenSource.Token);
    }

    [TestMethod]
    public async Task NonGenericSingleConnectionPipeServerRoundTripsRawBytes()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var pipeName = BaseTests.CreatePipeName("single-raw");
        await using var server = new SingleConnectionPipeServer(pipeName)
        {
            WaitFreePipe = true,
        };
        await using var client = new SingleConnectionPipeClient(pipeName);

        await RawDataTestAsync(server, client, cancellationTokenSource.Token);
    }

    private static async Task RawDataTestAsync(IPipeServer server, IPipeClient client, CancellationToken cancellationToken)
    {
        var values = new[]
        {
            Array.Empty<byte>(),
            new byte[] { 1, 2, 3, 4, 5 },
        };

        static TaskCompletionSource<byte[]?> CreateCompletionSource()
        {
            return new TaskCompletionSource<byte[]?>();
        }

        var serverCompletionSource = CreateCompletionSource();
        var clientCompletionSource = CreateCompletionSource();

        using var registration = cancellationToken.Register(() =>
        {
            serverCompletionSource.TrySetCanceled();
            clientCompletionSource.TrySetCanceled();
        });

        server.MessageReceived += (_, args) => serverCompletionSource.TrySetResult(args.Message);
        client.MessageReceived += (_, args) => clientCompletionSource.TrySetResult(args.Message);
        server.ExceptionOccurred += (_, args) => serverCompletionSource.TrySetException(args.Exception);
        client.ExceptionOccurred += (_, args) => clientCompletionSource.TrySetException(args.Exception);

        await server.StartAsync(cancellationToken).ConfigureAwait(false);
        await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

        foreach (var value in values)
        {
            serverCompletionSource = CreateCompletionSource();

            await client.WriteAsync(value, cancellationToken).ConfigureAwait(false);

            var serverMessage = await serverCompletionSource.Task.ConfigureAwait(false);
            serverMessage.Should().Equal(value);

            clientCompletionSource = CreateCompletionSource();

            await server.WriteAsync(value, cancellationToken).ConfigureAwait(false);

            var clientMessage = await clientCompletionSource.Task.ConfigureAwait(false);
            clientMessage.Should().Equal(value);
        }
    }
    
#if !NET8_0_OR_GREATER
    [TestMethod]
    public async Task TypeTest()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var completionSource = new TaskCompletionSource<bool>(false);
        using var registration = cancellationTokenSource.Token.Register(() => completionSource.TrySetCanceled());

        var pipeName = BaseTests.CreatePipeName();
        var formatter = new DefaultFormatter();
        await using var server = new PipeServer<object>(pipeName, formatter);
        await using var client = new PipeClient<object>(pipeName, formatter: formatter);

        server.ExceptionOccurred += (_, args) => Assert.Fail(args.Exception.ToString());
        client.ExceptionOccurred += (_, args) => Assert.Fail(args.Exception.ToString());
        server.MessageReceived += (_, args) =>
        {
            Console.WriteLine($"MessageReceived: {args.Message as Exception}");

            completionSource.TrySetResult(args.Message is Exception);
        };

        await server.StartAsync(cancellationTokenSource.Token);

        await client.ConnectAsync(cancellationTokenSource.Token);

        await client.WriteAsync(new Exception("Hello. It's server message"), cancellationTokenSource.Token);

        Assert.IsTrue(await completionSource.Task);
    }
#endif
}
