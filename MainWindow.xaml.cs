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

        public MainWindow(int userId)
        {
            InitializeComponent();
            _currentUserId = userId;
            LoadPasswords();
        }

        private void LoadPasswords(string searchText = "")
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

                PasswordsList.ItemsSource = query
                    .OrderByDescending(p => p.UpdatedAt)
                    .ToList();
            }
        }

        private void AddPassword_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddPasswordWindow(_currentUserId);
            if (dialog.ShowDialog() == true)
            {
                LoadPasswords();
            }
        }

        private void EditPassword_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var passwordEntry = button?.DataContext as PasswordEntry;

            if (passwordEntry != null)
            {
                var dialog = new AddPasswordWindow(_currentUserId, passwordEntry);
                if (dialog.ShowDialog() == true)
                {
                    LoadPasswords();
                }
            }
        }

        private void DeletePassword_Click(object sender, RoutedEventArgs e)
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
                        }
                    }
                }
            }
        }

        private void ShowPassword_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var passwordEntry = button?.DataContext as PasswordEntry;

            if (passwordEntry != null)
            {
                MessageBox.Show(
                    $"Пароль для {passwordEntry.Title}: {passwordEntry.EncryptedPassword}",
                    "Пароль",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
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
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}

