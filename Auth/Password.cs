using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.CompilerServices;

namespace Gaos.Auth
{
    public class EncodedPassword
    {
        public string? PasswordSalt { get; set; }
        public string? PasswordHash { get; set; }

    }
    public class Password
    {
        private static byte[] generateSalt(int byteLength)
        {
            byte[] salt = new byte[byteLength];
            RandomNumberGenerator.Create().GetBytes(salt);
            return salt;
        }

        private static string serializeSalt(byte[] salt)
        {
            string serializedSalt = Convert.ToBase64String(salt);
            return serializedSalt;
        }
        private static byte[] deSerializeSalt(string salt)
        {
            byte[] deserializedSalt = Convert.FromBase64String(salt);
            return deserializedSalt;
        }

        private static string getPasswordHash(byte[] salt, string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltedPasswordBytes = new byte[passwordBytes.Length + salt.Length];
            Array.Copy(passwordBytes, saltedPasswordBytes, passwordBytes.Length);
            Array.Copy(salt, 0, saltedPasswordBytes, passwordBytes.Length, salt.Length);

            byte[] hashedBytes = SHA256.HashData(saltedPasswordBytes);
            string hashedPassword = Convert.ToBase64String(hashedBytes);

            return hashedPassword;
        }

        public static bool verifyPassword(string passwordSalt, string passwordHash, string password)
        {
            byte[] saltBytes = Password.deSerializeSalt(passwordSalt);
            string _passwordHash = Password.getPasswordHash(saltBytes, password);
            if (string.Equals(passwordHash, _passwordHash))
            {
                return true;
            } 
            else
            {
                return false;
            } 

        }

        public static EncodedPassword getEncodedPassword(string password)
        {
            byte[] salt = Password.generateSalt(16);
            string passwordHash = Password.getPasswordHash(salt, password);
            string saltStr = Password.serializeSalt(salt);

            EncodedPassword encodedPassword = new EncodedPassword
            {
                PasswordSalt = saltStr,
                PasswordHash = passwordHash,
            };

            return encodedPassword;

        }
    }
}
