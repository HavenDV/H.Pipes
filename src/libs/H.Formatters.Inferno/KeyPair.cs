using System.Security.Cryptography;
using System.Text;
using SecurityDriven.Inferno.Extensions;
using System.Linq;

namespace H.Pipes.Encryption
{
    public class KeyPair
    {
        public string PublicKey => new(publicKey.Select(b => (char)b).ToArray());

        private readonly CngKey privateKey;
        private readonly byte[] publicKey;

        public KeyPair()
        {
            privateKey = CngKeyExtensions.CreateNewDhmKey();
            publicKey = privateKey.Export(CngKeyBlobFormat.EccPublicBlob);
        }
        public KeyPair(CngKey cngKey)
        {
            privateKey = cngKey;
            publicKey = cngKey.Export(CngKeyBlobFormat.EccPublicBlob);
        }

        public byte[] GenerateSharedKey(byte[] recipientPublicKey)
        {
            return privateKey.GetSharedDhmSecret(CngKey.Import(recipientPublicKey, CngKeyBlobFormat.EccPublicBlob));
        }

        public bool ValidatePublicKey(string message, out byte[]? publicKey)
        {
            var bytes = message.ToCharArray().Select(c => (byte)c).ToArray();
            if (bytes.Length == 104)
            {
                publicKey = bytes;
                return true;
            }
            publicKey = null;
            return false;
        }
    }
}
