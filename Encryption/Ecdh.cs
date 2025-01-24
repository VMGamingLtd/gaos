using System.Security.Cryptography;
using Serilog;

namespace Gaos.Encryption
{
    public class Ecdh
    {
        private static string CLASS_NAME = typeof(Ecdh).Name;

        public class DeriveSharedSecretResult
        {
            public byte[]? SharedSecret;
            public string? pubKeyBase64;
        }

        public static DeriveSharedSecretResult DeriveSharedSecret(string pubKeyBase64)
        {
            const string METHOD_NAME = "DeriveSharedSecret()";
            try
            {
                using var myEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                byte[] myPubKeySpkiBytes = myEcdh.ExportSubjectPublicKeyInfo();

                // Convert the public key to a base64 string
                string muPubKey = System.Convert.ToBase64String(myPubKeySpkiBytes);

                byte[] clientPubKeySpkiBytes = Convert.FromBase64String(pubKeyBase64);
                using var clientEcdhPub = ECDiffieHellman.Create();
                clientEcdhPub.ImportSubjectPublicKeyInfo(clientPubKeySpkiBytes, out _);

                //byte[] sharedSecret = myEcdh.DeriveKeyMaterial(clientEcdhPub.PublicKey);
                byte[] sharedSecret = myEcdh.DeriveKeyFromHash(clientEcdhPub.PublicKey, HashAlgorithmName.SHA256);
                // print sharedSecrent bytes
                /*
                for (int i = 0; i < sharedSecret.Length; i++)
                {
                    Log.Information($"@@@@@@@@@@@@@@@@@@@@@@@@@@@@ cp 500: sharedSecret[{i}]: {sharedSecret[i]}");
                }
                */

                DeriveSharedSecretResult result = new DeriveSharedSecretResult
                {
                    SharedSecret = sharedSecret,
                    pubKeyBase64 = muPubKey,
                };

                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                throw new Exception("error while deriving ecdh shared secret");
            }
        }
    }
}
