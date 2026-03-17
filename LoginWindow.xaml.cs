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
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            try
            {
                using (var db = new AppDbContext())
                {
                    db.Database.EnsureCreated();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании базы данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Password;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var db = new AppDbContext())
                {
                    // Получаем всех пользователей
                    var users = db.Users.ToList();
                    User foundUser = null;
                    byte[] masterKey = null;

                    // Ищем пользователя, расшифровывая логин каждого
                    foreach (var user in users)
                    {
                        try
                        {
                            // Пытаемся расшифровать мастер-ключ паролем
                            masterKey = PasswordEncryptor.DecryptMasterKey(user.EncryptedMasterKey, password);

                            // Если успешно, расшифровываем логин
                            string decryptedUsername = DeterministicEncryption.Decrypt(user.EncryptedUsername, masterKey);

                            if (decryptedUsername == username)
                            {
                                foundUser = user;
                                break;
                            }
                        }
                        catch
                        {
                            // Неправильный пароль для этого пользователя - пропускаем
                            continue;
                        }
                    }

                    if (foundUser == null)
                    {
                        MessageBox.Show("Неверное имя пользователя или пароль", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Проверяем пароль через хеш
                    if (!PasswordHasher.VerifyPassword(password, foundUser.PasswordHash))
                    {
                        MessageBox.Show("Неверное имя пользователя или пароль", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Вход выполнен успешно
                    foundUser.LastLoginAt = DateTime.Now;
                    db.SaveChanges();

                    MainWindow mainWindow = new MainWindow(foundUser.Id, password);
                    mainWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }

        private void TxtUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtPassword.Focus();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                BtnClose_Click(sender, e);
            }
        }
    }
}

