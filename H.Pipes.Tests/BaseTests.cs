using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using H.Formatters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Pipes.Tests
{
    public static class BaseTests
    {
        public static async Task DataTestAsync(IPipeServer<byte[]> server, IPipeClient<byte[]> client, int numBytes, int count = 1, CancellationToken cancellationToken = default)
        {
            Trace.WriteLine("Setting up test...");

            var completionSource = new TaskCompletionSource<bool>(false);
            // ReSharper disable once AccessToModifiedClosure
            cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));

            var actualHash = (string?)null;
            var clientDisconnected = false;

            server.ClientConnected += (sender, args) =>
            {
                Trace.WriteLine("Client connected");
            };
            server.ClientDisconnected += (sender, args) =>
            {
                Trace.WriteLine("Client disconnected");
                clientDisconnected = true;

                // ReSharper disable once AccessToModifiedClosure
                completionSource.TrySetResult(true);
            };
            server.MessageReceived += (sender, args) =>
            {
                Trace.WriteLine($"Received {args.Message.Length} bytes from the client");
                actualHash = Hash(args.Message);

                // ReSharper disable once AccessToModifiedClosure
                completionSource.TrySetResult(true);
            };
            server.ExceptionOccurred += (sender, args) =>
            {
                Trace.WriteLine($"Server exception occurred: {args.Exception}");

                // ReSharper disable once AccessToModifiedClosure
                completionSource.TrySetException(args.Exception);
            };
            client.Connected += (sender, args) => Trace.WriteLine("Client_OnConnected");
            client.Disconnected += (sender, args) => Trace.WriteLine("Client_OnDisconnected");
            client.MessageReceived += (sender, args) => Trace.WriteLine("Client_OnMessageReceived");
            client.ExceptionOccurred += (sender, args) =>
            {
                Trace.WriteLine($"Client exception occurred: {args.Exception}");

                // ReSharper disable once AccessToModifiedClosure
                completionSource.TrySetException(args.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is Exception exception)
                {
                    // ReSharper disable once AccessToModifiedClosure
                    completionSource.TrySetException(exception);
                }
            };

            server.ExceptionOccurred += (sender, args) => Trace.WriteLine(args.Exception.ToString());
            client.ExceptionOccurred += (sender, args) => Trace.WriteLine(args.Exception.ToString());

            await server.StartAsync(false, cancellationToken).ConfigureAwait(false);
            await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

            Trace.WriteLine("Client and server started");
            Trace.WriteLine("---");

            var watcher = Stopwatch.StartNew();

            for (var i = 0; i < count; i++)
            {
                Trace.WriteLine($"Generating {numBytes} bytes of random data...");

                // Generate some random data and compute its SHA-1 hash
                var data = new byte[numBytes];
                new Random().NextBytes(data);

                Trace.WriteLine($"Computing SHA-1 hash for {numBytes} bytes of data...");

                var expectedHash = Hash(data);

                Trace.WriteLine($"Sending {numBytes} bytes of data to the client...");

                await client.WriteAsync(data, cancellationToken).ConfigureAwait(false);

                Trace.WriteLine($"Finished sending {numBytes} bytes of data to the client");

                await completionSource.Task.ConfigureAwait(false);
                
                Assert.IsNotNull(actualHash, "Server should have received a zero-byte message from the client");
                Assert.AreEqual(expectedHash, actualHash, "SHA-1 hashes for zero-byte message should match");
                Assert.IsFalse(clientDisconnected, "Server should not disconnect the client for explicitly sending zero-length data");

                Trace.WriteLine("---");
                
                completionSource = new TaskCompletionSource<bool>(false);
            }

            Trace.WriteLine($"Test took {watcher.Elapsed}");
            Trace.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~");
        }

        public static async Task DataTestAsync(int numBytes, int count = 1, IFormatter? formatter = default, TimeSpan? timeout = default)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));

            const string pipeName = "data_test_pipe";
            await using var server = new PipeServer<byte[]>(pipeName, formatter ?? new BinaryFormatter());
            await using var client = new PipeClient<byte[]>(pipeName, formatter: formatter ?? new BinaryFormatter());

            await DataTestAsync(server, client, numBytes, count, cancellationTokenSource.Token);
        }

        public static async Task DataSingleTestAsync(int numBytes, int count = 1, IFormatter? formatter = default, TimeSpan? timeout = default)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));

            const string pipeName = "data_test_pipe";
            await using var server = new SingleConnectionPipeServer<byte[]>(pipeName, formatter ?? new BinaryFormatter());
            await using var client = new SingleConnectionPipeClient<byte[]>(pipeName, formatter: formatter ?? new BinaryFormatter());

            await DataTestAsync(server, client, numBytes, count, cancellationTokenSource.Token);
        }

        #region Helper methods

        /// <summary>
        /// Computes the SHA-1 hash (lowercase) of the specified byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static string Hash(byte[] bytes)
        {
            using var sha1 = System.Security.Cryptography.SHA1.Create();

            var hash = sha1.ComputeHash(bytes);
            var sb = new StringBuilder();
            foreach (var @byte in hash)
            {
                sb.Append(@byte.ToString("x2"));
            }
            return sb.ToString();
        }

#endregion
    }
}
