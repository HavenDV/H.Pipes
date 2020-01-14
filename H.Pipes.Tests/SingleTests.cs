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
            await BaseTests.DataSingleTestAsync(0, 1, null, TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public async Task TestMessageSize1B()
        {
            await BaseTests.DataSingleTestAsync(1, 1, null, TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public async Task TestMessageSize1Kx3_JSON()
        {
            await BaseTests.DataSingleTestAsync(1025, 3, formatter: new JsonFormatter(), TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public async Task TestMessageSize1Kx3_Wire()
        {
            await BaseTests.DataSingleTestAsync(1025, 3, formatter: new WireFormatter(), TimeSpan.FromSeconds(1));
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
