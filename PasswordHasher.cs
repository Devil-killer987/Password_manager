using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Password_manager
{
    public static class PasswordHasher
    {
        // Хеширование пароля
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Пароль не может быть пустым");

            // Генерируем соль и хешируем пароль
            // WorkFactor = 12 - оптимальное значение (чем выше, тем дольше, но безопаснее)
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        // Проверка пароля
        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        // Проверка сложности пароля
        public static (bool IsValid, string Message) ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return (false, "Пароль не может быть пустым");

            if (password.Length < 8)
                return (false, "Пароль должен содержать минимум 8 символов");

            bool hasUpperCase = password.Any(char.IsUpper);
            bool hasLowerCase = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

            if (!hasUpperCase)
                return (false, "Пароль должен содержать хотя бы одну заглавную букву");

            if (!hasLowerCase)
                return (false, "Пароль должен содержать хотя бы одну строчную букву");

            if (!hasDigit)
                return (false, "Пароль должен содержать хотя бы одну цифру");

            if (!hasSpecialChar)
                return (false, "Пароль должен содержать хотя бы один специальный символ");

            return (true, "Пароль надежный");
        }

        // Генерация случайного пароля с проверкой сложности
        public static string GenerateStrongPassword(int length = 16)
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specialChars = "!@#$%^&*()_-+=<>?";

            var random = new Random();
            var password = new char[length];

            // Гарантируем наличие хотя бы одного символа каждого типа
            password[0] = upperCase[random.Next(upperCase.Length)];
            password[1] = lowerCase[random.Next(lowerCase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = specialChars[random.Next(specialChars.Length)];

            // Заполняем остальные символы случайно
            string allChars = upperCase + lowerCase + digits + specialChars;
            for (int i = 4; i < length; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Перемешиваем символы
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }
    }
}
