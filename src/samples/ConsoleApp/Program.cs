using System;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace ConsoleApp
{
    internal class Program
    {
        private const string DefaultPipeName = "named_pipe_test_server";

        private static async Task Main(string[] arguments)
        {
            string? mode;
            string pipeName = DefaultPipeName;
            if (arguments.Any())
            {
                mode = arguments.ElementAt(0);
                pipeName = arguments.ElementAtOrDefault(1);
            }
            else
            {
                Console.WriteLine("Enter mode('server' or 'client'):");
                mode = await Console.In.ReadLineAsync().ConfigureAwait(false);
            }

            switch (mode?.ToUpperInvariant())
            {
                case "SERVER":
                    await MyServer.RunAsync(pipeName);
                    break;

                default:
                    await MyClient.RunAsync(pipeName);
                    break;
            }
        }
    }
}
