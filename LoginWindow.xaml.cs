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

            // Создание базы данных при первом запуске
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
                    // Ищем пользователя по имени
                    var user = db.Users.FirstOrDefault(u => u.Name == username);

                    if (user != null)
                    {
                        // Проверяем пароль с использованием BCrypt
                        if (PasswordHasher.VerifyPassword(password, user.PasswordHash))
                        {
                            // Обновляем дату последнего входа
                            user.LastLoginAt = DateTime.Now;
                            db.SaveChanges();

                            MainWindow mainWindow = new MainWindow(user.Id);
                            mainWindow.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Неверное имя пользователя или пароль", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Неверное имя пользователя или пароль", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}

