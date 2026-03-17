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
        private string _userPassword; // Пароль пользователя для расшифровки мастер-ключа
        private byte[] _masterKey; // Расшифрованный мастер-ключ для шифрования паролей

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
                LoadMasterKey();
                LoadPasswords();

                // Устанавливаем имя пользователя в статусную строку
                UserInfoText.Text = $"Пользователь: {GetCurrentUsername()}";
            }
            else
            {
                MessageBox.Show("Ошибка инициализации пользователя", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        /// <summary>
        /// Загрузка и расшифровка мастер-ключа из базы данных
        /// </summary>
        private void LoadMasterKey()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var user = db.Users.Find(_currentUserId);
                    if (user == null || string.IsNullOrEmpty(user.EncryptedMasterKey))
                    {
                        throw new Exception("Мастер-ключ не найден в базе данных");
                    }

                    // Расшифровываем мастер-ключ с использованием пароля пользователя
                    _masterKey = PasswordEncryptor.DecryptMasterKey(user.EncryptedMasterKey, _userPassword);

                    if (_masterKey == null || _masterKey.Length != 32)
                    {
                        throw new Exception("Неверный размер мастер-ключа");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке мастер-ключа: {ex.Message}\n\nВозможно, неверный пароль.",
                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
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
                            p.Website.Contains(searchText) ||
                            p.Notes.Contains(searchText));
                    }

                    var passwords = query
                        .OrderByDescending(p => p.UpdatedAt)
                        .ToList();

                    var displayList = new List<PasswordEntryDisplay>();

                    foreach (var p in passwords)
                    {
                        try
                        {
                            string decryptedPassword = PasswordEncryptor.Decrypt(p.EncryptedPassword, _masterKey);

                            displayList.Add(new PasswordEntryDisplay
                            {
                                Id = p.Id,
                                Title = p.Title,
                                Username = p.Username,
                                DecryptedPassword = decryptedPassword,
                                Website = p.Website,
                                Notes = p.Notes,
                                CreatedAt = p.CreatedAt,
                                UpdatedAt = p.UpdatedAt,
                                AccessCount = p.AccessCount,
                                LastAccessedAt = p.LastAccessedAt
                            });
                        }
                        catch (Exception ex)
                        {
                            displayList.Add(new PasswordEntryDisplay
                            {
                                Id = p.Id,
                                Title = p.Title + " [Ошибка расшифровки]",
                                Username = p.Username,
                                DecryptedPassword = "*** ОШИБКА ***",
                                Website = p.Website,
                                Notes = $"Не удалось расшифровать: {ex.Message}",
                                CreatedAt = p.CreatedAt,
                                UpdatedAt = p.UpdatedAt
                            });
                        }
                    }

                    PasswordsList.ItemsSource = displayList;

                    // Обновляем статус (ТЕПЕРЬ ЭТО РАБОТАЕТ)
                    StatusText.Text = $"Записей: {displayList.Count}";

                    // Обновляем информацию о пользователе
                    using (var userDb = new AppDbContext())
                    {
                        var user = userDb.Users.Find(_currentUserId);
                        if (user != null)
                        {
                            if (user != null && !string.IsNullOrEmpty(user.EncryptedUsername))
                            {
                                string username = DeterministicEncryption.Decrypt(user.EncryptedUsername, _masterKey);
                                UserInfoText.Text = $"Пользователь: {username}";
                            }
                        }
                    }
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
                    // Обновляем счетчик доступа в базе
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

                    // Показываем пароль в отдельном окне
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
            try
            {
                // Передаем мастер-ключ для шифрования нового пароля
                var dialog = new AddPasswordWindow(_currentUserId, _masterKey);
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    LoadPasswords();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении пароля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string GetCurrentUsername()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var user = db.Users.Find(_currentUserId);
                    if (user != null && !string.IsNullOrEmpty(user.EncryptedUsername))
                    {
                        // Расшифровываем логин для отображения
                        return DeterministicEncryption.Decrypt(user.EncryptedUsername, _masterKey);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении имени пользователя: {ex.Message}");
            }
            return "Пользователь";
        }


        private void EditPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var passwordEntry = button?.DataContext as PasswordEntryDisplay;

                if (passwordEntry != null)
                {
                    using (var db = new AppDbContext())
                    {
                        // Загружаем оригинальную запись из базы данных
                        var originalEntry = db.PasswordEntries.Find(passwordEntry.Id);

                        if (originalEntry != null)
                        {
                            // Расшифровываем пароль для редактирования
                            string decryptedPassword = PasswordEncryptor.Decrypt(originalEntry.EncryptedPassword, _masterKey);

                            // Создаем временный объект для редактирования
                            var entryForEdit = new PasswordEntry
                            {
                                Id = originalEntry.Id,
                                UserId = originalEntry.UserId,
                                Title = originalEntry.Title,
                                Username = originalEntry.Username,
                                EncryptedPassword = decryptedPassword, // ВРЕМЕННО храним расшифрованный пароль
                                Website = originalEntry.Website,
                                Notes = originalEntry.Notes,
                                CreatedAt = originalEntry.CreatedAt
                            };

                            var dialog = new AddPasswordWindow(_currentUserId, _masterKey, entryForEdit);
                            dialog.Owner = this;

                            if (dialog.ShowDialog() == true)
                            {
                                LoadPasswords();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeletePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var passwordEntry = button?.DataContext as PasswordEntryDisplay;

                if (passwordEntry != null)
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить пароль для '{passwordEntry.Title}'?\n\nЭто действие нельзя отменить.",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

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

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите выйти?",
                "Выход",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Очищаем мастер-ключ из памяти
                if (_masterKey != null)
                {
                    Array.Clear(_masterKey, 0, _masterKey.Length);
                    _masterKey = null;
                }

                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Очищаем мастер-ключ при закрытии окна
            if (_masterKey != null)
            {
                Array.Clear(_masterKey, 0, _masterKey.Length);
                _masterKey = null;
            }
        }

        private void CopyUsername_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var passwordEntry = button?.DataContext as PasswordEntryDisplay;

                if (passwordEntry != null && !string.IsNullOrEmpty(passwordEntry.Username))
                {
                    Clipboard.SetText(passwordEntry.Username);

                    // Визуальная обратная связь
                    var originalContent = button.Content;
                    var originalBackground = button.Background;

                    button.Content = "✅";
                    button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));

                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(1);
                    timer.Tick += (s, args) =>
                    {
                        button.Content = originalContent;
                        button.Background = originalBackground;
                        timer.Stop();
                    };
                    timer.Start();

                    // Обновляем статус (если есть StatusText)
                    try { StatusText.Text = "Логин скопирован в буфер обмена"; } catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при копировании: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var passwordEntry = button?.DataContext as PasswordEntryDisplay;

                if (passwordEntry != null && !string.IsNullOrEmpty(passwordEntry.DecryptedPassword))
                {
                    Clipboard.SetText(passwordEntry.DecryptedPassword);

                    // Визуальная обратная связь
                    var originalContent = button.Content;
                    var originalBackground = button.Background;

                    button.Content = "✅";
                    button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));

                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(1);
                    timer.Tick += (s, args) =>
                    {
                        button.Content = originalContent;
                        button.Background = originalBackground;
                        timer.Stop();
                    };
                    timer.Start();

                    // Обновляем статус (если есть StatusText)
                    try { StatusText.Text = "Пароль скопирован в буфер обмена"; } catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при копировании: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // При двойном клике показываем пароль
            var listView = sender as ListView;
            var passwordEntry = listView?.SelectedItem as PasswordEntryDisplay;

            if (passwordEntry != null)
            {
                // Вызываем метод показа пароля
                ShowPasswordForEntry(passwordEntry);
            }
        }

        private void ShowPasswordForEntry(PasswordEntryDisplay passwordEntry)
        {
            try
            {
                // Обновляем счетчик доступа в базе
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

                // Показываем пароль в отдельном окне
                var dialog = new PasswordDisplayWindow(
                    passwordEntry.Title,
                    passwordEntry.Username,
                    passwordEntry.DecryptedPassword,
                    passwordEntry.Website
                );
                dialog.Owner = this;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отображении пароля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Вспомогательный класс для отображения паролей в списке
        /// </summary>
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
            public bool IsFavorite { get; set; }
            public int AccessCount { get; set; }
            public DateTime? LastAccessedAt { get; set; }

            // Свойства для отображения в UI
            public string DisplayTitle => string.IsNullOrEmpty(Website) ? Title : $"{Title} ({Website})";
            public string LastAccessedDisplay => LastAccessedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Никогда";
        }
    }
}

