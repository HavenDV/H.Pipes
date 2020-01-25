using System;
using System.Threading;
using System.Threading.Tasks;
using H.Formatters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Pipes.Tests
{
    [TestClass]
    public class DataTests
    {
        [TestMethod]
        public async Task TestEmptyMessageDoesNotDisconnectClient()
        {
            await BaseTests.DataTestAsync(0);
        }

        [TestMethod]
        public async Task TestMessageSize1B()
        {
            await BaseTests.DataTestAsync(1);
        }

        [TestMethod]
        public async Task TestMessageSize2B()
        {
            await BaseTests.DataTestAsync(2);
        }

        [TestMethod]
        public async Task TestMessageSize3B()
        {
            await BaseTests.DataTestAsync(3);
        }

        [TestMethod]
        public async Task TestMessageSize9B()
        {
            await BaseTests.DataTestAsync(9);
        }

        [TestMethod]
        public async Task TestMessageSize33B()
        {
            await BaseTests.DataTestAsync(33);
        }

        [TestMethod]
        public async Task TestMessageSize1Kx3_JSON()
        {
            await BaseTests.DataTestAsync(1025, 3, new JsonFormatter());
        }

        [TestMethod]
        public async Task TestMessageSize1Kx3_Wire()
        {
            await BaseTests.DataTestAsync(1025, 3, new WireFormatter());
        }

        [TestMethod]
        public async Task TestMessageSize129B()
        {
            await BaseTests.DataTestAsync(129);
        }

        [TestMethod]
        public async Task TestMessageSize1K()
        {
            await BaseTests.DataTestAsync(1025);
        }

        [TestMethod]
        public async Task TestMessageSize1M()
        {
            await BaseTests.DataTestAsync(1024 * 1024 + 1);
        }

        [TestMethod]
        public async Task TestMessageSize300Mx3()
        {
            await BaseTests.DataTestAsync(1024 * 1024 * 300 + 1, 3, timeout: TimeSpan.FromMinutes(1));
        }

        [TestMethod]
        public async Task Single_TestEmptyMessageDoesNotDisconnectClient()
        {
            await BaseTests.DataSingleTestAsync(0);
        }

        [TestMethod]
        public async Task Single_TestMessageSize1B()
        {
            await BaseTests.DataSingleTestAsync(1);
        }

        [TestMethod]
        public async Task Single_TestMessageSize1Kx3_JSON()
        {
            await BaseTests.DataSingleTestAsync(1025, 3, new JsonFormatter());
        }

        [TestMethod]
        public async Task Single_TestMessageSize1Kx3_Wire()
        {
            await BaseTests.DataSingleTestAsync(1025, 3, new WireFormatter());
        }

        [TestMethod]
        public async Task TypeTest()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var completionSource = new TaskCompletionSource<bool>(false);
            cancellationTokenSource.Token.Register(() => completionSource.TrySetCanceled());

            const string pipeName = "data_test_pipe";
            var formatter = new BinaryFormatter();
            //var type = typeof(Exception);
            await using var server = new PipeServer<object>(pipeName, formatter);
            await using var client = new PipeClient<object>(pipeName, formatter: formatter);

            server.ExceptionOccurred += (sender, args) => Assert.Fail(args.Exception.ToString());
            client.ExceptionOccurred += (sender, args) => Assert.Fail(args.Exception.ToString());
            client.MessageReceived += (sender, args) =>
            {
                Console.WriteLine($"MessageReceived: {args.Message as Exception}");

                completionSource.TrySetResult(args.Message is Exception);
            };

            await server.StartAsync(cancellationToken: cancellationTokenSource.Token);

            await client.ConnectAsync(cancellationTokenSource.Token);

            await server.WriteAsync(new Exception("Hello. It's server message"), cancellationTokenSource.Token);

            Assert.IsTrue(await completionSource.Task);
        }
    }
}
