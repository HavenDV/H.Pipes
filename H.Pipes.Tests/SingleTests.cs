using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using H.Formatters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Pipes.Tests
{
    [TestClass]
    public class SingleTests
    {
        private static async Task BaseTestAsync(int numBytes, int count = 1, IFormatter formatter = default, CancellationToken cancellationToken = default)
        {
            Trace.WriteLine("Setting up test...");

            var completionSource = new TaskCompletionSource<bool>(false);
            // ReSharper disable once AccessToModifiedClosure
            cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));

            const string pipeName = "data_test_pipe";
            await using var server = new SingleConnectionPipeServer<byte[]>(pipeName, formatter ?? new BinaryFormatter());
            await using var client = new SingleConnectionPipeClient<byte[]>(pipeName, formatter: formatter ?? new BinaryFormatter());

            var actualHash = (string)null;
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

            server.ExceptionOccurred += (sender, args) => Trace.WriteLine(args.Exception.ToString());
            client.ExceptionOccurred += (sender, args) => Trace.WriteLine(args.Exception.ToString());

            await server.StartAsync(true, cancellationToken: cancellationToken);
            await client.ConnectAsync(cancellationToken);

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

                await client.WriteAsync(data, cancellationToken);

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

        [TestMethod]
        public async Task StartDisposeStart()
        {
            {
                await using var pipe = new SingleConnectionPipeServer<string>("test");
                await pipe.StartAsync();
            }
            {
                await using var pipe = new SingleConnectionPipeServer<string>("test");
                await pipe.StartAsync(false);
            }
        }

        [TestMethod]
        public async Task DoubleStartWithSameName()
        {
            await using var pipe1 = new SingleConnectionPipeServer<string>("test");
            await pipe1.StartAsync();
            await using var pipe2 = new SingleConnectionPipeServer<string>("test");

            await Assert.ThrowsExceptionAsync<IOException>(async () => await pipe2.StartAsync(false));
        }

        [TestMethod]
        public async Task ClientConnectTest()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await using var client = new SingleConnectionPipeClient<string>("this_pipe_100%_is_not_exists");

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => await client.ConnectAsync(cancellationTokenSource.Token));
        }

        [TestMethod]
        public void PipeExistsTest()
        {
            Assert.IsFalse(PipeWatcher.IsExists("this_pipe_100%_is_not_exists"));
        }

        [TestMethod]
        public async Task DoubleStartWithSameName_CommonDispose()
        {
            using var pipe1 = new SingleConnectionPipeServer<string>("test");
            await pipe1.StartAsync();
            using var pipe2 = new SingleConnectionPipeServer<string>("test");

            await Assert.ThrowsExceptionAsync<IOException>(async () => await pipe2.StartAsync(false));
        }

        [TestMethod]
        public async Task StartStopStart()
        {
            await using var pipe = new SingleConnectionPipeServer<string>("test");
            await pipe.StartAsync();
            await pipe.StopAsync();
            await pipe.StartAsync(false);
        }

        [TestMethod]
        public async Task TestEmptyMessageDoesNotDisconnectClient()
        {
            await BaseTestAsync(0);
        }

        [TestMethod]
        public async Task TestMessageSize1B()
        {
            await BaseTestAsync(1);
        }

        [TestMethod]
        public async Task TestMessageSize1Kx3_JSON()
        {
            await BaseTestAsync(1025, 3, formatter: new JsonFormatter());
        }

        [TestMethod]
        public async Task TestMessageSize1Kx3_Wire()
        {
            await BaseTestAsync(1025, 3, formatter: new WireFormatter());
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
