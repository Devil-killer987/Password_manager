using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Password_manager
{
    public static class DeterministicEncryption
    {
        private static readonly byte[] FixedIV = new byte[16]; // 16 нулевых байт для детерминизма

        /// <summary>
        /// Детерминированное шифрование строки
        /// </summary>
        public static string Encrypt(string plainText, byte[] masterKey)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = masterKey;
                    aes.IV = FixedIV;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

                    using (var ms = new MemoryStream())
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка детерминированного шифрования: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Дешифрование детерминированно зашифрованных данных
        /// </summary>
        public static string Decrypt(string cipherText, byte[] masterKey)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = masterKey;
                    aes.IV = FixedIV;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    byte[] cipherBytes = Convert.FromBase64String(cipherText);

                    using (var ms = new MemoryStream(cipherBytes))
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs, Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException)
            {
                throw new Exception("Ошибка дешифрования. Неверный ключ или поврежденные данные.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка дешифрования: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Проверка соответствия (для поиска)
        /// </summary>
        public static bool Matches(string plainText, string encryptedText, byte[] masterKey)
        {
            try
            {
                string encryptedPlain = Encrypt(plainText, masterKey);
                return encryptedPlain == encryptedText;
            }
            catch
            {
                return false;
            }
        }
    }
}
