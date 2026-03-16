using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Password_manager
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 10000;

        /// <summary>
        /// Хеширование пароля с солью
        /// </summary>
        public static string HashPassword(string password)
        {
            // Генерируем соль
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Хешируем пароль с солью
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(HashSize);

                // Объединяем соль и хеш
                byte[] hashBytes = new byte[SaltSize + HashSize];
                Array.Copy(salt, 0, hashBytes, 0, SaltSize);
                Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// Проверка пароля
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            // Получаем байты из сохраненного хеша
            byte[] hashBytes = Convert.FromBase64String(storedHash);

            // Извлекаем соль
            byte[] salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            // Извлекаем оригинальный хеш
            byte[] originalHash = new byte[HashSize];
            Array.Copy(hashBytes, SaltSize, originalHash, 0, HashSize);

            // Хешируем введенный пароль с той же солью
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] newHash = pbkdf2.GetBytes(HashSize);

                // Сравниваем хеши
                return CompareHashes(originalHash, newHash);
            }
        }

        /// <summary>
        /// Безопасное сравнение хешей
        /// </summary>
        private static bool CompareHashes(byte[] hash1, byte[] hash2)
        {
            if (hash1.Length != hash2.Length)
                return false;

            int result = 0;
            for (int i = 0; i < hash1.Length; i++)
            {
                result |= hash1[i] ^ hash2[i];
            }
            return result == 0;
        }

        /// <summary>
        /// Проверка сложности пароля
        /// </summary>
        public static (bool IsValid, string Message) ValidatePasswordStrength(string password)
        {
            if (password.Length < 8)
                return (false, "Пароль должен содержать минимум 8 символов");

            var messages = new System.Collections.Generic.List<string>();

            if (!password.Any(char.IsLower))
                messages.Add("добавьте строчные буквы");

            if (!password.Any(char.IsUpper))
                messages.Add("добавьте заглавные буквы");

            if (!password.Any(char.IsDigit))
                messages.Add("добавьте цифры");

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                messages.Add("добавьте спецсимволы");

            if (messages.Count == 0)
                return (true, "Надежный пароль");
            else if (messages.Count <= 2)
                return (true, $"Пароль приемлемый, но рекомендуется: {string.Join(", ", messages)}");
            else
                return (false, $"Пароль слабый. Рекомендации: {string.Join(", ", messages)}");
        }

        /// <summary>
        /// Оценка сложности пароля в процентах
        /// </summary>
        public static int CheckPasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return 0;

            int score = 0;

            // Длина
            if (password.Length >= 8) score += 25;
            else if (password.Length >= 6) score += 15;
            else score += 5;

            // Строчные буквы
            if (password.Any(char.IsLower)) score += 15;

            // Заглавные буквы
            if (password.Any(char.IsUpper)) score += 20;

            // Цифры
            if (password.Any(char.IsDigit)) score += 20;

            // Спецсимволы
            if (password.Any(ch => !char.IsLetterOrDigit(ch))) score += 20;

            return Math.Min(100, score);
        }
        public static string GenerateStrongPassword(
          int length = 16,
          bool includeUppercase = true,
          bool includeLowercase = true,
          bool includeDigits = true,
          bool includeSpecial = true)
        {
            const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
            const string digitChars = "0123456789";
            const string specialChars = "!@#$%^&*()_-+=<>?";

            // Собираем разрешенные символы
            StringBuilder allowedChars = new StringBuilder();
            StringBuilder password = new StringBuilder();

            if (includeUppercase)
            {
                allowedChars.Append(uppercaseChars);
                password.Append(uppercaseChars[RandomNumberGenerator.GetInt32(uppercaseChars.Length)]);
            }

            if (includeLowercase)
            {
                allowedChars.Append(lowercaseChars);
                password.Append(lowercaseChars[RandomNumberGenerator.GetInt32(lowercaseChars.Length)]);
            }

            if (includeDigits)
            {
                allowedChars.Append(digitChars);
                password.Append(digitChars[RandomNumberGenerator.GetInt32(digitChars.Length)]);
            }

            if (includeSpecial)
            {
                allowedChars.Append(specialChars);
                password.Append(specialChars[RandomNumberGenerator.GetInt32(specialChars.Length)]);
            }

            // Если не выбрано ни одного типа символов, используем все
            if (allowedChars.Length == 0)
            {
                allowedChars.Append(uppercaseChars + lowercaseChars + digitChars);
            }

            // Заполняем оставшуюся длину случайными символами
            for (int i = password.Length; i < length; i++)
            {
                password.Append(allowedChars[RandomNumberGenerator.GetInt32(allowedChars.Length)]);
            }

            // Перемешиваем пароль для случайного порядка
            return ShufflePassword(password.ToString());
        }

        /// <summary>
        /// Перемешивание символов в пароле
        /// </summary>
        private static string ShufflePassword(string password)
        {
            char[] array = password.ToCharArray();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (array[j], array[i]) = (array[i], array[j]);
            }
            return new string(array);
        }

        /// <summary>
        /// Генерация PIN-кода (только цифры)
        /// </summary>
        public static string GeneratePin(int length = 6)
        {
            const string digits = "0123456789";
            StringBuilder pin = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                pin.Append(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
            }

            return pin.ToString();
        }

        /// <summary>
        /// Генерация пароля с проверкой сложности (гарантирует надежность)
        /// </summary>
        public static string GenerateSecurePassword(int length = 16)
        {
            string password;
            do
            {
                password = GenerateStrongPassword(length);
            }
            while (!ValidatePasswordStrength(password).IsValid);

            return password;
        }
    }
}


