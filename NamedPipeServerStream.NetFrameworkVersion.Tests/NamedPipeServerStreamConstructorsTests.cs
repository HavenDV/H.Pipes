using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.IO.Pipes.Tests
{
    [TestClass]
    public class NamedPipeServerStreamConstructorsTests
    {
        [TestMethod]
        public void ExceptionTest()
        {
            var exception = Assert.ThrowsException<IOException>(() =>
            {
                using var stream1 = NamedPipeServerStreamConstructors.New(nameof(NamedPipeServerStreamConstructorsTests));
                using var stream2 = NamedPipeServerStreamConstructors.New(nameof(NamedPipeServerStreamConstructorsTests));
            });

            Console.WriteLine(exception.ToString());
        }

        [TestMethod]
        public void DisposeTest()
        {
            {
                using var stream = NamedPipeServerStreamConstructors.New(nameof(NamedPipeServerStreamConstructorsTests));
            }
            {
                using var stream = NamedPipeServerStreamConstructors.New(nameof(NamedPipeServerStreamConstructorsTests));
            }
        }
    }
}
