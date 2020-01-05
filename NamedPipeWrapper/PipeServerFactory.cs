using System.IO.Pipes;

namespace NamedPipeWrapper
{
    internal static class PipeServerFactory
    {
        public static NamedPipeServerStream CreateAndConnectPipe(string pipeName)
        {
            var pipe = CreatePipe(pipeName);
            pipe.WaitForConnection();

            return pipe;
        }

        public static NamedPipeServerStream CreatePipe(string pipeName)
        {
            return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough, 0, 0);
        }
    }
}