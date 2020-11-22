using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Pipes.Tests
{
    [TestClass]
    public class PipeServerTests
    {
        [TestMethod]
        public async Task DisposeTest()
        {
            {
                try
                {
                    using var source = new CancellationTokenSource(TimeSpan.FromSeconds(1));
#if NETCOREAPP3_1
                    await using var pipe = await PipeServerFactory.CreateAndWaitAsync("test", source.Token).ConfigureAwait(false);
#else
                    using var pipe = await PipeServerFactory.CreateAndWaitAsync("test", source.Token).ConfigureAwait(false);
#endif
                }
                catch (OperationCanceledException)
                {
                }
            }

            {
#if NETCOREAPP3_1
                await using var pipe = PipeServerFactory.Create("test");
#else
                using var pipe = PipeServerFactory.Create("test");
#endif
            }
        }

        [TestMethod]
        public async Task StartDisposeStart()
        {
            {
                await using var pipe = new PipeServer<string>("test");
                await pipe.StartAsync();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(1));

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

            await Assert.ThrowsExceptionAsync<IOException>(async () => await pipe2.StartAsync());
        }

        [TestMethod]
        public async Task StartStopStart()
        {
            await using var pipe = new PipeServer<string>("test");

            await pipe.StartAsync();
            await pipe.StopAsync();
            await pipe.StartAsync();
        }
    }
}
