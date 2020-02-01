using System;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes.Args;

namespace H.Pipes.Extensions
{
    /// <summary>
    /// Common client/server extensions
    /// </summary>
    public static class ConnectionExtensions
    {
        /// <summary>
        /// Waits for the next message asynchronously <br/>
        /// Returns ConnectionMessageEventArgs if message was received <br/>
        /// Throws <see cref="OperationCanceledException"/> if method was canceled <br/>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="func"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public static async Task<ConnectionMessageEventArgs<T>> WaitMessageAsync<T>(this IPipeConnection<T> connection, Func<CancellationToken, Task>? func = null, CancellationToken cancellationToken = default)
        {
            return await connection.WaitEventAsync<ConnectionMessageEventArgs<T>>(
                func ?? (token => Task.CompletedTask),
                nameof(connection.MessageReceived),
                cancellationToken);
        }

        /// <summary>
        /// Waits for the next message asynchronously with specified timeout <br/>
        /// Returns DataEventArgs if message was received <br/>
        /// Throws <see cref="OperationCanceledException"/> if method was canceled <br/>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="timeout"></param>
        /// <param name="func"></param>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public static async Task<ConnectionMessageEventArgs<T>> WaitMessageAsync<T>(this IPipeConnection<T> connection, TimeSpan timeout, Func<CancellationToken, Task>? func = null)
        {
            using var tokenSource = new CancellationTokenSource(timeout);

            return await connection.WaitMessageAsync(func, tokenSource.Token);
        }
    }
}
