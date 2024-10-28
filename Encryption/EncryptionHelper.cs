using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Gaos.Encryption
{
    public class EncryptionHelper
    {
        private static string? G_PASSWORD = null;

        public static string GetPasswordFileName()
        {
            string homeFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeFolderPath, "gao_password.txt");
        }

        public static string GetPassword()
        {
            if (G_PASSWORD == null)
            {
                string passwordFileName = GetPasswordFileName();
                G_PASSWORD = File.ReadAllText(passwordFileName).Trim();
            }
            return G_PASSWORD;
        }

        public static string Encrypt(string plaintext)
        {
            string password = GetPassword();
            return Encrypt(password, plaintext);
        }

        public static string Encrypt(string password, string plaintext)
        {
            using var rng = RandomNumberGenerator.Create();

            byte[] salt = new byte[16];
            rng.GetBytes(salt);

            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] key = deriveBytes.GetBytes(32);

            using var aes = new AesGcm(key, 16);

            byte[] nonce = new byte[12];
            rng.GetBytes(nonce);

            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[16];

            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            string encryptedString = $"{Convert.ToBase64String(ciphertext)}:{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(tag)}";
            return encryptedString;
        }

        public static string Decrypt(string encryptedString)
        {
            string password = GetPassword();
            return Decrypt(password, encryptedString);
        }

        public static string Decrypt(string password, string encryptedString)
        {
            var parts = encryptedString.Split(':');
            if (parts.Length != 4)
            {
                throw new ArgumentException("Invalid encrypted string format.");
            }

            byte[] ciphertext = Convert.FromBase64String(parts[0]);
            byte[] nonce = Convert.FromBase64String(parts[1]);
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] tag = Convert.FromBase64String(parts[3]);

            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] key = deriveBytes.GetBytes(32);

            using var aes = new AesGcm(key, 16);

            byte[] plaintextBytes = new byte[ciphertext.Length];
            aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);

            return Encoding.UTF8.GetString(plaintextBytes);
        }

        // Example usage
        public static void test(string[] args)
        {
            string password = GetPassword();
            string plaintext = "Hello, World!";

            string encrypted = Encrypt(password, plaintext);
            Console.WriteLine($"Encrypted: {encrypted}");

            string decrypted = Decrypt(password, encrypted);
            Console.WriteLine($"Decrypted: {decrypted}");
        }
    }
}
