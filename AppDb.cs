using Microsoft.EntityFrameworkCore;
using Password_manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace Manager_password
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<PasswordEntry> PasswordEntries { get; set; }
        public DbSet<Category> Categories { get; set; } // Добавлено

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(
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

            // Связь с категорией
            modelBuilder.Entity<PasswordEntry>()
                .HasOne(p => p.Category)
                .WithMany(c => c.PasswordEntries)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull); // При удалении категории, пароли остаются без категории

            modelBuilder.Entity<Category>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Индексы
            modelBuilder.Entity<User>()
                .HasIndex(u => u.EncryptedUsername)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<PasswordEntry>()
                .HasIndex(p => p.Website);

            modelBuilder.Entity<PasswordEntry>()
                .HasIndex(p => p.CategoryId);

            modelBuilder.Entity<Category>()
                .HasIndex(c => new { c.UserId, c.Name })
                .IsUnique(); // У пользователя не может быть двух категорий с одинаковым именем
        }
    }
}
