using System.Security.Cryptography;
using SecurityDriven.Inferno.Extensions;

namespace H.Formatters;

internal class KeyPair : IDisposable
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
        using var key = CngKey.Import(recipientPublicKey, CngKeyBlobFormat.EccPublicBlob);

        return PrivateKey.GetSharedDhmSecret(key);
    }

    public static void ValidatePublicKey(byte[] bytes)
    {
        bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));

        if (bytes.Length != 104)
        {
            throw new ArgumentException("bytes.Lenght is not 104.");
        }
    }

    public void Dispose()
    {
        PrivateKey.Dispose();
    }

    #endregion
}
