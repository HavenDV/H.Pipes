using H.Pipes.Encryption;
using System.Threading;
using System.Threading.Tasks;

namespace H.Formatters
{
    public class CryptoFormatter : FormatterBase
    {
        public byte[]? Key { get; set; }
        private readonly IFormatter _formatter;
        private bool StartEncrypting() => Key != null || Key != default;

        public CryptoFormatter(IFormatter formatter)
        {
            _formatter = formatter;
        }

        /// <summary>
        /// Encrypts and serializes using any <see cref="FormatterBase"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override byte[] SerializeInternal(object obj)
        {
            var bytes = SerializeAsyncIfPossible(obj).GetAwaiter().GetResult();

            return StartEncrypting() ? Encryption.EncryptMessage(Key, bytes) : bytes;
        }

        /// <summary>
        /// Decrypts and deserializes using <see cref="FormatterBase"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public override T DeserializeInternal<T>(byte[] bytes)
        {
            byte[] message = StartEncrypting() ? Encryption.DecryptMessage(Key, bytes) : bytes;
            return _formatter.Deserialize<T>(message);
        }

        private async Task<byte[]> SerializeAsyncIfPossible(object value, CancellationToken cancellationToken = default) => _formatter is IAsyncFormatter asyncFormatter
                ? await asyncFormatter.SerializeAsync(value, cancellationToken).ConfigureAwait(false)
                : _formatter.Serialize(value);
    }
}
