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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Простой путь к базе в папке приложения
            string dbPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "users.db");

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}
