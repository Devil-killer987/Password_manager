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
        private Random _random = new Random();

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
                txtPassword.Password = entry.EncryptedPassword; // В реальном проекте нужно дешифровать
                txtWebsite.Text = entry.Website;
                txtNotes.Text = entry.Notes;
            }
        }

        private void GeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            // Простая генерация пароля
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var password = new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[_random.Next(s.Length)]).ToArray());

            txtPassword.Password = password;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
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
                        EncryptedPassword = password, // В реальном проекте нужно шифровать!
                        Website = website,
                        Notes = notes,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    db.PasswordEntries.Add(newEntry);
                }
                else
                {
                    // Обновление существующего
                    var entry = db.PasswordEntries.Find(_editingEntry.Id);
                    if (entry != null)
                    {
                        entry.Title = title;
                        entry.Username = username;
                        entry.EncryptedPassword = password; // В реальном проекте нужно шифровать!
                        entry.Website = website;
                        entry.Notes = notes;
                        entry.UpdatedAt = DateTime.Now;
                    }
                }

                db.SaveChanges();
            }

            DialogResult = true;
            Close();
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
