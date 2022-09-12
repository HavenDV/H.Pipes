namespace H.Pipes.Tests;

[TestClass]
public class PipeClientTests
{
    [TestMethod]
    public async Task ConnectCancellationTest()
    {
        using var       cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var client                  = new PipeClient("this_pipe_100%_is_not_exists");

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            async () => await client.ConnectAsync(cancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task WriteAsyncCancellationTest()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var cancellationToken = cancellationTokenSource.Token;

        await using var client = new PipeClient<string>("this_pipe_100%_is_not_exists");

        var firstTask = Task.Run(async () =>
        {
            using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            source.CancelAfter(TimeSpan.FromSeconds(1));

            // ReSharper disable once AccessToDisposedClosure
            await client.WriteAsync(string.Empty, source.Token);
        }, cancellationToken);

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        {
            using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            source.CancelAfter(TimeSpan.FromSeconds(1));

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                async () => await client.WriteAsync(string.Empty, source.Token));
        }

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            async () => await firstTask);
    }
}
