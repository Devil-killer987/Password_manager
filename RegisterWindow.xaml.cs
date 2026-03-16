using Manager_password;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

                // Отключаем кнопку регистрации до совпадения паролей
                btnRegister.IsEnabled = false;
            }

            /// <summary>
            /// Генерация случайного пароля
            /// </summary>
            private string GenerateRandomPassword(int length = 16)
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

                // Заполняем остальные символы
                for (int i = 4; i < length; i++)
                {
                    password[i] = allChars[random.Next(allChars.Length)];
                }

                // Перемешиваем
                return new string(password.OrderBy(x => random.Next()).ToArray());
            }

            /// <summary>
            /// Обработчик кнопки генерации пароля
            /// </summary>
            private void GeneratePassword_Click(object sender, RoutedEventArgs e)
            {
                try
                {
                    string newPassword = GenerateRandomPassword(16);
                    txtPassword.Password = newPassword;
                    txtConfirmPassword.Password = newPassword;

                    // Копируем в буфер обмена
                    Clipboard.SetText(newPassword);

                    // Обновляем индикатор сложности
                    TxtPassword_PasswordChanged(sender, e);
                    CheckPasswordsMatch();

                    MessageBox.Show($"Сгенерирован пароль:\n\n{newPassword}\n\nПароль скопирован в буфер обмена.",
                        "Генератор", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при генерации пароля: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            /// <summary>
            /// Проверка сложности пароля
            /// </summary>
            private int CheckPasswordStrength(string password)
            {
                if (string.IsNullOrEmpty(password))
                    return 0;

                int score = 0;

                // Длина
                if (password.Length >= 12) score += 30;
                else if (password.Length >= 8) score += 20;
                else if (password.Length >= 6) score += 10;

                // Разнообразие символов
                if (password.Any(char.IsLower)) score += 20;
                if (password.Any(char.IsUpper)) score += 20;
                if (password.Any(char.IsDigit)) score += 15;
                if (password.Any(ch => !char.IsLetterOrDigit(ch))) score += 15;

                return Math.Min(100, score);
            }

            /// <summary>
            /// Проверка совпадения паролей
            /// </summary>
            private void CheckPasswordsMatch()
            {
                string password = txtPassword.Password;
                string confirmPassword = txtConfirmPassword.Password;

                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
                {
                    PasswordMatchText.Text = "";
                    btnRegister.IsEnabled = false;
                    return;
                }

                if (password == confirmPassword)
                {
                    PasswordMatchText.Text = "✓ Пароли совпадают";
                    PasswordMatchText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
                    btnRegister.IsEnabled = true;
                }
                else
                {
                    PasswordMatchText.Text = "✗ Пароли не совпадают";
                    PasswordMatchText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                    btnRegister.IsEnabled = false;
                }
            }

            /// <summary>
            /// Обновление индикатора сложности пароля
            /// </summary>
            private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
            {
                string password = txtPassword.Password;

                if (string.IsNullOrEmpty(password))
                {
                    PasswordStrengthBar.Value = 0;
                    PasswordStrengthText.Text = "";
                    return;
                }

                int strength = CheckPasswordStrength(password);
                PasswordStrengthBar.Value = strength;

                if (strength < 30)
                {
                    PasswordStrengthText.Text = "Слабый";
                    PasswordStrengthText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                }
                else if (strength < 60)
                {
                    PasswordStrengthText.Text = "Средний";
                    PasswordStrengthText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
                }
                else if (strength < 80)
                {
                    PasswordStrengthText.Text = "Хороший";
                    PasswordStrengthText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                }
                else
                {
                    PasswordStrengthText.Text = "Отличный";
                    PasswordStrengthText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
                }
            }

            /// <summary>
            /// Обработчик изменения поля подтверждения пароля
            /// </summary>
            private void TxtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
            {
                CheckPasswordsMatch();
            }

            /// <summary>
            /// Навигация по клавише Enter
            /// </summary>
            private void TxtUsername_KeyDown(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter)
                {
                    txtEmail.Focus();
                }
            }

            private void TxtEmail_KeyDown(object sender, KeyEventArgs e)
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
                    txtConfirmPassword.Focus();
                }
            }

            private void TxtConfirmPassword_KeyDown(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter && btnRegister.IsEnabled)
                {
                    BtnRegister_Click(sender, e);
                }
            }

            /// <summary>
            /// Обработчик клавиш окна
            /// </summary>
            private void Window_KeyDown(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Escape)
                {
                    BtnClose_Click(sender, e);
                }
            }

            /// <summary>
            /// Регистрация нового пользователя
            /// </summary>
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
                    int strength = CheckPasswordStrength(password);
                    if (strength < 50)
                    {
                        var result = MessageBox.Show(
                            "Пароль слишком слабый. Рекомендуется использовать более надежный пароль.\n\nВсё равно продолжить?",
                            "Предупреждение",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.No)
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

                        // Генерация мастер-ключа
                        byte[] masterKey = new byte[32];
                        using (var rng = RandomNumberGenerator.Create())
                        {
                            rng.GetBytes(masterKey);
                        }

                        // Шифрование мастер-ключа
                        string encryptedMasterKey = PasswordEncryptor.EncryptMasterKey(masterKey, password);

                        // Создание пользователя
                        var newUser = new User
                        {
                            Name = username,
                            Email = email,
                            PasswordHash = passwordHash,
                            CreatedAt = DateTime.Now,
                            LastLoginAt = null,
                            IsActive = true,
                            EncryptedMasterKey = encryptedMasterKey,
                            PasswordEntries = new System.Collections.Generic.List<PasswordEntry>()
                        };

                        db.Users.Add(newUser);
                        db.SaveChanges();

                        MessageBox.Show("Регистрация прошла успешно!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Открываем главное окно
                        MainWindow mainWindow = new MainWindow(newUser.Id, password);
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

            /// <summary>
            /// Проверка корректности email
            /// </summary>
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

            /// <summary>
            /// Переход к окну входа
            /// </summary>
            private void LoginLink_Click(object sender, RoutedEventArgs e)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }

            /// <summary>
            /// Закрытие окна
            /// </summary>
            private void BtnClose_Click(object sender, RoutedEventArgs e)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }

            /// <summary>
            /// Перетаскивание окна
            /// </summary>
            private void Window_MouseDown(object sender, MouseButtonEventArgs e)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    this.DragMove();
            }
        }
    }

