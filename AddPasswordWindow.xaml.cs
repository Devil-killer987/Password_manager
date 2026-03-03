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
        private PasswordEntry _editingEntry;

        public AddPasswordWindow(int userId, PasswordEntry entry = null)
        {
            InitializeComponent();
            _userId = userId;
            _editingEntry = entry;

            if (entry != null)
            {
                WindowTitle.Text = "✏️ Редактировать пароль";
                btnSave.Content = "Обновить";
                txtTitle.Text = entry.Title;
                txtUsername.Text = entry.Username;
                txtPassword.Password = entry.EncryptedPassword;
                txtWebsite.Text = entry.Website;
                txtNotes.Text = entry.Notes;
            }
        }

        private void GeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Используем улучшенный генератор паролей
                string password = PasswordHasher.GenerateStrongPassword(16);
                txtPassword.Password = password;

                // Показываем информацию о пароле
                var strengthCheck = PasswordHasher.ValidatePasswordStrength(password);
                MessageBox.Show($"Сгенерирован надежный пароль!\n\nПароль: {password}\n\n{strengthCheck.Message}",
                    "Генератор паролей", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации пароля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string title = txtTitle.Text.Trim();
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Password;
                string website = txtWebsite.Text.Trim();
                string notes = txtNotes.Text.Trim();

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Пожалуйста, заполните обязательные поля (Название, Логин, Пароль)",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Опционально можно проверять сложность пароля
                if (password.Length < 8)
                {
                    var result = MessageBox.Show(
                        "Пароль слишком короткий. Рекомендуется использовать пароль длиннее 8 символов.\n\nВсё равно сохранить?",
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
                        // Добавление нового пароля
                        var newEntry = new PasswordEntry
                        {
                            UserId = _userId,
                            Title = title,
                            Username = username,
                            EncryptedPassword = password, // В реальном проекте здесь должно быть шифрование
                            Website = website,
                            Notes = notes,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            AccessCount = 0
                        };

                        db.PasswordEntries.Add(newEntry);
                        db.SaveChanges();

                        MessageBox.Show("Пароль успешно добавлен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Обновление существующего
                        var entry = db.PasswordEntries.Find(_editingEntry.Id);
                        if (entry != null)
                        {
                            entry.Title = title;
                            entry.Username = username;
                            entry.EncryptedPassword = password;
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

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}
