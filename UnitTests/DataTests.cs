using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using NamedPipeWrapper;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;

namespace UnitTests
{
    [TestFixture]
    class DataTests
    {
        private static readonly log4net.ILog Logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static DataTests()
        {
            var layout = new PatternLayout("%-6timestamp %-5level - %message%newline");
            var appender = new ConsoleAppender { Layout = layout };
            layout.ActivateOptions();
            appender.ActivateOptions();
            BasicConfigurator.Configure(appender);
        }

        private const string PipeName = "data_test_pipe";

        private NamedPipeServer<byte[]> _server;
        private NamedPipeClient<byte[]> _client;

        private byte[] _expectedData;
        private string _expectedHash;
        private byte[] _actualData;
        private string _actualHash;
        private bool _clientDisconnected;

        private DateTime _startTime;

        private readonly ManualResetEvent _barrier = new ManualResetEvent(false);

        #region Setup and teardown

        [SetUp]
        public void SetUp()
        {
            Logger.Debug("Setting up test...");

            _barrier.Reset();

            _server = new NamedPipeServer<byte[]>(PipeName);
            _client = new NamedPipeClient<byte[]>(PipeName);

            _expectedData = null;
            _expectedHash = null;
            _actualData = null;
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

            Logger.Debug("Client and server started");
            Logger.Debug("---");

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
            Logger.Debug("---");
            Logger.Debug("Stopping client and server...");

            _server.Stop();
            _client.Stop();

            _server.ClientDisconnected -= ServerOnClientDisconnected;
            _server.ClientMessage -= ServerOnClientMessage;

            _server.Error -= ServerOnError;
            _client.Error -= ClientOnError;

            Logger.Debug("Client and server stopped");
            Logger.DebugFormat("Test took {0}", (DateTime.Now - _startTime));
            Logger.Debug("~~~~~~~~~~~~~~~~~~~~~~~~~~");
        }

        #endregion

        #region Events

        private void ServerOnClientDisconnected(NamedPipeConnection<byte[], byte[]> connection)
        {
            Logger.Warn("Client disconnected");
            _clientDisconnected = true;
            _barrier.Set();
        }

        private void ServerOnClientMessage(NamedPipeConnection<byte[], byte[]> connection, byte[] message)
        {
            Logger.DebugFormat("Received {0} bytes from the client", message.Length);
            _actualData = message;
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
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize2B()
        {
            const int numBytes = 2;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize3B()
        {
            const int numBytes = 3;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize9B()
        {
            const int numBytes = 9;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize33B()
        {
            const int numBytes = 33;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize129B()
        {
            const int numBytes = 129;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize1K()
        {
            const int numBytes = 1025;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize1M()
        {
            const int numBytes = 1024 * 1024 + 1;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize100M()
        {
            const int numBytes = 1024 * 1024 * 100 + 1;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize200M()
        {
            const int numBytes = 1024 * 1024 * 200 + 1;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize300M()
        {
            const int numBytes = 1024 * 1024 * 300 + 1;
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize100Mx3()
        {
            const int numBytes = 1024 * 1024 * 100 + 1;

            _barrier.Reset();
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");

            Logger.Debug("...");

            _barrier.Reset();
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");

            Logger.Debug("...");

            _barrier.Reset();
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        [Test]
        public void TestMessageSize300Mx3()
        {
            const int numBytes = 1024 * 1024 * 300 + 1;

            _barrier.Reset();
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");

            Logger.Debug("...");

            _barrier.Reset();
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");

            Logger.Debug("...");

            _barrier.Reset();
            SendMessageToServer(numBytes);
            _barrier.WaitOne(TimeSpan.FromSeconds(20));
            Assert.NotNull(_actualHash, string.Format("Server should have received client's {0} byte message", numBytes));
            Assert.AreEqual(_expectedHash, _actualHash, string.Format("SHA-1 hashes for {0} byte message should match", numBytes));
            Assert.IsFalse(_clientDisconnected, "Server should still be connected to the client");
        }

        #endregion

        #region Helper methods

        private void SendMessageToServer(int numBytes)
        {
            Logger.DebugFormat("Generating {0} bytes of random data...", numBytes);

            // Generate some random data and compute its SHA-1 hash
            var data = new byte[numBytes];
            new Random().NextBytes(data);

            Logger.DebugFormat("Computing SHA-1 hash for {0} bytes of data...", numBytes);

            _expectedData = data;
            _expectedHash = Hash(data);

            Logger.DebugFormat("Sending {0} bytes of data to the client...", numBytes);

            _client.PushMessage(data);

            Logger.DebugFormat("Finished sending {0} bytes of data to the client", numBytes);
        }

        /// <summary>
        /// Computes the SHA-1 hash (lowercase) of the specified byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static string Hash(byte[] bytes)
        {
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                var hash = sha1.ComputeHash(bytes);
                var sb = new StringBuilder();
                for (var i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        #endregion
    }
}
