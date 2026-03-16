using Manager_password;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Password_manager
{
    /// <summary>
    /// Логика взаимодействия для AddPasswordWindow.xaml
    /// </summary>
    public partial class AddPasswordWindow : Window
    {
        private int _userId;
        private byte[] _masterKey;
        private PasswordEntry _editingEntry;

        public AddPasswordWindow(int userId, byte[] masterKey, PasswordEntry entry = null)
        {
            InitializeComponent();
            _userId = userId;
            _masterKey = masterKey;
            _editingEntry = entry;

            if (entry != null)
            {
                // Режим редактирования
                WindowTitle.Text = "✏️ Редактировать пароль";
                btnSave.Content = "Обновить";

                txtTitle.Text = entry.Title;
                txtUsername.Text = entry.Username;
                txtWebsite.Text = entry.Website;
                txtNotes.Text = entry.Notes;

                // ВАЖНО: entry.EncryptedPassword содержит ВРЕМЕННО расшифрованный пароль
                txtPassword.Password = entry.EncryptedPassword;
            }
        }

        /// <summary>
        /// Генерация случайного пароля
        /// </summary>
        private string GenerateStrongPassword(int length = 16)
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()_-+=<>?";

            string allChars = uppercase + lowercase + digits + special;
            Random random = new Random();

            char[] password = new char[length];

            // Гарантируем хотя бы по одному символу каждого типа
            password[0] = uppercase[random.Next(uppercase.Length)];
            password[1] = lowercase[random.Next(lowercase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = special[random.Next(special.Length)];

            // Заполняем остальные символы случайно
            for (int i = 4; i < length; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Перемешиваем массив
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }

        /// <summary>
        /// Обработчик кнопки генерации пароля
        /// </summary>
        private void GeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string password = GenerateStrongPassword(16);
                txtPassword.Password = password;

                // Копируем в буфер обмена
                Clipboard.SetText(password);

                // Визуальная обратная связь
                var button = sender as Button;
                if (button != null)
                {
                    var originalContent = button.Content;
                    var originalBackground = button.Background;

                    button.Content = "✓";
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
                }

                // Показываем уведомление
                MessageBox.Show($"Пароль сгенерирован и скопирован в буфер обмена!\n\nПароль: {password}",
                    "Генератор", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации пароля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Отмена - закрытие окна
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Закрытие окна (кнопка ✕)
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Перетаскивание окна
        /// </summary>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        /// <summary>
        /// Обработка клавиш
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                BtnCancel_Click(sender, e);
            }
            else if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Ctrl+Enter - сохранить
                BtnSave_Click(sender, e);
            }
        }

        #region Навигация по клавише Enter

        private void TxtTitle_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtUsername.Focus();
            }
        }

        private void TxtUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtPassword.Focus();
            }
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtWebsite.Focus();
            }
        }

        private void TxtWebsite_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtNotes.Focus();
            }
        }

        private void TxtNotes_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                BtnSave_Click(sender, e);
            }
            else if (e.Key == Key.Enter)
            {
                // Обычный Enter в заметках переводит на новую строку
                // Ничего не делаем
            }
        }

        #endregion

        /// <summary>
        /// Сохранение пароля
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string title = txtTitle.Text.Trim();
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Password;
                string website = txtWebsite.Text.Trim();
                string notes = txtNotes.Text.Trim();

                // Валидация
                if (string.IsNullOrEmpty(title))
                {
                    MessageBox.Show("Введите название", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtTitle.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(username))
                {
                    MessageBox.Show("Введите логин", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtUsername.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Введите пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPassword.Focus();
                    return;
                }

                // Проверка сложности пароля (опционально)
                if (password.Length < 6)
                {
                    var result = MessageBox.Show(
                        "Пароль слишком короткий. Рекомендуется использовать минимум 8 символов.\n\nВсё равно продолжить?",
                        "Предупреждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                        return;
                }

                using (var db = new AppDbContext())
                {
                    if (_editingEntry == null)
                    {
                        // Новый пароль
                        string encryptedPassword = PasswordEncryptor.Encrypt(password, _masterKey);

                        var newEntry = new PasswordEntry
                        {
                            UserId = _userId,
                            Title = title,
                            Username = username,
                            EncryptedPassword = encryptedPassword,
                            Website = website,
                            Notes = notes,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            AccessCount = 0
                        };

                        db.PasswordEntries.Add(newEntry);
                        db.SaveChanges();

                        MessageBox.Show("Пароль успешно сохранен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Обновление существующего пароля
                        var entry = db.PasswordEntries.Find(_editingEntry.Id);
                        if (entry != null)
                        {
                            entry.Title = title;
                            entry.Username = username;
                            entry.EncryptedPassword = PasswordEncryptor.Encrypt(password, _masterKey);
                            entry.Website = website;
                            entry.Notes = notes;
                            entry.UpdatedAt = DateTime.Now;

                            db.SaveChanges();

                            MessageBox.Show("Пароль успешно обновлен!", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}