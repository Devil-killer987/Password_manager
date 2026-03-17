using Password_manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Manager_password
{
    public class User
    {
        public int Id { get; set; }

        // Зашифрованный логин (детерминированное шифрование)
        public string EncryptedUsername { get; set; }

        // Хеш пароля (включает соль внутри себя)
        public string PasswordHash { get; set; }

        // Email пользователя
        public string Email { get; set; }

        // Дата создания
        public DateTime CreatedAt { get; set; }

        // Дата последнего входа
        public DateTime? LastLoginAt { get; set; }

        // Активен ли пользователь
        public bool IsActive { get; set; } = true;

        // Зашифрованный мастер-ключ для шифрования паролей
        public string? EncryptedMasterKey { get; set; }

        // Навигационное свойство - все пароли пользователя
        public ICollection<PasswordEntry> PasswordEntries { get; set; }
    }

    public class PasswordEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? CategoryId { get; set; } // Добавлено - связь с категорией

        public string Title { get; set; }
        public string Username { get; set; }
        public string EncryptedPassword { get; set; }
        public string Website { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int AccessCount { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public bool IsFavorite { get; set; }

        // Навигационные свойства
        public User User { get; set; }
        public Category Category { get; set; } // Добавлено
    }

}

