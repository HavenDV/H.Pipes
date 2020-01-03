using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExampleCLI
{
    class Program
    {
        private const string DefaultPipeName = "MyServerPipe";

        static void Main(string[] args)
        {
            if (args.Length >= 1 && string.Equals("/server", args[0], StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Running in SERVER mode");
                Console.WriteLine("Press 'q' to exit");
                new MyServer(DefaultPipeName);
            }
            else
            {
                Console.WriteLine("Running in CLIENT mode");
                Console.WriteLine("Press 'q' to exit");
                new MyClient(DefaultPipeName);
            }
        }
    }
}
