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
        private Random _random = new Random();

        // Поля для хранения последних настроек генератора
        private int _lastPasswordLength = 16;
        private bool _lastIncludeUppercase = true;
        private bool _lastIncludeLowercase = true;
        private bool _lastIncludeDigits = true;
        private bool _lastIncludeSpecial = true;
        private bool _lastExcludeSimilar = false;
        private bool _lastExcludeAmbiguous = false;
        private bool _lastNoConsecutive = false;

        // Вспомогательный класс для ComboBox
        public class CategoryItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public AddPasswordWindow(int userId, byte[] masterKey, PasswordEntry entry = null)
        {
            InitializeComponent();
            _userId = userId;
            _masterKey = masterKey;
            _editingEntry = entry;

            LoadCategories();

            if (entry != null)
            {
                WindowTitle.Text = "✏️ Редактировать пароль";
                btnSave.Content = "Обновить";

                txtTitle.Text = entry.Title;
                txtUsername.Text = entry.Username;
                txtWebsite.Text = entry.Website;
                txtNotes.Text = entry.Notes;
                txtPassword.Password = entry.EncryptedPassword;

                // Выбираем категорию
                if (entry.CategoryId.HasValue)
                {
                    cmbCategory.SelectedValue = entry.CategoryId.Value;
                }
            }
        }

        private void LoadCategories()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var categories = db.Categories
                        .Where(c => c.UserId == _userId)
                        .OrderBy(c => c.DisplayOrder)
                        .ToList();

                    var categoryList = new System.Collections.Generic.List<CategoryItem>();

                    // Добавляем пункт "Без категории"
                    categoryList.Add(new CategoryItem
                    {
                        Id = 0,
                        Name = "🚫 Без категории"
                    });

                    // Добавляем категории пользователя
                    foreach (var cat in categories)
                    {
                        categoryList.Add(new CategoryItem
                        {
                            Id = cat.Id,
                            Name = $"{cat.Icon} {cat.Name}"
                        });
                    }

                    cmbCategory.ItemsSource = categoryList;
                    cmbCategory.SelectedValue = 0; // "Без категории" по умолчанию

                    // Если редактируем существующий пароль, выбираем его категорию
                    if (_editingEntry != null && _editingEntry.CategoryId.HasValue)
                    {
                        cmbCategory.SelectedValue = _editingEntry.CategoryId.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #region Генерация паролей

        /// <summary>
        /// Быстрая генерация пароля (простая)
        /// </summary>
        private void GenerateSimplePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string password = GenerateStrongPassword(16);
                txtPassword.Password = password;
                Clipboard.SetText(password);

                ShowTemporaryButtonFeedback(sender as Button, "✓");

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
        /// Генерация с расширенными настройками
        /// </summary>
        private void GenerateWithSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new PasswordGeneratorSettingsWindow(
                    _lastPasswordLength,
                    _lastIncludeUppercase,
                    _lastIncludeLowercase,
                    _lastIncludeDigits,
                    _lastIncludeSpecial,
                    _lastExcludeSimilar,
                    _lastExcludeAmbiguous,
                    _lastNoConsecutive
                );

                settingsWindow.Owner = this;

                if (settingsWindow.ShowDialog() == true)
                {
                    txtPassword.Password = settingsWindow.GeneratedPassword;
                    Clipboard.SetText(settingsWindow.GeneratedPassword);

                    // Сохраняем настройки для следующего раза
                    _lastPasswordLength = settingsWindow.PasswordLength;
                    _lastIncludeUppercase = settingsWindow.IncludeUppercase;
                    _lastIncludeLowercase = settingsWindow.IncludeLowercase;
                    _lastIncludeDigits = settingsWindow.IncludeDigits;
                    _lastIncludeSpecial = settingsWindow.IncludeSpecial;
                    _lastExcludeSimilar = settingsWindow.ExcludeSimilar;
                    _lastExcludeAmbiguous = settingsWindow.ExcludeAmbiguous;
                    _lastNoConsecutive = settingsWindow.NoConsecutive;

                    ShowTemporaryButtonFeedback(sender as Button, "✓");

                    MessageBox.Show($"Пароль сгенерирован с заданными параметрами и скопирован в буфер обмена!\n\nПароль: {settingsWindow.GeneratedPassword}",
                        "Генератор", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации пароля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Продвинутая генерация пароля с полными настройками
        /// </summary>
        private string GenerateStrongPassword(int length,
                                               bool includeUppercase = true,
                                               bool includeLowercase = true,
                                               bool includeDigits = true,
                                               bool includeSpecial = true,
                                               bool excludeSimilar = false,
                                               bool excludeAmbiguous = false,
                                               bool noConsecutive = false)
        {
            string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string lowercase = "abcdefghijklmnopqrstuvwxyz";
            string digits = "0123456789";
            string special = "!@#$%^&*()_-+=<>?";

            // Исключаем похожие символы
            if (excludeSimilar)
            {
                uppercase = uppercase.Replace("I", "").Replace("L", "").Replace("O", "");
                lowercase = lowercase.Replace("i", "").Replace("l", "").Replace("o", "");
                digits = digits.Replace("1", "").Replace("0", "");
            }

            // Исключаем неоднозначные символы
            if (excludeAmbiguous)
            {
                special = special.Replace("{", "").Replace("}", "")
                                .Replace("[", "").Replace("]", "")
                                .Replace("(", "").Replace(")", "")
                                .Replace("/", "").Replace("\\", "");
            }

            // Собираем разрешенные символы
            StringBuilder allowedChars = new StringBuilder();

            if (includeUppercase && !string.IsNullOrEmpty(uppercase))
                allowedChars.Append(uppercase);
            if (includeLowercase && !string.IsNullOrEmpty(lowercase))
                allowedChars.Append(lowercase);
            if (includeDigits && !string.IsNullOrEmpty(digits))
                allowedChars.Append(digits);
            if (includeSpecial && !string.IsNullOrEmpty(special))
                allowedChars.Append(special);

            // Если ничего не выбрано, используем все
            if (allowedChars.Length == 0)
                allowedChars.Append(uppercase + lowercase + digits);

            string allChars = allowedChars.ToString();

            // Генерируем пароль
            StringBuilder password = new StringBuilder();

            // Добавляем по одному символу каждого типа, если они выбраны
            if (includeUppercase && !string.IsNullOrEmpty(uppercase))
                password.Append(uppercase[_random.Next(uppercase.Length)]);
            if (includeLowercase && !string.IsNullOrEmpty(lowercase))
                password.Append(lowercase[_random.Next(lowercase.Length)]);
            if (includeDigits && !string.IsNullOrEmpty(digits))
                password.Append(digits[_random.Next(digits.Length)]);
            if (includeSpecial && !string.IsNullOrEmpty(special))
                password.Append(special[_random.Next(special.Length)]);

            // Заполняем остальную длину
            while (password.Length < length)
            {
                char nextChar;
                do
                {
                    nextChar = allChars[_random.Next(allChars.Length)];
                } while (noConsecutive && password.Length > 0 && password[password.Length - 1] == nextChar);

                password.Append(nextChar);
            }

            // Перемешиваем
            return ShuffleString(password.ToString());
        }

        /// <summary>
        /// Простая генерация (для обратной совместимости)
        /// </summary>
        private string GenerateStrongPassword(int length = 16)
        {
            return GenerateStrongPassword(length, true, true, true, true, false, false, false);
        }

        /// <summary>
        /// Перемешивание строки
        /// </summary>
        private string ShuffleString(string input)
        {
            char[] array = input.ToCharArray();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (array[j], array[i]) = (array[i], array[j]);
            }
            return new string(array);
        }

        /// <summary>
        /// Визуальная обратная связь для кнопок
        /// </summary>
        private void ShowTemporaryButtonFeedback(Button button, string symbol)
        {
            if (button != null)
            {
                var originalContent = button.Content;
                var originalBackground = button.Background;

                button.Content = symbol;
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
        }

        #endregion

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
                cmbCategory.Focus();
            }
        }

        #endregion

        #region Обработчики окон

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                BtnCancel_Click(sender, e);
            }
            else if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                BtnSave_Click(sender, e);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region Сохранение

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string title = txtTitle.Text.Trim();
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Password;
                string website = txtWebsite.Text.Trim();
                string notes = txtNotes.Text.Trim();

                // Получаем выбранную категорию
                int? categoryId = null;
                if (cmbCategory.SelectedValue != null)
                {
                    int selectedId = (int)cmbCategory.SelectedValue;
                    if (selectedId > 0)
                    {
                        categoryId = selectedId;
                    }
                }

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

                // Проверка сложности пароля
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
                            CategoryId = categoryId,
                            Title = title,
                            Username = username,
                            EncryptedPassword = encryptedPassword,
                            Website = website,
                            Notes = notes,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            AccessCount = 0,
                            IsFavorite = false
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
                            entry.CategoryId = categoryId;
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

        #endregion
    }
}