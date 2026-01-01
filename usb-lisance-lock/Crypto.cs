using System.Security.Cryptography;
using System.Text;

public static class Crypto
{
    public static bool VerifySignature(
        string message,
        byte[] signature,
        string publicKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);

        return rsa.VerifyData(
            Encoding.UTF8.GetBytes(message),
            signature,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
    }

    public static byte[] SignData(
        string message,
        string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        return rsa.SignData(
            Encoding.UTF8.GetBytes(message),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
    }
}
