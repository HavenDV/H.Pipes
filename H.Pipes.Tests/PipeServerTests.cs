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
                    using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
#if NETCOREAPP2_0
                    using var pipe = await PipeServerFactory.CreateAndWaitAsync("test", source.Token).ConfigureAwait(false);
#else
                    await using var pipe = await PipeServerFactory.CreateAndWaitAsync("test", source.Token).ConfigureAwait(false);
#endif
                }
                catch (TaskCanceledException)
                {
                }
            }

            {
#if NETCOREAPP2_0
                using var pipe = PipeServerFactory.Create("test");
#else
                await using var pipe = PipeServerFactory.Create("test");
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
        public async Task DoubleStartWithSameName_CommonDispose()
        {
            // ReSharper disable once UseAwaitUsing
            using var pipe1 = new PipeServer<string>("test");
            await pipe1.StartAsync();

            // ReSharper disable once UseAwaitUsing
            using var pipe2 = new PipeServer<string>("test");

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
