#pragma warning disable 8600, 8602 // Disable null check warnings for fields that are initialized in the constructor

namespace Gaos.Auth
{
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;

    public class RSAKeys
    {
        public static RSA ReadPrivateKey(string pkc12KeyStoreFilePath, string keyStorePassword)
        {
            X509Certificate2 certificate = new X509Certificate2(pkc12KeyStoreFilePath, keyStorePassword);

            RSA privateKey = certificate.GetRSAPrivateKey();
            if (privateKey == null)
            {
                throw new Exception($"cannot read private key from keystore {pkc12KeyStoreFilePath}");
            }

            return privateKey;
        }
        public static RSA ReadPublicKey(string pkc12KeyStoreFilePath, string keyStorePassword)
        {
            X509Certificate2 certificate = new X509Certificate2(pkc12KeyStoreFilePath, keyStorePassword);

            RSA publicKey = certificate.GetRSAPublicKey();
            if (publicKey == null)
            {
                throw new Exception($"cannot read public key from keystore {pkc12KeyStoreFilePath}");
            }

            return publicKey;
        }
    }
}
