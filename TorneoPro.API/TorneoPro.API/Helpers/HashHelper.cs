using System;
using System.Security.Cryptography;
using System.Text;

namespace TorneoPro.API.Helpers
{
    public class HashHelper
    {
        private const int SaltSize = 16; // 128 bits
        private const int HashSize = 32; // 256 bits
        private const int Iterations = 10000;

        public static string HashPassword(string password, string? salt = null)
        {
            byte[] saltBytes;

            if (string.IsNullOrEmpty(salt))
            {
                saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            }
            else
            {
                saltBytes = Convert.FromBase64String(salt);
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256
            );

            byte[] hashBytes = pbkdf2.GetBytes(HashSize);
            byte[] hashWithSaltBytes = new byte[SaltSize + HashSize];

            Array.Copy(saltBytes, 0, hashWithSaltBytes, 0, SaltSize);
            Array.Copy(hashBytes, 0, hashWithSaltBytes, SaltSize, HashSize);

            return Convert.ToBase64String(hashWithSaltBytes);
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            byte[] hashWithSaltBytes = Convert.FromBase64String(storedHash);

            if (hashWithSaltBytes.Length != SaltSize + HashSize)
            {
                return false;
            }

            byte[] saltBytes = new byte[SaltSize];
            Array.Copy(hashWithSaltBytes, 0, saltBytes, 0, SaltSize);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256
            );

            byte[] hashBytes = pbkdf2.GetBytes(HashSize);

            for (int i = 0; i < HashSize; i++)
            {
                if (hashWithSaltBytes[SaltSize + i] != hashBytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static string GenerateSalt()
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            return Convert.ToBase64String(saltBytes);
        }

        public static string ComputeSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        public static string ComputeMd5Hash(string input)
        {
            using var md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public static string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
            var result = new StringBuilder();

            using var rng = RandomNumberGenerator.Create();
            byte[] uintBuffer = new byte[sizeof(uint)];

            while (length-- > 0)
            {
                rng.GetBytes(uintBuffer);
                uint num = BitConverter.ToUInt32(uintBuffer, 0);
                result.Append(validChars[(int)(num % (uint)validChars.Length)]);
            }

            return result.ToString();
        }
    }
}