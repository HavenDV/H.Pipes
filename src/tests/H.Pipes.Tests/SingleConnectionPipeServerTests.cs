﻿namespace H.Pipes.Tests;

[TestClass]
public class SingleConnectionPipeServerTests
{
    [TestMethod]
    public async Task StartDisposeStart()
    {
        {
            await using var pipe = new SingleConnectionPipeServer<string>("test");

            await pipe.StartAsync();
        }

        await Task.Delay(TimeSpan.FromMilliseconds(1));

        {
            await using var pipe = new SingleConnectionPipeServer<string>("test");

            await pipe.StartAsync();
        }
    }

    [TestMethod]
    public async Task DoubleStartWithSameName()
    {
        await using var pipe1 = new SingleConnectionPipeServer<string>("test");

        await pipe1.StartAsync();

        await using var pipe2 = new SingleConnectionPipeServer<string>("test");

        await Assert.ThrowsExceptionAsync<IOException>(async () => await pipe2.StartAsync());
    }

    [TestMethod]
    public async Task StartStopStart()
    {
        await using var pipe = new SingleConnectionPipeServer<string>("test");

        await pipe.StartAsync();
        await pipe.StopAsync();
        await pipe.StartAsync();
    }
}
