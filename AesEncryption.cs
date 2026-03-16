using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;



namespace Password_manager
{
    public static class AesEncryption
    {
        // Константы для шифрования
        private const int KeySize = 256;              // 256 бит
        private const int BlockSize = 128;             // 128 бит
        private const int IvSize = 16;                 // 16 байт для IV
        private const int SaltSize = 16;                // 16 байт для соли
        private const int Iterations = 100000;          // 100k итераций PBKDF2 (рекомендуется не менее 100k)
        private const int MasterKeySize = 32;           // 32 байта = 256 бит для мастер-ключа

        #region Основные методы шифрования/дешифрования

        /// <summary>
        /// Шифрование текста с использованием пароля
        /// </summary>
        /// <param name="plainText">Текст для шифрования</param>
        /// <param name="password">Пароль для шифрования</param>
        /// <returns>Зашифрованная строка в формате Base64</returns>
        public static string Encrypt(string plainText, string password)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            // Генерируем соль
            byte[] salt = GenerateRandomBytes(SaltSize);

            // Создаем ключ из пароля с использованием PBKDF2
            byte[] key = DeriveKeyFromPassword(password, salt, Iterations);

            // Генерируем случайный IV
            byte[] iv = GenerateRandomBytes(IvSize);

            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var ms = new MemoryStream())
                {
                    // Записываем соль в начало (нужна для дешифрования)
                    ms.Write(salt, 0, salt.Length);

                    // Записываем IV
                    ms.Write(iv, 0, iv.Length);

                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }

