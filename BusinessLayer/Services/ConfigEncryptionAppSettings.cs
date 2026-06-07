using System.Security.Cryptography;
using System.Text;

namespace BusinessLayer.Services
{
    public static class ConfigEncryptionHelper
    {
        private const string KEY = "12345678901234567890123456789012";

        public static void EncryptFile(string inputFile, string outputFile)
        {
            var plainText = File.ReadAllText(inputFile);

            using Aes aes = Aes.Create();

            aes.Key = Encoding.UTF8.GetBytes(KEY);

            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();

            using var ms = new MemoryStream();

            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                using var sw =
                    new StreamWriter(cs);

                sw.Write(plainText);
            }

            File.WriteAllBytes(outputFile, ms.ToArray());
        }

        public static string DecryptFile(string encryptedFile)
        {
            var fullBytes = File.ReadAllBytes(encryptedFile);

            using Aes aes = Aes.Create();

            aes.Key = Encoding.UTF8.GetBytes(KEY);

            byte[] iv = new byte[16];

            Array.Copy(fullBytes, 0, iv, 0, 16);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();

            using var ms = new MemoryStream(fullBytes, 16, fullBytes.Length - 16);

            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);

            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
    }
}