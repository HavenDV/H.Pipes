using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using H.Formatters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Pipes.Tests
{
    public static class BaseTests
    {
        public static async Task DataTestAsync<T>(IPipeServer<T> server, IPipeClient<T> client, List<T> values, Func<T, string>? hashFunc = null, CancellationToken cancellationToken = default)
        {
            Trace.WriteLine("Setting up test...");

            var completionSource = new TaskCompletionSource<bool>(false);
            // ReSharper disable once AccessToModifiedClosure
            using var registration = cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));

            var actualHash = (string?)null;
            var clientDisconnected = false;

            server.ClientConnected += (sender, args) =>
            {
                Trace.WriteLine("Client connected");
            };
            server.ClientDisconnected += (sender, args) =>
            {
                Trace.WriteLine("Client disconnected");
                clientDisconnected = true;

                // ReSharper disable once AccessToModifiedClosure
                completionSource.TrySetResult(true);
            };
            server.MessageReceived += (sender, args) =>
            {
                Trace.WriteLine($"Server_OnMessageReceived: {args.Message}");
                actualHash = hashFunc?.Invoke(args.Message);
                Trace.WriteLine($"ActualHash: {actualHash}");

                // ReSharper disable once AccessToModifiedClosure
                completionSource.TrySetResult(true);
            };
            server.ExceptionOccurred += (sender, args) =>
            {
                Trace.WriteLine($"Server exception occurred: {args.Exception}");

                // ReSharper disable once AccessToModifiedClosure
                completionSource.TrySetException(args.Exception);
            };
            client.Connected += (sender, args) => Trace.WriteLine("Client_OnConnected");
            client.Disconnected += (sender, args) => Trace.WriteLine("Client_OnDisconnected");
            client.MessageReceived += (sender, args) => Trace.WriteLine($"Client_OnMessageReceived: {args.Message}");
            client.ExceptionOccurred += (sender, args) =>
            {
                Trace.WriteLine($"Client exception occurred: {args.Exception}");

                // ReSharper disable once AccessToModifiedClosure
                completionSource.TrySetException(args.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is Exception exception)
                {
                    // ReSharper disable once AccessToModifiedClosure
                    completionSource.TrySetException(exception);
                }
            };

            server.ExceptionOccurred += (sender, args) => Trace.WriteLine(args.Exception.ToString());
            client.ExceptionOccurred += (sender, args) => Trace.WriteLine(args.Exception.ToString());

            await server.StartAsync(cancellationToken).ConfigureAwait(false);
            await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

            Trace.WriteLine("Client and server started");
            Trace.WriteLine("---");

            var watcher = Stopwatch.StartNew();

            foreach (var value in values)
            {
                var expectedHash = hashFunc?.Invoke(value);
                Trace.WriteLine($"ExpectedHash: {expectedHash}");

                await client.WriteAsync(value, cancellationToken).ConfigureAwait(false);

                await completionSource.Task.ConfigureAwait(false);

                if (hashFunc != null)
                {
                    Assert.IsNotNull(actualHash, "Server should have received a zero-byte message from the client");
                }

                Assert.AreEqual(expectedHash, actualHash, "SHA-1 hashes for zero-byte message should match");
                Assert.IsFalse(clientDisconnected, "Server should not disconnect the client for explicitly sending zero-length data");

                Trace.WriteLine("---");
                
                completionSource = new TaskCompletionSource<bool>(false);
            }

            Trace.WriteLine($"Test took {watcher.Elapsed}");
            Trace.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~");
        }

        public static async Task DataTestAsync<T>(List<T> values, Func<T, string>? hashFunc = null, IFormatter? formatter = default, TimeSpan? timeout = default)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));

            const string pipeName = "data_test_pipe";
            await using var server = new PipeServer<T>(pipeName, formatter ?? new BinaryFormatter());
            await using var client = new PipeClient<T>(pipeName, formatter: formatter ?? new BinaryFormatter());

            await DataTestAsync(server, client, values, hashFunc, cancellationTokenSource.Token);
        }

        public static async Task DataSingleTestAsync<T>(List<T> values, Func<T, string>? hashFunc = null, IFormatter? formatter = default, TimeSpan? timeout = default)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));

            const string pipeName = "data_test_pipe";
            await using var server = new SingleConnectionPipeServer<T>(pipeName, formatter ?? new BinaryFormatter());
            await using var client = new SingleConnectionPipeClient<T>(pipeName, formatter: formatter ?? new BinaryFormatter());

            await DataTestAsync(server, client, values, hashFunc, cancellationTokenSource.Token);
        }

        public static async Task BinaryDataTestAsync(int numBytes, int count = 1, IFormatter? formatter = default, TimeSpan? timeout = default)
        {
            await DataTestAsync(GenerateData(numBytes, count), Hash, formatter, timeout);
        }

        public static async Task BinaryDataSingleTestAsync(int numBytes, int count = 1, IFormatter? formatter = default, TimeSpan? timeout = default)
        {
            await DataSingleTestAsync(GenerateData(numBytes, count), Hash, formatter, timeout);
        }

        #region Helper methods

        public static List<byte[]> GenerateData(int numBytes, int count = 1)
        {
            var values = new List<byte[]>();

            for (var i = 0; i < count; i++)
            {
                var value = new byte[numBytes];
                new Random().NextBytes(value);

                values.Add(value);
            }

            return values;
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
