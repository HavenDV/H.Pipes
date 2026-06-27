namespace H.Pipes.Tests;

[TestClass]
public class SingleConnectionPipeServerTests
{
    [TestMethod]
    public async Task StartDisposeStart()
    {
        var pipeName = BaseTests.CreatePipeName("test");

        {
            await using var pipe = new SingleConnectionPipeServer<string>(pipeName);

            await pipe.StartAsync();
        }

        await Task.Delay(TimeSpan.FromMilliseconds(1));

        {
            await using var pipe = new SingleConnectionPipeServer<string>(pipeName);

            await pipe.StartAsync();
        }
    }

    [TestMethod]
    public async Task DoubleStartWithSameName()
    {
        var pipeName = BaseTests.CreatePipeName("test");

        await using var pipe1 = new SingleConnectionPipeServer<string>(pipeName);

        await pipe1.StartAsync();

        await using var pipe2 = new SingleConnectionPipeServer<string>(pipeName);

        await Assert.ThrowsExactlyAsync<IOException>(async () => await pipe2.StartAsync());
    }

    [TestMethod]
    public async Task StartStopStart()
    {
        var pipeName = BaseTests.CreatePipeName("test");
        await using var pipe = new SingleConnectionPipeServer<string>(pipeName);

        await pipe.StartAsync();
        await pipe.StopAsync();
        await pipe.StartAsync();
    }
}
