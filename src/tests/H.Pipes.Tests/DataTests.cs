using H.Formatters;

namespace H.Pipes.Tests;

[TestClass]
public class DataTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task NullTest(bool useGeneric)
    {
        var values = new List<string?> { null };
        static string HashFunc(string? value) => value ?? "null";

        await BaseTests.DataSingleTestAsync(values, HashFunc, useGeneric: useGeneric);
        await BaseTests.DataSingleTestAsync(values, HashFunc, new NewtonsoftJsonFormatter(), useGeneric: useGeneric);
        await BaseTests.DataSingleTestAsync(values, HashFunc, new SystemTextJsonFormatter(), useGeneric: useGeneric);
        await BaseTests.DataSingleTestAsync(values, HashFunc, new CerasFormatter(), useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task EmptyArrayTest(bool useGeneric)
    {
        var values = new List<byte[]?> { Array.Empty<byte>() };
        static string HashFunc(byte[]? value) => value?.Length.ToString() ?? "null";

        await BaseTests.DataSingleTestAsync(values, HashFunc, useGeneric: useGeneric);
        await BaseTests.DataSingleTestAsync(values, HashFunc, new NewtonsoftJsonFormatter(), useGeneric: useGeneric);
        await BaseTests.DataSingleTestAsync(values, HashFunc, new SystemTextJsonFormatter(), useGeneric: useGeneric);
        await BaseTests.DataSingleTestAsync(values, HashFunc, new CerasFormatter(), useGeneric: useGeneric);

        values = new List<byte[]?> { null };

        await BaseTests.DataSingleTestAsync(values, HashFunc, useGeneric: useGeneric);
        await BaseTests.DataSingleTestAsync(values, HashFunc, new NewtonsoftJsonFormatter(), useGeneric: useGeneric);
        await BaseTests.DataSingleTestAsync(values, HashFunc, new SystemTextJsonFormatter(), useGeneric: useGeneric);
        await BaseTests.DataSingleTestAsync(values, HashFunc, new CerasFormatter(), useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task EmptyArrayParallelTest(bool useGeneric)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var cancellationToken = cancellationTokenSource.Token;

        const string pipeName = "data_test_pipe";
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
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestEmptyMessageDoesNotDisconnectClient(bool useGeneric)
    {
        await BaseTests.BinaryDataTestAsync(0, useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestMessageSize1B(bool useGeneric)
    {
        await BaseTests.BinaryDataTestAsync(1, useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestMessageSize2B(bool useGeneric)
    {
        await BaseTests.BinaryDataTestAsync(2, useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestMessageSize3B(bool useGeneric)
    {
        await BaseTests.BinaryDataTestAsync(3, useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestMessageSize9B(bool useGeneric)
    {
        await BaseTests.BinaryDataTestAsync(9, useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestMessageSize33B(bool useGeneric)
    {
        await BaseTests.BinaryDataTestAsync(33, useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestMessageSize1Kx3_NewtonsoftJson(bool useGeneric)
    {
        await BaseTests.BinaryDataTestAsync(1025, 3, new NewtonsoftJsonFormatter(), useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestMessageSize1Kx3_SystemTextJson(bool useGeneric)
    {
        await BaseTests.BinaryDataTestAsync(1025, 3, new SystemTextJsonFormatter(), useGeneric: useGeneric);
    }

    //[TestMethod]
    //[DataRow(true)]
    //[DataRow(false)]
    //public async Task TestMessageSize1Kx3_Ceras(bool useGeneric)
    //{
    //    await BaseTests.BinaryDataTestAsync(1025, 3, new CerasFormatter(), useGeneric: useGeneric);
    //}

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestMessageSize129B(bool useGeneric)
    {
        await BaseTests.BinaryDataTestAsync(129, useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestMessageSize1K(bool useGeneric)
    {
        await BaseTests.BinaryDataTestAsync(1025, useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestMessageSize1M(bool useGeneric)
    {
        await BaseTests.BinaryDataTestAsync(1024 * 1024 + 1, useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Single_TestEmptyMessageDoesNotDisconnectClient(bool useGeneric)
    {
        await BaseTests.BinaryDataSingleTestAsync(0, useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Single_TestMessageSize1B(bool useGeneric)
    {
        await BaseTests.BinaryDataSingleTestAsync(1, useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Single_TestMessageSize1Kx3_NewtonsoftJson(bool useGeneric)
    {
        await BaseTests.BinaryDataSingleTestAsync(1025, 3, new NewtonsoftJsonFormatter(), useGeneric: useGeneric);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Single_TestMessageSize1Kx3_SystemTextJson(bool useGeneric)
    {
        await BaseTests.BinaryDataSingleTestAsync(1025, 3, new SystemTextJsonFormatter(), useGeneric: useGeneric);
    }

    //[TestMethod]
    //[DataRow(true)]
    //[DataRow(false)]
    //public async Task Single_TestMessageSize1Kx3_Ceras(bool useGeneric)
    //{
    //    await BaseTests.BinaryDataSingleTestAsync(1025, 3, new CerasFormatter(), useGeneric: useGeneric);
    //}

    [TestMethod]
    public async Task TypeTest()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var completionSource = new TaskCompletionSource<bool>(false);
        using var registration = cancellationTokenSource.Token.Register(() => completionSource.TrySetCanceled());

        const string pipeName = "data_test_pipe";
        var formatter = new BinaryFormatter();
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
}
