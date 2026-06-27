using H.Pipes.Factories;

namespace H.Pipes.Tests;

[TestClass]
public class PipeServerTests
{
    [TestMethod]
    public async Task DisposeTest()
    {
        var pipeName = BaseTests.CreatePipeName("test");

        {
            try
            {
                using var source = new CancellationTokenSource(TimeSpan.FromSeconds(1));
#if !NETFRAMEWORK
                await using var pipe = await PipeServerFactory.CreateAndWaitAsync(pipeName, source.Token).ConfigureAwait(false);
#else
                using var pipe = await PipeServerFactory.CreateAndWaitAsync(pipeName, source.Token).ConfigureAwait(false);
#endif
            }
            catch (OperationCanceledException)
            {
            }
        }

        {
#if !NETFRAMEWORK
            await using var pipe = PipeServerFactory.Create(pipeName);
#else
            using var pipe = PipeServerFactory.Create(pipeName);
#endif
        }
    }

    [TestMethod]
    public async Task StartDisposeStart()
    {
        var pipeName = BaseTests.CreatePipeName("test");

        {
            await using var pipe = new PipeServer<string>(pipeName);
            await pipe.StartAsync();
        }

        await Task.Delay(TimeSpan.FromMilliseconds(1));

        {
            await using var pipe = new PipeServer<string>(pipeName);
            await pipe.StartAsync();
        }
    }

    [TestMethod]
    public async Task DoubleStartWithSameName()
    {
        var pipeName = BaseTests.CreatePipeName("test");

        await using var pipe1 = new PipeServer<string>(pipeName);

        await pipe1.StartAsync();

        await using var pipe2 = new PipeServer<string>(pipeName);

        await Assert.ThrowsExactlyAsync<IOException>(async () => await pipe2.StartAsync());
    }

    [TestMethod]
    public async Task StartStopStart()
    {
        var pipeName = BaseTests.CreatePipeName("test");
        await using var pipe = new PipeServer<string>(pipeName);

        await pipe.StartAsync();
        await pipe.StopAsync();
        await pipe.StartAsync();
    }
}
