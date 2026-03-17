using Manager_password;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Password_manager
{
    public class Category
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; } // Эмодзи или иконка
        public string Color { get; set; } // Цвет в HEX
        public int DisplayOrder { get; set; } // Порядок отображения
        public DateTime CreatedAt { get; set; }
        public bool IsDefault { get; set; } // Системная категория (нельзя удалить)

        // Навигационные свойства
        public User User { get; set; }
        public ICollection<PasswordEntry> PasswordEntries { get; set; }
    }
}
