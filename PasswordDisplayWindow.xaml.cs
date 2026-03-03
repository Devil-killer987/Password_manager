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
    /// Логика взаимодействия для PasswordDisplayWindow.xaml
    /// </summary>
   
        public partial class PasswordDisplayWindow : Window
        {
            public PasswordDisplayWindow(string service, string username, string password, string website)
            {
                InitializeComponent();

                txtService.Text = string.IsNullOrEmpty(website) ? service : $"{service} ({website})";
                txtUsername.Text = username;
                txtPassword.Text = password;
            }

            private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
            {
                try
                {
                    Clipboard.SetText(txtPassword.Text);

                    // Визуальная обратная связь
                    var button = sender as Button;
                    button.Content = "✅";

                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(1);
                    timer.Tick += (s, args) =>
                    {
                        button.Content = "📋";
                        timer.Stop();
                    };
                    timer.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при копировании: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            private void BtnClose_Click(object sender, RoutedEventArgs e)
            {
                Close();
            }

            private void Window_MouseDown(object sender, MouseButtonEventArgs e)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    this.DragMove();
            }
        }
    }

