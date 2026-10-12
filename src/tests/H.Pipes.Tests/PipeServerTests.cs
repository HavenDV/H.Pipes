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

        for (var iteration = 0; iteration < 25; iteration++)
        {
            await pipe.StartAsync();
            Assert.IsTrue(pipe.IsStarted);
            await pipe.StopAsync();
            Assert.IsFalse(pipe.IsStarted);
        }

        await pipe.StartAsync();
    }

    [TestMethod]
    public async Task WorkerDisposal_WaitsForCanceledListener_BeforeReusingPipe()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var listenerStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCleanup = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pipeName = BaseTests.CreatePipeName("cleanup");
        Func<CancellationToken, Task> listener = async token =>
        {
            try
            {
                using var pipe = PipeServerFactory.Create(pipeName);
                _ = listenerStarted.TrySetResult(true);
                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                finally
                {
                    _ = cleanupStarted.TrySetResult(true);
                    await releaseCleanup.Task;
                }
            }
            finally
            {
                _ = cleanupCompleted.TrySetResult(true);
            }
        };

        // Exercise the actual listener worker without adding a public test hook to the library.
        var workerType = typeof(PipeServer).Assembly.GetType("H.Pipes.Utilities.TaskWorker", throwOnError: true)!;
        var worker = (IAsyncDisposable)Activator.CreateInstance(workerType, listener, null)!;
        Task? stopTask = null;
        try
        {
            await WaitForAsync(listenerStarted.Task, timeout.Token);
            stopTask = worker.DisposeAsync().AsTask();
            await WaitForAsync(cleanupStarted.Task, timeout.Token);

            // Cleanup deliberately keeps the native pipe open, so disposal must remain pending.
            _ = await Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromSeconds(1), timeout.Token));
            timeout.Token.ThrowIfCancellationRequested();
            Assert.IsFalse(stopTask.IsCompleted, "Worker disposal returned while the listener still owned the pipe.");
        }
        finally
        {
            _ = releaseCleanup.TrySetResult(true);
            if (stopTask != null)
            {
                await stopTask;
            }
            else
            {
                await worker.DisposeAsync();
            }
            await WaitForAsync(cleanupCompleted.Task, timeout.Token);
        }

        // Rebinding immediately after disposal must succeed without a sleep or retry.
        using var replacement = PipeServerFactory.Create(pipeName);
    }

    private static async Task WaitForAsync(Task task, CancellationToken cancellationToken)
    {
        var canceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => canceled.TrySetResult(true));
        _ = await Task.WhenAny(task, canceled.Task);
        cancellationToken.ThrowIfCancellationRequested();
        await task;
    }

    [TestMethod]
    public async Task StopAsync_WithIdleConnectedClient_CompletesAndAllowsRestart()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var pipeName = BaseTests.CreatePipeName("idle");
        var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errors = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
        await using var server = new PipeServer<string>(pipeName);
        await using var client = new PipeClient<string>(pipeName)
        {
            AutoReconnect = false,
        };
        server.ClientConnected += (_, _) => connected.TrySetResult(true);
        server.ExceptionOccurred += (_, args) => errors.Enqueue(args.Exception);

        await server.StartAsync(timeout.Token);
        await client.ConnectAsync(timeout.Token);
        await WaitForAsync(connected.Task, timeout.Token);

        var stopTask = server.StopAsync(timeout.Token);
        try
        {
            await WaitForAsync(stopTask, timeout.Token);
        }
        finally
        {
            // Always unblock the listener if the regression returns, so failure cannot hang the suite.
            await client.DisconnectAsync();
            await stopTask;
        }

        Assert.AreEqual(0, errors.Count, "Normal shutdown must not report interrupted reads as transport errors.");
        Assert.IsFalse(server.IsStarted);
        await server.StartAsync(timeout.Token);
        await server.StopAsync(timeout.Token);
    }
}
