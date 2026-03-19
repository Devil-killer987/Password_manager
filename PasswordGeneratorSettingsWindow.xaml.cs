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
    /// Логика взаимодействия для PasswordGeneratorSettingsWindow.xaml
    /// </summary>
    public partial class PasswordGeneratorSettingsWindow : Window
    {
        public string GeneratedPassword { get; private set; }
        public int PasswordLength { get; private set; }
        public bool IncludeUppercase { get; private set; }
        public bool IncludeLowercase { get; private set; }
        public bool IncludeDigits { get; private set; }
        public bool IncludeSpecial { get; private set; }
        public bool ExcludeSimilar { get; private set; }
        public bool ExcludeAmbiguous { get; private set; }
        public bool NoConsecutive { get; private set; }

        private Random _random = new Random();

        public PasswordGeneratorSettingsWindow(int defaultLength = 16,
                                               bool uppercase = true,
                                               bool lowercase = true,
                                               bool digits = true,
                                               bool special = true,
                                               bool excludeSimilar = false,
                                               bool excludeAmbiguous = false,
                                               bool noConsecutive = false)
        {
            InitializeComponent(); // ВАЖНО: сначала инициализируем компоненты

            // Теперь можно обращаться к элементам управления
            sldLength.Value = defaultLength;
            chkUppercase.IsChecked = uppercase;
            chkLowercase.IsChecked = lowercase;
            chkDigits.IsChecked = digits;
            chkSpecial.IsChecked = special;
            chkExcludeSimilar.IsChecked = excludeSimilar;
            chkExcludeAmbiguous.IsChecked = excludeAmbiguous;
            chkNoConsecutive.IsChecked = noConsecutive;

            UpdatePreview();
        }

        private void sldLength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtLengthValue != null) // Проверка на null
            {
                txtLengthValue.Text = ((int)sldLength.Value).ToString();
            }
            UpdatePreview();
        }

        private void UpdatePreview(object sender = null, RoutedEventArgs e = null)
        {
            try
            {
                // Проверяем, что все элементы управления существуют
                if (txtPreview == null || txtStrength == null || txtEntropy == null || StrengthBar == null)
                    return;

                string password = GeneratePassword(updateFields: false);
                txtPreview.Text = password;

                // Оцениваем сложность
                var (strength, entropy) = EvaluatePasswordStrength(password);

                PasswordLength = (int)sldLength.Value;
                IncludeUppercase = chkUppercase.IsChecked ?? true;
                IncludeLowercase = chkLowercase.IsChecked ?? true;
                IncludeDigits = chkDigits.IsChecked ?? true;
                IncludeSpecial = chkSpecial.IsChecked ?? true;
                ExcludeSimilar = chkExcludeSimilar.IsChecked ?? false;
                ExcludeAmbiguous = chkExcludeAmbiguous.IsChecked ?? false;
                NoConsecutive = chkNoConsecutive.IsChecked ?? false;

                // Обновляем индикаторы
                txtStrength.Text = $"Сложность: {strength}";
                txtEntropy.Text = $"Энтропия: {entropy} бит";

                // Меняем цвет индикатора
                UpdateStrengthBar(entropy);
            }
            catch (Exception ex)
            {
                if (txtPreview != null)
                    txtPreview.Text = "Ошибка генерации";
            }
        }

        private void UpdateStrengthBar(int entropy)
        {
            if (StrengthBar == null || txtStrength == null)
                return;

            StrengthBar.Value = Math.Min(100, entropy);

            if (entropy < 30)
            {
                StrengthBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                txtStrength.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            }
            else if (entropy < 50)
            {
                StrengthBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
                txtStrength.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
            }
            else if (entropy < 70)
            {
                StrengthBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                txtStrength.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
            }
            else
            {
                StrengthBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
                txtStrength.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
            }
        }

        private (string, int) EvaluatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return ("Очень слабый", 0);

            int entropy = 0;
            string strength;

            // Длина пароля
            if (password.Length >= 16) entropy += 40;
            else if (password.Length >= 12) entropy += 30;
            else if (password.Length >= 8) entropy += 20;
            else entropy += 10;

            // Набор символов
            if (password.Any(char.IsLower)) entropy += 15;
            if (password.Any(char.IsUpper)) entropy += 15;
            if (password.Any(char.IsDigit)) entropy += 15;
            if (password.Any(c => !char.IsLetterOrDigit(c))) entropy += 15;

            // Уникальность
            double uniqueRatio = (double)password.Distinct().Count() / password.Length;
            entropy += (int)(uniqueRatio * 10);

            // Определяем текстовую оценку
            if (entropy >= 80) strength = "Отличный";
            else if (entropy >= 60) strength = "Хороший";
            else if (entropy >= 40) strength = "Средний";
            else if (entropy >= 20) strength = "Слабый";
            else strength = "Очень слабый";

            return (strength, Math.Min(100, entropy));
        }

        private string GeneratePassword(bool updateFields = true)
        {
            int length = (int)sldLength.Value;
            bool includeUpper = chkUppercase.IsChecked ?? true;
            bool includeLower = chkLowercase.IsChecked ?? true;
            bool includeDigits = chkDigits.IsChecked ?? true;
            bool includeSpecial = chkSpecial.IsChecked ?? true;
            bool excludeSimilar = chkExcludeSimilar.IsChecked ?? false;
            bool excludeAmbiguous = chkExcludeAmbiguous.IsChecked ?? false;
            bool noConsecutive = chkNoConsecutive.IsChecked ?? false;

            // Базовые наборы символов
            string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
            string digitChars = "0123456789";
            string specialChars = "!@#$%^&*()_-+=<>?";

            // Исключаем похожие символы если нужно
            if (excludeSimilar)
            {
                uppercaseChars = uppercaseChars.Replace("I", "").Replace("L", "").Replace("O", "");
                lowercaseChars = lowercaseChars.Replace("i", "").Replace("l", "").Replace("o", "");
                digitChars = digitChars.Replace("1", "").Replace("0", "");
            }

            // Исключаем неоднозначные символы если нужно
            if (excludeAmbiguous)
            {
                specialChars = specialChars.Replace("{", "").Replace("}", "")
                                          .Replace("[", "").Replace("]", "")
                                          .Replace("(", "").Replace(")", "")
                                          .Replace("/", "").Replace("\\", "");
            }

            // Собираем разрешенные символы
            StringBuilder allowedChars = new StringBuilder();
            StringBuilder password = new StringBuilder();

            if (includeUpper && !string.IsNullOrEmpty(uppercaseChars))
            {
                allowedChars.Append(uppercaseChars);
                password.Append(uppercaseChars[_random.Next(uppercaseChars.Length)]);
            }

            if (includeLower && !string.IsNullOrEmpty(lowercaseChars))
            {
                allowedChars.Append(lowercaseChars);
                password.Append(lowercaseChars[_random.Next(lowercaseChars.Length)]);
            }

            if (includeDigits && !string.IsNullOrEmpty(digitChars))
            {
                allowedChars.Append(digitChars);
                password.Append(digitChars[_random.Next(digitChars.Length)]);
            }

            if (includeSpecial && !string.IsNullOrEmpty(specialChars))
            {
                allowedChars.Append(specialChars);
                password.Append(specialChars[_random.Next(specialChars.Length)]);
            }

            // Если не выбрано ни одного типа символов, используем все
            if (allowedChars.Length == 0)
            {
                allowedChars.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
            }

            // Заполняем оставшуюся длину
            string allowedString = allowedChars.ToString();
            for (int i = password.Length; i < length; i++)
            {
                char nextChar;
                do
                {
                    nextChar = allowedString[_random.Next(allowedString.Length)];
                } while (noConsecutive && i > 0 && password[i - 1] == nextChar);

                password.Append(nextChar);
            }

            // Перемешиваем пароль
            string result = ShuffleString(password.ToString());

            if (updateFields)
            {
                GeneratedPassword = result;
            }

            return result;
        }

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

        private void GenerateNewPreview_Click(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
        }

        private void ApplySettings_Click(object sender, RoutedEventArgs e)
        {
            GeneratedPassword = txtPreview.Text;
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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                BtnCancel_Click(sender, e);
            }
            else if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ApplySettings_Click(sender, e);
            }
        }
    }
}
