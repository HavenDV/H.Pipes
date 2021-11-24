using ConsoleApp;

const string DefaultPipeName = "named_pipe_test_server";

string? mode;
string pipeName = DefaultPipeName;
if (args.Any())
{
    mode = args.ElementAt(0);
    pipeName = args.ElementAtOrDefault(1) ?? DefaultPipeName;
}
else
{
    Console.WriteLine("Enter mode('server' or 'client'):");
    mode = await Console.In.ReadLineAsync().ConfigureAwait(false);
}

switch (mode?.ToUpperInvariant())
{
    case "SERVER":
        await MyServer.RunAsync(pipeName).ConfigureAwait(false);
        break;

    default:
        await MyClient.RunAsync(pipeName).ConfigureAwait(false);
        break;
}
