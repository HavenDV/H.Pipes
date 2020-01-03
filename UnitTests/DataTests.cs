using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace NamedPipeWrapper.Tests
{
    [TestFixture]
    internal class DataTests
    {
        private const string PipeName = "data_test_pipe";

        private NamedPipeServer<byte[]> _server;
        private NamedPipeClient<byte[]> _client;

        private string _expectedHash;
        private string _actualHash;
        private bool _clientDisconnected;

        private DateTime _startTime;

        private readonly ManualResetEvent _barrier = new ManualResetEvent(false);

        #region Setup and teardown

        [SetUp]
        public void SetUp()
        {
            Trace.WriteLine("Setting up test...");

            _barrier.Reset();

            _server = new NamedPipeServer<byte[]>(PipeName);
            _client = new NamedPipeClient<byte[]>(PipeName);

            _expectedHash = null;
            _actualHash = null;
            _clientDisconnected = false;

            _server.ClientDisconnected += ServerOnClientDisconnected;
            _server.ClientMessage += ServerOnClientMessage;

            _server.Error += ServerOnError;
            _client.Error += ClientOnError;

            _server.Start();
            _client.Start();

            // Give the client and server a few seconds to connect before sending data
            Thread.Sleep(TimeSpan.FromSeconds(1));

            Trace.WriteLine("Client and server started");
            Trace.WriteLine("---");

            _startTime = DateTime.Now;
        }

        private void ServerOnError(Exception exception)
        {
            throw new NotImplementedException();
        }

        private void ClientOnError(Exception exception)
        {
            throw new NotImplementedException();
        }

        [TearDown]
        public void TearDown()
        {
            Trace.WriteLine("---");
            Trace.WriteLine("Stopping client and server...");

            _server.Stop();
            _client.Stop();

            _server.ClientDisconnected -= ServerOnClientDisconnected;
            _server.ClientMessage -= ServerOnClientMessage;

            _server.Error -= ServerOnError;
            _client.Error -= ClientOnError;

            Trace.WriteLine("Client and server stopped");
            Trace.WriteLine($"Test took {DateTime.Now - _startTime}");
            Trace.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~");
        }

        #endregion

        #region Events

        private void ServerOnClientDisconnected(NamedPipeConnection<byte[], byte[]> connection)
        {
            Trace.WriteLine("Client disconnected");
            _clientDisconnected = true;
            _barrier.Set();
        }

        private void ServerOnClientMessage(NamedPipeConnection<byte[], byte[]> connection, byte[] message)
        {
            Trace.WriteLine($"Received {message.Length} bytes from the client");
            _actualHash = Hash(message);
            _barrier.Set();
        }

        #endregion

        #region Tests

        [Test]
        public void TestEmptyMessageDoesNotDisconnectClient()
        {
            SendMessageToServer(0);
            _barrier.WaitOne(TimeSpan.FromSeconds(2));
            Assert.NotNull(_actualHash, "Server should have received a zero-byte message from the client");
            Assert.AreEqual(_expectedHash, _actualHash, "SHA-1 hashes for zero-byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should not disconnect the client for explicitly sending zero-length data");
        }

        [Test]
        public void TestMessageSize1B()
        {
            const int numBytes = 1;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize2B()
        {
            const int numBytes = 2;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize3B()
        {
            const int numBytes = 3;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize9B()
        {
            const int numBytes = 9;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize33B()
        {
            const int numBytes = 33;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize129B()
        {
            const int numBytes = 129;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize1K()
        {
            const int numBytes = 1025;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize1M()
        {
            const int numBytes = 1024 * 1024 + 1;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize100M()
        {
            const int numBytes = 1024 * 1024 * 100 + 1;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize200M()
        {
            const int numBytes = 1024 * 1024 * 200 + 1;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize300M()
        {
            const int numBytes = 1024 * 1024 * 300 + 1;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize100Mx3()
        {
            const int numBytes = 1024 * 1024 * 100 + 1;

            _barrier.Reset();
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");

            Trace.WriteLine("...");

            _barrier.Reset();
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");

            Trace.WriteLine("...");

            _barrier.Reset();
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize300Mx3()
        {
            const int numBytes = 1024 * 1024 * 300 + 1;

            _barrier.Reset();
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");

            Trace.WriteLine("...");

            _barrier.Reset();
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");

            Trace.WriteLine("...");

            _barrier.Reset();
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, $"Server should have received client's {numBytes} byte message");
            Assert.AreEqual(_expectedHash, _actualHash, $"SHA-1 hashes for {numBytes} byte message should match");
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        #endregion

        #region Helper methods

        private void SendMessageToServer(int numBytes)
        {
            Trace.WriteLine($"Generating {numBytes} bytes of random data...");

            // Generate some random data and compute its SHA-1 hash
            var data = new byte[numBytes];
            new Random().NextBytes(data);

            Trace.WriteLine($"Computing SHA-1 hash for {numBytes} bytes of data...");

            _expectedHash = Hash(data);

            Trace.WriteLine($"Sending {numBytes} bytes of data to the client...");

            _client.PushMessage(data);

            Trace.WriteLine($"Finished sending {numBytes} bytes of data to the client");
        }

        /// <summary>
        /// Computes the SHA-1 hash (lowercase) of the specified byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static string Hash(byte[] bytes)
        {
            using var sha1 = System.Security.Cryptography.SHA1.Create();

            var hash = sha1.ComputeHash(bytes);
            var sb = new StringBuilder();
            foreach (var @byte in hash)
            {
                sb.Append(@byte.ToString("x2"));
            }
            return sb.ToString();
        }

        #endregion
    }
}
