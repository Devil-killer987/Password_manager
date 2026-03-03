using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Password_manager
{
    public static class PasswordEncryptor
    {
        
        private static readonly byte[] Salt = new byte[]
            { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };

        // Метод для создания ключа из мастер-пароля пользователя
        public static byte[] DeriveKeyFromPassword(string password)
        {
            using (var keyDerivation = new Rfc2898DeriveBytes(password, Salt, 10000, HashAlgorithmName.SHA256))
            {
                return keyDerivation.GetBytes(32); // 256 бит для AES-256
            }
        }

        // Шифрование пароля
        public static string Encrypt(string plainText, byte[] key)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            byte[] encryptedBytes;

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV(); // Генерируем случайный IV

                using (MemoryStream ms = new MemoryStream())
                {
                    // Сохраняем IV в начало зашифрованных данных
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }

                    encryptedBytes = ms.ToArray();
                }
            }

            return Convert.ToBase64String(encryptedBytes);
        }

        // Дешифрование пароля
        public static string Decrypt(string cipherText, byte[] key)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;

                    // Извлекаем IV из начала данных
                    byte[] iv = new byte[aes.BlockSize / 8];
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    using (MemoryStream ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length, false))
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                // Если не удалось расшифровать, возвращаем как есть
                return cipherText;
            }
        }

        
        public static string EncryptDemo(string plainText)
        {
           
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }

        public static string DecryptDemo(string cipherText)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(cipherText));
            }
            catch
            {
                return cipherText;
            }
        }
    }
}
