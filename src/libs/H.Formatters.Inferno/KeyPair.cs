using System.Security.Cryptography;
using SecurityDriven.Inferno.Extensions;

namespace H.Formatters;

#if NET5_0_OR_GREATER
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
#elif NETSTANDARD2_0_OR_GREATER || NET461_OR_GREATER
#else
#error Target Framework is not supported
#endif
internal sealed class KeyPair : IDisposable
{
    #region Properties

    private CngKey PrivateKey { get; }
    public byte[] PublicKey { get; }

    #endregion

    #region Constructors

    public KeyPair(CngKey key)
    {
        key = key ?? throw new ArgumentNullException(nameof(key));

        PrivateKey = key;
        PublicKey = key.Export(CngKeyBlobFormat.EccPublicBlob);
    }

    public KeyPair() : this(CngKeyExtensions.CreateNewDhmKey())
    {
    }

    #endregion

    #region Methods

    public byte[] GenerateSharedKey(byte[] recipientPublicKey)
    {
        ValidatePublicKey(recipientPublicKey);

        using var key = CngKey.Import(recipientPublicKey, CngKeyBlobFormat.EccPublicBlob);

        return PrivateKey.GetSharedDhmSecret(key);
    }

    public void Dispose()
    {
        PrivateKey.Dispose();
    }

    private static void ValidatePublicKey(byte[] bytes)
    {
        bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));

        if (bytes.Length != 104)
        {
            throw new ArgumentException("bytes.Lenght is not 104.");
        }
        try
        {
            using var tempKey = CngKey.Import(bytes, CngKeyBlobFormat.EccPublicBlob);
        }
        catch (Exception)
        {
            throw new ArgumentException("Public key is not valid.");
        }
    }

    #endregion
}
