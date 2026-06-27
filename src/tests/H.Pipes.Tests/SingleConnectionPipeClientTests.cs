namespace H.Pipes.Tests;

[TestClass]
public class SingleConnectionPipeClientTests
{
    [TestMethod]
    public async Task ClientConnectCancellationTest()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var client = new SingleConnectionPipeClient<string>(BaseTests.CreatePipeName("missing_pipe"));

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await client.ConnectAsync(cancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task ClientConnectTest()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var pipeName = BaseTests.CreatePipeName(nameof(ClientConnectTest));
        await using var client = new SingleConnectionPipeClient<string>(pipeName);
        await using var server = new SingleConnectionPipeServer<string>(pipeName);

        await server.StartAsync(cancellationToken: cancellationTokenSource.Token);

        await client.ConnectAsync(cancellationTokenSource.Token);
    }

    [TestMethod]
    public async Task ClientDoubleConnectTest()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pipeName = BaseTests.CreatePipeName(nameof(ClientDoubleConnectTest));
        await using var client = new SingleConnectionPipeClient<string>(pipeName);
        await using var server = new SingleConnectionPipeServer<string>(pipeName);

        await server.StartAsync(cancellationToken: cancellationTokenSource.Token);

        await client.ConnectAsync(cancellationTokenSource.Token);

        await client.DisconnectAsync(cancellationTokenSource.Token);

        await client.ConnectAsync(cancellationTokenSource.Token);
    }
}
