using Microsoft.EntityFrameworkCore;
using Password_manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Manager_password
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _currentUserId;
        private string _userPassword; // Пароль пользователя для расшифровки
        private byte[] _masterKey;

        public MainWindow() : this(0, "")
        {
        }

        public MainWindow(int userId, string userPassword)
        {
            InitializeComponent();
            _currentUserId = userId;
            _userPassword = userPassword;

            if (userId > 0 && !string.IsNullOrEmpty(userPassword))
            {
                // Создаем мастер-ключ из пароля
                _masterKey = PasswordEncryptor.DeriveKeyFromPassword(userPassword);
                LoadPasswords();
            }
        }

        private void LoadPasswords(string searchText = "")
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var query = db.PasswordEntries
                        .Where(p => p.UserId == _currentUserId);

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        query = query.Where(p =>
                            p.Title.Contains(searchText) ||
                            p.Username.Contains(searchText) ||
                            p.Website.Contains(searchText));
                    }

                    var passwords = query
                        .OrderByDescending(p => p.UpdatedAt)
                        .ToList();

                    // Для отображения в списке создаем копии с расшифрованными паролями
                    var displayList = passwords.Select(p => new PasswordEntryDisplay
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Username = p.Username,
                        DecryptedPassword = PasswordEncryptor.Decrypt(p.EncryptedPassword, _masterKey),
                        Website = p.Website,
                        Notes = p.Notes,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    }).ToList();

                    PasswordsList.ItemsSource = displayList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке паролей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var passwordEntry = button?.DataContext as PasswordEntryDisplay;

                if (passwordEntry != null)
                {
                    // Обновляем счетчик доступа
                    using (var db = new AppDbContext())
                    {
                        var entry = db.PasswordEntries.Find(passwordEntry.Id);
                        if (entry != null)
                        {
                            entry.AccessCount++;
                            entry.LastAccessedAt = DateTime.Now;
                            db.SaveChanges();
                        }
                    }

                    // Показываем пароль
                    var dialog = new PasswordDisplayWindow(
                        passwordEntry.Title,
                        passwordEntry.Username,
                        passwordEntry.DecryptedPassword,
                        passwordEntry.Website
                    );
                    dialog.Owner = this;
                    dialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отображении пароля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddPassword_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddPasswordWindow(_currentUserId, _masterKey);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                LoadPasswords();
            }
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите выйти?",
                "Выход",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
        private void DeletePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var passwordEntry = button?.DataContext as PasswordEntry;

                if (passwordEntry != null)
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить пароль для '{passwordEntry.Title}'?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        using (var db = new AppDbContext())
                        {
                            var entry = db.PasswordEntries.Find(passwordEntry.Id);
                            if (entry != null)
                            {
                                db.PasswordEntries.Remove(entry);
                                db.SaveChanges();
                                LoadPasswords();

                                MessageBox.Show("Пароль успешно удален!", "Успех",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            LoadPasswords(SearchBox.Text);
        }

        private void EditPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var passwordEntry = button?.DataContext as PasswordEntryDisplay;

                if (passwordEntry != null)
                {
                    // Создаем временный объект PasswordEntry для редактирования
                    var entry = new PasswordEntry
                    {
                        Id = passwordEntry.Id,
                        UserId = _currentUserId,
                        Title = passwordEntry.Title,
                        Username = passwordEntry.Username,
                        EncryptedPassword = passwordEntry.DecryptedPassword, // Для редактирования используем расшифрованный
                        Website = passwordEntry.Website,
                        Notes = passwordEntry.Notes
                    };

                    var dialog = new AddPasswordWindow(_currentUserId, _masterKey, entry);
                    dialog.Owner = this;
                    if (dialog.ShowDialog() == true)
                    {
                        LoadPasswords();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Вспомогательный класс для отображения
    public class PasswordEntryDisplay
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Username { get; set; }
        public string DecryptedPassword { get; set; }
        public string Website { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

