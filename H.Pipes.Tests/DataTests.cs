using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Pipes.Tests
{
    [TestClass]
    public class DataTests
    {
        private static async Task BaseTestAsync(int numBytes, int count = 1, CancellationToken cancellationToken = default)
        {
            Trace.WriteLine("Setting up test...");

            var completionSource = new TaskCompletionSource<bool>(false);
            // ReSharper disable once AccessToModifiedClosure
            cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));

            const string pipeName = "data_test_pipe";
            var server = new PipeServer<byte[]>(pipeName);
            var client = new PipeClient<byte[]>(pipeName);

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

            await server.StartAsync(cancellationToken: cancellationToken);
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

            Trace.WriteLine("Disposing client and server...");

            await server.DisposeAsync();
            await client.DisposeAsync();

            Trace.WriteLine("Client and server stopped");
            Trace.WriteLine($"Test took {watcher.Elapsed}");
            Trace.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~");
        }

        #region Tests

        [TestMethod]
        public async Task DisposeTest()
        {
            {
                try
                {
                    using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                    using var pipe = await PipeServerFactory.CreateAndWaitAsync("test", source.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                }
            }

            {
                using var pipe = PipeServerFactory.Create("test");
            }
        }

        [TestMethod]
        public async Task StartDisposeStart()
        {
            {
                await using var pipe = new PipeServer<string>("test");
                await pipe.StartAsync();
            }
            {
                await using var pipe = new PipeServer<string>("test");
                await pipe.StartAsync();
            }
        }

        [TestMethod]
        public async Task DoubleStartWithSameName()
        {
            await using var pipe1 = new PipeServer<string>("test");
            await pipe1.StartAsync();
            await using var pipe2 = new PipeServer<string>("test");

            await Assert.ThrowsExceptionAsync<IOException>(async () => await pipe2.StartAsync(false));
        }

        [TestMethod]
        public async Task StartStopStart()
        {
            await using var pipe = new PipeServer<string>("test");
            await pipe.StartAsync();
            await pipe.StopAsync();
            await pipe.StartAsync();
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
        public async Task TestMessageSize2B()
        {
            await BaseTestAsync(2);
        }

        [TestMethod]
        public async Task TestMessageSize3B()
        {
            await BaseTestAsync(3);
        }

        [TestMethod]
        public async Task TestMessageSize9B()
        {
            await BaseTestAsync(9);
        }

        [TestMethod]
        public async Task TestMessageSize33B()
        {
            await BaseTestAsync(33);
        }

        [TestMethod]
        public async Task TestMessageSize129B()
        {
            await BaseTestAsync(129);
        }

        [TestMethod]
        public async Task TestMessageSize1K()
        {
            await BaseTestAsync(1025);
        }

        [TestMethod]
        public async Task TestMessageSize1M()
        {
            await BaseTestAsync(1024 * 1024 + 1);
        }

        [TestMethod]
        public async Task TestMessageSize100M()
        {
            await BaseTestAsync(1024 * 1024 * 100 + 1);
        }

        [TestMethod]
        public async Task TestMessageSize200M()
        {
            await BaseTestAsync(1024 * 1024 * 200 + 1);
        }

        [TestMethod]
        public async Task TestMessageSize300M()
        {
            await BaseTestAsync(1024 * 1024 * 300 + 1);
        }

        [TestMethod]
        public async Task TestMessageSize100Mx3()
        {
            await BaseTestAsync(1024 * 1024 * 100 + 1, 3);
        }

        [TestMethod]
        public async Task TestMessageSize300Mx3()
        {
            await BaseTestAsync(1024 * 1024 * 300 + 1, 3);
        }

        #endregion

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
