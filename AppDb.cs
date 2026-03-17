using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Manager_password
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<PasswordEntry> PasswordEntries { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "password_manager.db");

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка связей
            modelBuilder.Entity<PasswordEntry>()
                .HasOne(p => p.User)
                .WithMany(u => u.PasswordEntries)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Индексы для быстрого поиска
            // ИСПРАВЛЕНО: Вместо Name используем EncryptedUsername
            modelBuilder.Entity<User>()
                .HasIndex(u => u.EncryptedUsername)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<PasswordEntry>()
                .HasIndex(p => p.Website);

            // Добавляем индекс для поиска по зашифрованному логину
            modelBuilder.Entity<PasswordEntry>()
                .HasIndex(p => p.Username);
        }
    }
}
