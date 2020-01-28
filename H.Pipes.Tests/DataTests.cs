using System;
using System.Collections.Generic;
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
        public async Task NullTest()
        {
            await BaseTests.DataSingleTestAsync(new List<string?>{ null });
            await BaseTests.DataSingleTestAsync(new List<string?> { null }, formatter: new JsonFormatter());
            await BaseTests.DataSingleTestAsync(new List<string?> { null }, formatter: new WireFormatter());
        }

        [TestMethod]
        public async Task TestEmptyMessageDoesNotDisconnectClient()
        {
            await BaseTests.BinaryDataTestAsync(0);
        }

        [TestMethod]
        public async Task TestMessageSize1B()
        {
            await BaseTests.BinaryDataTestAsync(1);
        }

        [TestMethod]
        public async Task TestMessageSize2B()
        {
            await BaseTests.BinaryDataTestAsync(2);
        }

        [TestMethod]
        public async Task TestMessageSize3B()
        {
            await BaseTests.BinaryDataTestAsync(3);
        }

        [TestMethod]
        public async Task TestMessageSize9B()
        {
            await BaseTests.BinaryDataTestAsync(9);
        }

        [TestMethod]
        public async Task TestMessageSize33B()
        {
            await BaseTests.BinaryDataTestAsync(33);
        }

        [TestMethod]
        public async Task TestMessageSize1Kx3_JSON()
        {
            await BaseTests.BinaryDataTestAsync(1025, 3, new JsonFormatter());
        }

        [TestMethod]
        public async Task TestMessageSize1Kx3_Wire()
        {
            await BaseTests.BinaryDataTestAsync(1025, 3, new WireFormatter());
        }

        [TestMethod]
        public async Task TestMessageSize129B()
        {
            await BaseTests.BinaryDataTestAsync(129);
        }

        [TestMethod]
        public async Task TestMessageSize1K()
        {
            await BaseTests.BinaryDataTestAsync(1025);
        }

        [TestMethod]
        public async Task TestMessageSize1M()
        {
            await BaseTests.BinaryDataTestAsync(1024 * 1024 + 1);
        }

        [TestMethod]
        public async Task TestMessageSize300Mx3()
        {
            await BaseTests.BinaryDataTestAsync(1024 * 1024 * 300 + 1, 3, timeout: TimeSpan.FromMinutes(1));
        }

        [TestMethod]
        public async Task Single_TestEmptyMessageDoesNotDisconnectClient()
        {
            await BaseTests.BinaryDataSingleTestAsync(0);
        }

        [TestMethod]
        public async Task Single_TestMessageSize1B()
        {
            await BaseTests.BinaryDataSingleTestAsync(1);
        }

        [TestMethod]
        public async Task Single_TestMessageSize1Kx3_JSON()
        {
            await BaseTests.BinaryDataSingleTestAsync(1025, 3, new JsonFormatter());
        }

        [TestMethod]
        public async Task Single_TestMessageSize1Kx3_Wire()
        {
            await BaseTests.BinaryDataSingleTestAsync(1025, 3, new WireFormatter());
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
