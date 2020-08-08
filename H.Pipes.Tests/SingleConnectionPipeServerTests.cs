using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Pipes.Tests
{
    [TestClass]
    public class SingleConnectionPipeServerTests
    {
        [TestMethod]
        public async Task StartDisposeStart()
        {
            {
#if NETCOREAPP3_1
                await using var pipe = new SingleConnectionPipeServer<string>("test");
#else
                using var pipe = new SingleConnectionPipeServer<string>("test");
#endif

                await pipe.StartAsync();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(1));

            {
#if NETCOREAPP3_1
                await using var pipe = new SingleConnectionPipeServer<string>("test");
#else
                using var pipe = new SingleConnectionPipeServer<string>("test");
#endif

                await pipe.StartAsync();
            }
        }

        [TestMethod]
        public async Task DoubleStartWithSameName()
        {
#if NETCOREAPP3_1
            await using var pipe1 = new SingleConnectionPipeServer<string>("test");
#else
            using var pipe1 = new SingleConnectionPipeServer<string>("test");
#endif

            await pipe1.StartAsync();

#if NETCOREAPP3_1
            await using var pipe2 = new SingleConnectionPipeServer<string>("test");
#else
            using var pipe2 = new SingleConnectionPipeServer<string>("test");
#endif

            await Assert.ThrowsExceptionAsync<IOException>(async () => await pipe2.StartAsync());
        }

        [TestMethod]
        public async Task DoubleStartWithSameName_CommonDispose()
        {
            // ReSharper disable once UseAwaitUsing
            using var pipe1 = new SingleConnectionPipeServer<string>("test");
            await pipe1.StartAsync();

            // ReSharper disable once UseAwaitUsing
            using var pipe2 = new SingleConnectionPipeServer<string>("test");

            await Assert.ThrowsExceptionAsync<IOException>(async () => await pipe2.StartAsync());
        }

        [TestMethod]
        public async Task StartStopStart()
        {
#if NETCOREAPP3_1
            await using var pipe = new SingleConnectionPipeServer<string>("test");
#else
            using var pipe = new SingleConnectionPipeServer<string>("test");
#endif

            await pipe.StartAsync();
            await pipe.StopAsync();
            await pipe.StartAsync();
        }
    }
}
