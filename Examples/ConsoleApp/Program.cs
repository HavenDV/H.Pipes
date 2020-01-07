using System;
using System.Threading.Tasks;

namespace ConsoleApp
{
    internal class Program
    {
        private const string DefaultPipeName = "named_pipe_test_server";

        private static async Task Main()
        {
            Console.WriteLine("Enter mode('server' or 'client'):");
            var mode = await Console.In.ReadLineAsync().ConfigureAwait(false);

            switch (mode?.ToUpperInvariant())
            {
                case "SERVER":
                    await MyServer.RunAsync(DefaultPipeName);
                    break;

                default:
                    await MyClient.RunAsync(DefaultPipeName);
                    break;
            }
        }
    }
}
