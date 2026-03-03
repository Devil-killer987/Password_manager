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
            // Уникальный индекс для имени пользователя
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Name)
                .IsUnique();

            // Уникальный индекс для email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Настройка связи между User и PasswordEntry
            modelBuilder.Entity<PasswordEntry>()
                .HasOne(p => p.User)
                .WithMany(u => u.PasswordEntries)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Индекс для быстрого поиска по UserId
            modelBuilder.Entity<PasswordEntry>()
                .HasIndex(p => p.UserId);
        }
    }
}
