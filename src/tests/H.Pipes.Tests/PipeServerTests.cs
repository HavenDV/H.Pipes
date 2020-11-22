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
                    using var source = new CancellationTokenSource(TimeSpan.FromMinutes(1));
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
#if NETCOREAPP3_1
                await using var pipe = new PipeServer<string>("test");
#else
                using var pipe = new PipeServer<string>("test");
#endif

                await pipe.StartAsync();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(1));

            {
#if NETCOREAPP3_1
                await using var pipe = new PipeServer<string>("test");
#else
                using var pipe = new PipeServer<string>("test");
#endif

                await pipe.StartAsync();
            }
        }

        [TestMethod]
        public async Task DoubleStartWithSameName()
        {
#if NETCOREAPP3_1
            await using var pipe1 = new PipeServer<string>("test");
#else
            using var pipe1 = new PipeServer<string>("test");
#endif

            await pipe1.StartAsync();

#if NETCOREAPP3_1
            await using var pipe2 = new PipeServer<string>("test");
#else
            using var pipe2 = new PipeServer<string>("test");
#endif

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
#if NETCOREAPP3_1
            await using var pipe = new PipeServer<string>("test");
#else
            using var pipe = new PipeServer<string>("test");
#endif

            await pipe.StartAsync();
            await pipe.StopAsync();
            await pipe.StartAsync();
        }
    }
}
