using System.Security.Cryptography;
using SecurityDriven.Inferno.Extensions;

namespace H.Pipes.Encryption;

/// <summary>
/// 
/// </summary>
public class KeyPair
{
    /// <summary>
    /// 
    /// </summary>
    public string PublicKey => new(publicKey.Select(b => (char)b).ToArray());

    private readonly CngKey privateKey;
    private readonly byte[] publicKey;

    /// <summary>
    /// 
    /// </summary>
    public KeyPair()
    {
        privateKey = CngKeyExtensions.CreateNewDhmKey();
        publicKey = privateKey.Export(CngKeyBlobFormat.EccPublicBlob);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    public KeyPair(CngKey key)
    {
        key = key ?? throw new ArgumentNullException(nameof(key));

        privateKey = key;
        publicKey = key.Export(CngKeyBlobFormat.EccPublicBlob);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="recipientPublicKey"></param>
    /// <returns></returns>
    public byte[] GenerateSharedKey(byte[] recipientPublicKey)
    {
        return privateKey.GetSharedDhmSecret(CngKey.Import(recipientPublicKey, CngKeyBlobFormat.EccPublicBlob));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static byte[] ValidatePublicKey(string message)
    {
        message = message ?? throw new ArgumentNullException(nameof(message));

        var bytes = message.ToCharArray().Select(c => (byte)c).ToArray();
        if (bytes.Length != 104)
        {
            throw new ArgumentException("message.Lenght is not 104");
        }

        return bytes;
    }
}
