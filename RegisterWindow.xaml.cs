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
    /// Логика взаимодействия для RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = txtUsername.Text.Trim();
                string email = txtEmail.Text.Trim();
                string password = txtPassword.Password;
                string confirmPassword = txtConfirmPassword.Password;

                // Валидация
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (password != confirmPassword)
                {
                    MessageBox.Show("Пароли не совпадают", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка сложности пароля
                var strengthCheck = PasswordHasher.ValidatePasswordStrength(password);
                if (!strengthCheck.IsValid)
                {
                    MessageBox.Show($"Пароль недостаточно надежный:\n{strengthCheck.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!IsValidEmail(email))
                {
                    MessageBox.Show("Введите корректный email адрес", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var db = new AppDbContext())
                {
                    // Проверка существования пользователя
                    if (db.Users.Any(u => u.Name == username))
                    {
                        MessageBox.Show("Пользователь с таким именем уже существует", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (db.Users.Any(u => u.Email == email))
                    {
                        MessageBox.Show("Пользователь с таким email уже существует", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Хеширование пароля
                    string passwordHash = PasswordHasher.HashPassword(password);

                    // Создание нового пользователя
                    var newUser = new User
                    {
                        Name = username,
                        Email = email,
                        PasswordHash = passwordHash, // Сохраняем хеш
                        CreatedAt = DateTime.Now,
                        IsActive = true,
                        PasswordEntries = new System.Collections.Generic.List<PasswordEntry>()
                    };

                    db.Users.Add(newUser);
                    db.SaveChanges();

                    MessageBox.Show("Регистрация прошла успешно!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Сразу входим в систему после регистрации
                    MainWindow mainWindow = new MainWindow(newUser.Id);
                    mainWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void LoginLink_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}

