using System.IO.Pipes;
using H.Formatters;
using H.Pipes.Args;

namespace H.Pipes.Tests;

/// <summary>
/// Verifies immediate key-exchange responses and failure isolation between connection generations.
/// </summary>
[TestClass]
public sealed class InfernoKeyExchangeEdgeCaseTests
{
    /// <summary>
    /// Asynchronously verifies an immediate response sent using the receiving connection reaches the client.
    /// </summary>
    [TestMethod]
    public async Task SingleConnectionPipeServer_ImmediateReplyThroughMessageConnection_ReachesClient()
    {
        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = timeoutSource.Token;
        var pipeName = $"inferno-reply-{Guid.NewGuid():N}";

        await using var server = new SingleConnectionPipeServer<byte[]>(
            pipeName,
            new SystemTextJsonFormatter());
        await using var client = new SingleConnectionPipeClient<byte[]>(
            pipeName,
            new SystemTextJsonFormatter())
        {
            AutoReconnect = false,
        };

        var receivedSource = new TaskCompletionSource<byte[]?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var replyFailureSource = new TaskCompletionSource<Exception>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new byte[] { 4, 5, 6 };

        server.MessageReceived += async (_, args) =>
        {
            try
            {
                await args.Connection.WriteAsync(response, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _ = replyFailureSource.TrySetResult(exception);
            }
        };
        client.MessageReceived += (_, args) =>
        {
            _ = receivedSource.TrySetResult(args.Message);
        };

        await server.StartAsync(cancellationToken).ConfigureAwait(false);
        await client.ConnectAsync(cancellationToken).ConfigureAwait(false);
        await client.WriteAsync(new byte[] { 1, 2, 3 }, cancellationToken).ConfigureAwait(false);

        var observed = await Task.WhenAny(
            receivedSource.Task,
            replyFailureSource.Task,
            Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken)).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        if (ReferenceEquals(observed, replyFailureSource.Task))
        {
            throw new InvalidOperationException(
                "The immediate secondary-pipe response failed.",
                await replyFailureSource.Task.ConfigureAwait(false));
        }

        CollectionAssert.AreEqual(response, await receivedSource.Task.ConfigureAwait(false));
    }

    /// <summary>
    /// Asynchronously verifies that a failed handshake from an older connection cannot disconnect its replacement.
    /// </summary>
    [TestMethod]
    public async Task EnableEncryption_WhenEarlierHandshakeFails_DoesNotDisconnectReplacementConnection()
    {
#if !NETFRAMEWORK
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("Inferno key exchange requires Windows CNG.");
            return;
        }
#endif

        await VerifyStaleHandshakeFailureAsync().ConfigureAwait(false);
    }

#if NET8_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    private static async Task VerifyStaleHandshakeFailureAsync()
    {
        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var cancellationToken = timeoutSource.Token;
        var pipeName = $"inferno-stale-{Guid.NewGuid():N}";

        await using var server = new PipeServer<string>(
            pipeName,
            new SystemTextJsonFormatter());
        await using var firstClient = new PipeClient<string>(
            pipeName,
            new SystemTextJsonFormatter())
        {
            AutoReconnect = false,
        };
        await using var secondClient = new PipeClient<string>(
            pipeName,
            new SystemTextJsonFormatter())
        {
            AutoReconnect = false,
        };

        await server.StartAsync(cancellationToken).ConfigureAwait(false);
        await firstClient.ConnectAsync(cancellationToken).ConfigureAwait(false);

        var original = firstClient.Connection ??
            throw new InvalidOperationException("The original pipe connection was not established.");

        var client = new RotatingPipeClient(pipeName, original);
        var handshakeFailureSource = new TaskCompletionSource<Exception>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        client.EnableEncryption(exception =>
        {
            _ = handshakeFailureSource.TrySetResult(exception);
        });

        await using var secondary = new SingleConnectionPipeServer<byte[]>(
            $"{original.PipeName}_Inferno",
            new SystemTextJsonFormatter());
        var publicKeyConnectionSource = new TaskCompletionSource<PipeConnection<byte[]>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        secondary.MessageReceived += (_, args) =>
        {
            _ = publicKeyConnectionSource.TrySetResult(args.Connection);
        };

        await secondary.StartAsync(cancellationToken).ConfigureAwait(false);
        client.RaiseConnected();

        // Keep the first exchange pending until the replacement connection is active.
        var responseConnection = await WaitOrCancelAsync(
            publicKeyConnectionSource.Task,
            cancellationToken).ConfigureAwait(false);

        await firstClient.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        await secondClient.ConnectAsync(cancellationToken).ConfigureAwait(false);

        var replacement = secondClient.Connection ??
            throw new InvalidOperationException("The replacement pipe connection was not established.");
        client.ReplaceConnection(replacement);

        // An invalid public-key blob makes the first handshake fail immediately, without a timed race.
        await responseConnection.WriteAsync(new byte[] { 1 }, cancellationToken).ConfigureAwait(false);
        _ = await WaitOrCancelAsync(handshakeFailureSource.Task, cancellationToken).ConfigureAwait(false);

        Assert.AreSame(
            replacement,
            client.Connection,
            "An old handshake failure must not disconnect the current connection.");
        Assert.AreEqual(
            0,
            client.DisconnectCount,
            "The failed handshake belongs to a previous connection generation.");
    }

    private static async Task<T> WaitOrCancelAsync<T>(
        Task<T> task,
        CancellationToken cancellationToken)
    {
        var canceledSource = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() =>
        {
            _ = canceledSource.TrySetResult(true);
        });

        _ = await Task.WhenAny(task, canceledSource.Task).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return await task.ConfigureAwait(false);
    }

    private sealed class RotatingPipeClient : IPipeClient<string>
    {
        private PipeConnection<string>? _connection;
        private int _disconnectCount;

        public RotatingPipeClient(string pipeName, PipeConnection<string> connection)
        {
            PipeName = pipeName;
            Formatter = connection.Formatter;
            _connection = connection;
        }

        public IFormatter Formatter { get; }

        public string PipeName { get; }

        public string ServerName => ".";

        public Func<string, string, NamedPipeClientStream>? CreatePipeStreamFunc { get; set; }

        public bool AutoReconnect { get; set; }

        public TimeSpan ReconnectionInterval => TimeSpan.FromMilliseconds(100);

        public bool IsConnected => Connection is { IsConnected: true };

        public bool IsConnecting => false;

        public PipeConnection<string>? Connection => Volatile.Read(ref _connection);

        public int DisconnectCount => Volatile.Read(ref _disconnectCount);

        public event EventHandler<ConnectionEventArgs<string>>? Connected;

        public event EventHandler<ConnectionEventArgs<string>>? Disconnected
        {
            add { }
            remove { }
        }

        public event EventHandler<ConnectionMessageEventArgs<string?>>? MessageReceived
        {
            add { }
            remove { }
        }

        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred
        {
            add { }
            remove { }
        }

        public void RaiseConnected()
        {
            var connection = Connection ??
                throw new InvalidOperationException("No connection is available.");

            Connected?.Invoke(this, new ConnectionEventArgs<string>(connection));
        }

        public void ReplaceConnection(PipeConnection<string> connection)
        {
            Volatile.Write(ref _connection, connection);
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            _ = Interlocked.Increment(ref _disconnectCount);
            Volatile.Write(ref _connection, null);
            return Task.CompletedTask;
        }

        public Task WriteAsync(string value, CancellationToken cancellationToken = default)
        {
            var connection = Connection ??
                throw new InvalidOperationException("No connection is available.");

            return connection.WriteAsync(value, cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }
    }
}
