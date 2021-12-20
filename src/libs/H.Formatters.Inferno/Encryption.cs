using SecurityDriven.Inferno;

namespace H.Formatters;

internal static class Encryption
{
    internal static byte[] EncryptMessage(byte[] key, byte[] message)
    {
        var encrypted = SuiteB.Encrypt(key, new ArraySegment<byte>(message));
        var hash = CalculateHash(encrypted, key); // while reading, get the last 48 bytes as hash key to validate
        return encrypted.Concat(hash).ToArray();
    }

    internal static byte[] DecryptMessage(byte[] key, byte[] encryptedMessage)
    {
        var hash = encryptedMessage.Skip(encryptedMessage.Length - 48).ToArray();
        var cipherText = encryptedMessage.Take(encryptedMessage.Length - 48).ToArray();
        if (!ValidateHash(hash, cipherText, key))
        {
            throw new InvalidOperationException($"Integrity of the message is broken.");
        }

        return SuiteB.Decrypt(key, new ArraySegment<byte>(cipherText));
    }

    private static byte[] CalculateHash(byte[] message, byte[] key)
    {
        byte[] hash;
        using (var hmac = SuiteB.HmacFactory()) // HMACSHA384
        {
            hmac.Key = key;
            hash = hmac.ComputeHash(message); //384 bits, 48 bytes
        }
        return hash;
    }

    private static bool ValidateHash(byte[] hash, byte[] message, byte[] key)
    {
        return hash.SequenceEqual((byte[]?)CalculateHash(message, key));
    }
}
