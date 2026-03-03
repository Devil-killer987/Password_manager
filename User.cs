using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manager_password
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; } 
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; } // Дата последнего входа
        public bool IsActive { get; set; } = true; // Активен ли аккаунт

        // Навигационное свойство для связанных паролей
        public ICollection<PasswordEntry> PasswordEntries { get; set; }
    }

    public class PasswordEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Username { get; set; }
        public string EncryptedPassword { get; set; } // Здесь храним зашифрованный пароль
        public string Website { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int AccessCount { get; set; } // Счетчик доступа к паролю
        public DateTime? LastAccessedAt { get; set; } // Дата последнего доступа

        // Навигационное свойство
        public User User { get; set; }
    }
}
