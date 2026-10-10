using H.Formatters;

namespace H.Pipes.Tests;

/// <summary>
/// Verifies encrypted key exchange and application message delivery when both peers respond immediately.
/// </summary>
[TestClass]
public sealed class InfernoKeyExchangeTests
{
    /// <summary>
    /// Asynchronously verifies repeated key exchanges complete and both directions remain encrypted.
    /// </summary>
    [TestMethod]
    public async Task EnableEncryption_ImmediatePeerResponse_CompletesKeyExchangeAndRoundTripsMessages()
    {
#if !NETFRAMEWORK
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("Inferno key exchange requires Windows CNG.");
            return;
        }
#endif

        for (var iteration = 0; iteration < 16; iteration++)
        {
            using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await VerifyEncryptedRoundTripAsync(iteration, timeoutSource.Token).ConfigureAwait(false);
        }
    }

#if NET9_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    private static async Task VerifyEncryptedRoundTripAsync(
        int iteration,
        CancellationToken cancellationToken)
    {
        var pipeName = $"inferno-{Guid.NewGuid():N}";
        var connectedSource = new TaskCompletionSource<PipeConnection<string>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedByServer = new TaskCompletionSource<string?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedByClient = new TaskCompletionSource<string?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var encryptionFailure = new TaskCompletionSource<Exception>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await using var server = new PipeServer<string>(
            pipeName,
            new SystemTextJsonFormatter());
        await using var client = new PipeClient<string>(
            pipeName,
            new SystemTextJsonFormatter());

        server.EnableEncryption(exception =>
        {
            _ = encryptionFailure.TrySetResult(exception);
        });
        client.EnableEncryption(exception =>
        {
            _ = encryptionFailure.TrySetResult(exception);
        });

        server.ClientConnected += (_, args) =>
        {
            _ = connectedSource.TrySetResult(args.Connection);
        };
        server.MessageReceived += (_, args) =>
        {
            _ = receivedByServer.TrySetResult(args.Message);
        };
        client.MessageReceived += (_, args) =>
        {
            _ = receivedByClient.TrySetResult(args.Message);
        };
        server.ExceptionOccurred += (_, args) =>
        {
            _ = encryptionFailure.TrySetResult(args.Exception);
        };
        client.ExceptionOccurred += (_, args) =>
        {
            _ = encryptionFailure.TrySetResult(args.Exception);
        };

        await server.StartAsync(cancellationToken).ConfigureAwait(false);
        await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

        var serverConnection = await WaitOrCancelAsync(
            connectedSource.Task,
            cancellationToken).ConfigureAwait(false);
        var clientConnection = client.Connection ??
            throw new InvalidOperationException("The client did not establish a connection.");

        var exchangeTask = Task.WhenAll(
            serverConnection.WaitExchangeAsync(cancellationToken),
            clientConnection.WaitExchangeAsync(cancellationToken));
        var cancellationSource = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            _ = cancellationSource.TrySetResult(true);
        });
        var observed = await Task.WhenAny(
            exchangeTask,
            encryptionFailure.Task,
            cancellationSource.Task).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        if (ReferenceEquals(observed, encryptionFailure.Task))
        {
            throw new InvalidOperationException(
                "The encrypted key exchange failed.",
                await encryptionFailure.Task.ConfigureAwait(false));
        }

        cancellationToken.ThrowIfCancellationRequested();
        await exchangeTask.ConfigureAwait(false);

        var request = $"request-{iteration}";
        await client.WriteAsync(request, cancellationToken).ConfigureAwait(false);
        Assert.AreEqual(
            request,
            await WaitOrCancelAsync(receivedByServer.Task, cancellationToken).ConfigureAwait(false));

        var response = $"response-{iteration}";
        await server.WriteAsync(response, cancellationToken).ConfigureAwait(false);
        Assert.AreEqual(
            response,
            await WaitOrCancelAsync(receivedByClient.Task, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<T> WaitOrCancelAsync<T>(
        Task<T> task,
        CancellationToken cancellationToken)
    {
        var cancellationSource = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            _ = cancellationSource.TrySetResult(true);
        });

        _ = await Task.WhenAny(task, cancellationSource.Task).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return await task.ConfigureAwait(false);
    }
}