                    byte[] encryptedBytes = ms.ToArray();
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        /// <summary>
        /// Дешифрование текста с использованием пароля
        /// </summary>
        /// <param name="cipherText">Зашифрованный текст в формате Base64</param>
        /// <param name="password">Пароль для дешифрования</param>
        /// <returns>Расшифрованный текст</returns>
        public static string Decrypt(string cipherText, string password)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);

                // Проверяем минимальную длину (соль + IV)
                if (fullCipher.Length < SaltSize + IvSize)
                    throw new ArgumentException("Некорректные зашифрованные данные");

                // Извлекаем соль (первые SaltSize байт)
                byte[] salt = new byte[SaltSize];
                Array.Copy(fullCipher, 0, salt, 0, salt.Length);

                // Извлекаем IV (следующие IvSize байт)
                byte[] iv = new byte[IvSize];
                Array.Copy(fullCipher, salt.Length, iv, 0, iv.Length);

                // Остальное - зашифрованные данные
                byte[] cipherBytes = new byte[fullCipher.Length - salt.Length - iv.Length];
                Array.Copy(fullCipher, salt.Length + iv.Length, cipherBytes, 0, cipherBytes.Length);

                // Восстанавливаем ключ из пароля
                byte[] key = DeriveKeyFromPassword(password, salt, Iterations);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.BlockSize = BlockSize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = key;
                    aes.IV = iv;

                    using (var ms = new MemoryStream(cipherBytes))
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs, Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException ex)
            {
                throw new Exception("Ошибка дешифрования. Возможно, неверный пароль или поврежденные данные.", ex);
            }
            catch (FormatException ex)
            {
                throw new Exception("Некорректный формат зашифрованных данных.", ex);
            }
        }

        #endregion

        #region Методы для работы с мастер-ключом

        /// <summary>
        /// Генерация случайного мастер-ключа для AES (256 бит)
        /// </summary>
        public static byte[] GenerateMasterKey()
        {
            return GenerateRandomBytes(MasterKeySize);
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
            byte[] salt = GenerateRandomBytes(SaltSize);

            // Создаем ключ из пароля
            byte[] key = DeriveKeyFromPassword(password, salt, Iterations);

            // Генерируем IV
            byte[] iv = GenerateRandomBytes(IvSize);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var ms = new MemoryStream())
                {
                    // Записываем соль и IV
                    ms.Write(salt, 0, salt.Length);
                    ms.Write(iv, 0, iv.Length);

                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(masterKey, 0, masterKey.Length);
                        cs.FlushFinalBlock();
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
            byte[] key = DeriveKeyFromPassword(password, salt, Iterations);

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
                        throw new Exception($"Дешифрованный мастер-ключ имеет неверный размер: {decryptedKey.Length} байт");

                    return decryptedKey;
                }
            }
        }

        #endregion

        #region Методы для шифрования паролей

        /// <summary>
        /// Шифрование пароля с использованием мастер-ключа
        /// </summary>
        /// <param name="plainPassword">Исходный пароль</param>
        /// <param name="masterKey">Мастер-ключ (32 байта)</param>
        /// <returns>Зашифрованный пароль в формате Base64</returns>
        public static string EncryptPassword(string plainPassword, byte[] masterKey)
        {
            if (string.IsNullOrEmpty(plainPassword))
                return string.Empty;

            if (masterKey == null || masterKey.Length != MasterKeySize)
                throw new ArgumentException($"Мастер-ключ должен быть размером {MasterKeySize} байт");

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainPassword);

            using (var aes = Aes.Create())
            {
                aes.Key = masterKey;
                aes.GenerateIV(); // Случайный IV для каждого пароля
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var ms = new MemoryStream())
                {
                    // Сохраняем IV в начале
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// Дешифрование пароля с использованием мастер-ключа
        /// </summary>
        /// <param name="encryptedPassword">Зашифрованный пароль в формате Base64</param>
        /// <param name="masterKey">Мастер-ключ (32 байта)</param>
        /// <returns>Расшифрованный пароль</returns>
        public static string DecryptPassword(string encryptedPassword, byte[] masterKey)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return string.Empty;

            if (masterKey == null || masterKey.Length != MasterKeySize)
                throw new ArgumentException($"Мастер-ключ должен быть размером {MasterKeySize} байт");

            byte[] fullData = Convert.FromBase64String(encryptedPassword);

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

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Генерация случайных байт
        /// </summary>
        private static byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return bytes;
        }

        /// <summary>
        /// Получение ключа из пароля с использованием PBKDF2
        /// </summary>
        private static byte[] DeriveKeyFromPassword(string password, byte[] salt, int iterations)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(MasterKeySize);
            }
        }

        /// <summary>
        /// Безопасное сравнение мастер-ключей (защита от timing attacks)
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

            Array.Clear(data, 0, data.Length);
        }

        /// <summary>
        /// Конвертация строки в мастер-ключ (для тестирования)
        /// </summary>
        public static byte[] StringToMasterKey(string keyString)
        {
            if (string.IsNullOrEmpty(keyString))
                throw new ArgumentException("Ключ не может быть пустым");

            // Для реального использования лучше использовать GenerateMasterKey()
            // Этот метод только для совместимости с существующим кодом
            byte[] key = Encoding.UTF8.GetBytes(keyString);

            if (key.Length > MasterKeySize)
            {
                // Обрезаем до нужного размера
                byte[] truncated = new byte[MasterKeySize];
                Array.Copy(key, 0, truncated, 0, MasterKeySize);
                return truncated;
            }
            else if (key.Length < MasterKeySize)
            {
                // Дополняем нулями
                byte[] padded = new byte[MasterKeySize];
                Array.Copy(key, 0, padded, 0, key.Length);
                return padded;
            }

            return key;
        }

        #endregion

        #region Методы для тестирования

        /// <summary>
        /// Тестирование шифрования (для проверки работоспособности)
        /// </summary>
        public static bool TestEncryption()
        {
            try
            {
                string testPassword = "TestPassword123!";
                string testText = "Секретные данные для тестирования";

                // Тест 1: Шифрование/дешифрование с паролем
                string encrypted = Encrypt(testText, testPassword);
                string decrypted = Decrypt(encrypted, testPassword);

                if (decrypted != testText)
                    return false;

                // Тест 2: Мастер-ключ
                byte[] masterKey = GenerateMasterKey();
                string encryptedMasterKey = EncryptMasterKey(masterKey, testPassword);
                byte[] decryptedMasterKey = DecryptMasterKey(encryptedMasterKey, testPassword);

                if (!SecureCompare(masterKey, decryptedMasterKey))
                    return false;

                // Тест 3: Шифрование пароля мастер-ключом
                string testPassword2 = "MySecretPassword";
                string encryptedPassword = EncryptPassword(testPassword2, masterKey);
                string decryptedPassword = DecryptPassword(encryptedPassword, masterKey);

                return decryptedPassword == testPassword2;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
