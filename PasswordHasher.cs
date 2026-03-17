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
        /// Хеширование пароля (соль включается в хеш)
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

                // Объединяем соль и хеш в один массив
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
            try
            {
                // Получаем байты из сохраненного хеша
                byte[] hashBytes = Convert.FromBase64String(storedHash);

                // Извлекаем соль (первые SaltSize байт)
                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                // Извлекаем оригинальный хеш (оставшиеся байты)
                byte[] expectedHash = new byte[HashSize];
                Array.Copy(hashBytes, SaltSize, expectedHash, 0, HashSize);

                // Хешируем введенный пароль с той же солью
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] actualHash = pbkdf2.GetBytes(HashSize);

                    // Сравниваем хеши
                    return SlowEquals(expectedHash, actualHash);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Безопасное сравнение хешей (защита от timing attacks)
        /// </summary>
        private static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
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
            if (password.Length >= 12) score += 30;
            else if (password.Length >= 8) score += 20;
            else if (password.Length >= 6) score += 10;

            // Строчные буквы
            if (password.Any(char.IsLower)) score += 20;

            // Заглавные буквы
            if (password.Any(char.IsUpper)) score += 20;

            // Цифры
            if (password.Any(char.IsDigit)) score += 15;

            // Спецсимволы
            if (password.Any(ch => !char.IsLetterOrDigit(ch))) score += 15;

            return Math.Min(100, score);
        }
    }
}


