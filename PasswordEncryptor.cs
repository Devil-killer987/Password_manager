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
        // Константы
        private const int KeySize = 256;              // 256 бит для AES-256
        private const int IvSize = 16;                 // 16 байт для IV
        private const int SaltSize = 16;                // 16 байт для соли
        private const int Iterations = 100000;          // 100k итераций PBKDF2
        private const int MasterKeySize = 32;           // 32 байта = 256 бит

        #region Методы для работы с мастер-ключом

        /// <summary>
        /// Генерация случайного мастер-ключа для AES (256 бит)
        /// </summary>
        public static byte[] GenerateMasterKey()
        {
            byte[] key = new byte[MasterKeySize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        /// <summary>
        /// Получение ключа из пароля с использованием PBKDF2
        /// </summary>
        /// <param name="password">Пароль пользователя</param>
        /// <param name="salt">Соль (если не указана, генерируется новая)</param>
        /// <returns>Ключ шифрования</returns>
        public static byte[] DeriveKeyFromPassword(string password, byte[] salt = null)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Пароль не может быть пустым");

            // Генерируем новую соль, если не передана
            if (salt == null)
            {
                salt = new byte[SaltSize];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(MasterKeySize);
            }
        }

        /// <summary>
        /// Шифрование мастер-ключа с использованием пароля пользователя
        /// </summary>
        /// <param name="masterKey">Мастер-ключ (32 байта)</param>
        /// <param name="password">Пароль пользователя</param>
        /// <returns>Зашифрованный мастер-ключ в формате Base64</returns>
        public static string EncryptMasterKey(byte[] masterKey, string password)
        {
            if (masterKey == null || masterKey.Length != MasterKeySize)
                throw new ArgumentException($"Мастер-ключ должен быть размером {MasterKeySize} байт");

            // Генерируем соль
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Создаем ключ из пароля с солью
            byte[] key = DeriveKeyFromPassword(password, salt);

            // Генерируем IV
            byte[] iv = new byte[IvSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var ms = new MemoryStream())
                {
                    // Записываем соль и IV в начало (нужны для расшифровки)
                    ms.Write(salt, 0, salt.Length);
                    ms.Write(iv, 0, iv.Length);

                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(masterKey, 0, masterKey.Length);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// Дешифрование мастер-ключа с использованием пароля
        /// </summary>
        /// <param name="encryptedMasterKey">Зашифрованный мастер-ключ в формате Base64</param>
        /// <param name="password">Пароль пользователя</param>
        /// <returns>Дешифрованный мастер-ключ (32 байта)</returns>
        public static byte[] DecryptMasterKey(string encryptedMasterKey, string password)
        {
            if (string.IsNullOrEmpty(encryptedMasterKey))
                throw new ArgumentException("Зашифрованный мастер-ключ не может быть пустым");

            byte[] fullData = Convert.FromBase64String(encryptedMasterKey);

            // Проверяем минимальную длину
            if (fullData.Length < SaltSize + IvSize)
                throw new ArgumentException("Некорректные данные мастер-ключа");

            // Извлекаем соль
            byte[] salt = new byte[SaltSize];
            Array.Copy(fullData, 0, salt, 0, salt.Length);

            // Извлекаем IV
            byte[] iv = new byte[IvSize];
            Array.Copy(fullData, salt.Length, iv, 0, iv.Length);

            // Извлекаем зашифрованные данные
            byte[] encryptedData = new byte[fullData.Length - salt.Length - iv.Length];
            Array.Copy(fullData, salt.Length + iv.Length, encryptedData, 0, encryptedData.Length);

            // Восстанавливаем ключ из пароля
            byte[] key = DeriveKeyFromPassword(password, salt);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var ms = new MemoryStream(encryptedData))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var msOut = new MemoryStream())
                {
                    cs.CopyTo(msOut);
                    byte[] decryptedKey = msOut.ToArray();

                    if (decryptedKey.Length != MasterKeySize)
                        throw new CryptographicException($"Дешифрованный мастер-ключ имеет неверный размер: {decryptedKey.Length} байт");

                    return decryptedKey;
                }
            }
        }

        #endregion

        #region Методы для шифрования паролей

        /// <summary>
        /// Шифрование пароля с использованием мастер-ключа
        /// </summary>
        /// <param name="plainText">Исходный пароль</param>
        /// <param name="masterKey">Мастер-ключ (32 байта)</param>
        /// <returns>Зашифрованный пароль в формате Base64</returns>
        public static string Encrypt(string plainText, byte[] masterKey)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            if (masterKey == null || masterKey.Length != MasterKeySize)
                throw new ArgumentException($"Мастер-ключ должен быть размером {MasterKeySize} байт");

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            using (var aes = Aes.Create())
            {
                aes.Key = masterKey;
                aes.GenerateIV(); // Случайный IV для каждого пароля
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var ms = new MemoryStream())
                {
                    // Сохраняем IV в начале зашифрованных данных
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                    }

                    byte[] encryptedBytes = ms.ToArray();
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        /// <summary>
        /// Дешифрование пароля с использованием мастер-ключа
        /// </summary>
        /// <param name="cipherText">Зашифрованный пароль в формате Base64</param>
        /// <param name="masterKey">Мастер-ключ (32 байта)</param>
        /// <returns>Расшифрованный пароль</returns>
        public static string Decrypt(string cipherText, byte[] masterKey)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            if (masterKey == null || masterKey.Length != MasterKeySize)
                throw new ArgumentException($"Мастер-ключ должен быть размером {MasterKeySize} байт");

            try
            {
                byte[] fullData = Convert.FromBase64String(cipherText);

                // Проверяем минимальную длину
                if (fullData.Length < IvSize)
                    throw new ArgumentException("Некорректные данные пароля");

                // Извлекаем IV (первые IvSize байт)
                byte[] iv = new byte[IvSize];
                Array.Copy(fullData, 0, iv, 0, iv.Length);

                // Извлекаем зашифрованные данные
                byte[] encryptedData = new byte[fullData.Length - iv.Length];
                Array.Copy(fullData, iv.Length, encryptedData, 0, encryptedData.Length);

                using (var aes = Aes.Create())
                {
                    aes.Key = masterKey;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var ms = new MemoryStream(encryptedData))
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs, Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Ошибка дешифрования. Возможно, неверный мастер-ключ или поврежденные данные.", ex);
            }
            catch (FormatException ex)
            {
                throw new FormatException("Некорректный формат зашифрованных данных.", ex);
            }
        }

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Безопасное сравнение ключей (защита от timing attacks)
        /// </summary>
        public static bool SecureCompare(byte[] key1, byte[] key2)
        {
            if (key1 == null || key2 == null)
                return false;

            if (key1.Length != key2.Length)
                return false;

            int result = 0;
            for (int i = 0; i < key1.Length; i++)
            {
                result |= key1[i] ^ key2[i];
            }
            return result == 0;
        }

        /// <summary>
        /// Безопасная очистка массива (затирание ключа в памяти)
        /// </summary>
        public static void SecureClear(byte[] data)
        {
            if (data == null)
                return;

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0;
            }
        }

        /// <summary>
        /// Конвертация строки в мастер-ключ (для обратной совместимости)
        /// </summary>
        public static byte[] StringToMasterKey(string keyString)
        {
            if (string.IsNullOrEmpty(keyString))
                throw new ArgumentException("Ключ не может быть пустым");

            byte[] key = Encoding.UTF8.GetBytes(keyString);

            // Приводим к нужному размеру (32 байта)
            if (key.Length > MasterKeySize)
            {
                // Обрезаем
                byte[] truncated = new byte[MasterKeySize];
                Array.Copy(key, 0, truncated, 0, MasterKeySize);
                return truncated;
            }
            else if (key.Length < MasterKeySize)
            {
                // Дополняем хешем от ключа
                using (var sha256 = SHA256.Create())
                {
                    byte[] hash = sha256.ComputeHash(key);
                    Array.Copy(hash, 0, key, 0, key.Length);
                }

                // Если всё ещё меньше, дополняем нулями
                if (key.Length < MasterKeySize)
                {
                    byte[] padded = new byte[MasterKeySize];
                    Array.Copy(key, 0, padded, 0, key.Length);
                    return padded;
                }
            }

            return key;
        }

        /// <summary>
        /// Проверка валидности мастер-ключа
        /// </summary>
        public static bool IsValidMasterKey(byte[] masterKey)
        {
            return masterKey != null && masterKey.Length == MasterKeySize;
        }

        #endregion

        #region Демо-методы (для тестирования без шифрования)

        /// <summary>
        /// Демо-шифрование (только Base64, без реального шифрования)
        /// </summary>
        public static string EncryptDemo(string plainText)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }

        /// <summary>
        /// Демо-дешифрование
        /// </summary>
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

        #endregion
    }
}
