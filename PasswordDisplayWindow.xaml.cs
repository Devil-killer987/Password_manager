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

            // Улучшенное отображение сервиса
            txtService.Text = string.IsNullOrEmpty(website) ? service : $"{service} - {website}";
            txtUsername.Text = username;
            txtPassword.Text = password;

            // Добавляем обработчик для копирования по двойному клику
            txtPassword.MouseDoubleClick += TxtPassword_MouseDoubleClick;
        }

        private void TxtPassword_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CopyPasswordToClipboard();
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            CopyPasswordToClipboard();
        }

        private void CopyPasswordToClipboard()
        {
            try
            {
                Clipboard.SetText(txtPassword.Text);

                // Визуальная обратная связь
                ShowCopyFeedback();

                // Показываем всплывающее уведомление
                var notification = new TextBlock
                {
                    Text = "✓ Скопировано!",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                // Добавляем уведомление временно
                CopyNotificationPanel.Children.Clear();
                CopyNotificationPanel.Children.Add(notification);

                // Убираем уведомление через 2 секунды
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(2);
                timer.Tick += (s, args) =>
                {
                    CopyNotificationPanel.Children.Clear();
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

        private void ShowCopyFeedback()
        {
            var button = FindVisualChild<Button>(this, "btnCopy");
            if (button != null)
            {
                var originalContent = button.Content;
                button.Content = "✅";
                button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));

                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (s, args) =>
                {
                    button.Content = originalContent;
                    button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                    timer.Stop();
                };
                timer.Start();
            }
        }

        private T FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                {
                    if (child is FrameworkElement element && element.Name == childName)
                        return tChild;
                }

                var result = FindVisualChild<T>(child, childName);
                if (result != null)
                    return result;
            }
            return null;
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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Ctrl+C копирует пароль
                CopyPasswordToClipboard();
                e.Handled = true;
            }
        }
    }
}

