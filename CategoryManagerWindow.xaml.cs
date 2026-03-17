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
    /// Логика взаимодействия для CategoryManagerWindow.xaml
    /// </summary>
    public partial class CategoryManagerWindow : Window
    {
        private int _userId;
        private byte[] _masterKey;
        private List<Category> _categories;

        /// <summary>
        /// Конструктор с параметрами (вызывается из MainWindow)
        /// </summary>
        public CategoryManagerWindow(int userId, byte[] masterKey)
        {
            InitializeComponent();
            _userId = userId;
            _masterKey = masterKey;

            LoadCategories();
        }

        /// <summary>
        /// Загрузка категорий пользователя
        /// </summary>
        private void LoadCategories()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    _categories = db.Categories
                        .Where(c => c.UserId == _userId)
                        .OrderBy(c => c.DisplayOrder)
                        .ToList();

                    CategoriesListView.ItemsSource = _categories;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Добавление новой категории
        /// </summary>
        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            // Простое окно для ввода названия категории
            string categoryName = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите название новой категории:",
                "Новая категория",
                "",
                -1, -1);

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        // Проверяем, есть ли уже такая категория
                        if (db.Categories.Any(c => c.UserId == _userId && c.Name == categoryName))
                        {
                            MessageBox.Show("Категория с таким названием уже существует",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var newCategory = new Category
                        {
                            UserId = _userId,
                            Name = categoryName,
                            Icon = "📁", // Иконка по умолчанию
                            Color = "#3498DB", // Синий цвет по умолчанию
                            DisplayOrder = db.Categories.Count(c => c.UserId == _userId) + 1,
                            CreatedAt = DateTime.Now,
                            IsDefault = false
                        };

                        db.Categories.Add(newCategory);
                        db.SaveChanges();

                        LoadCategories(); // Перезагружаем список
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании категории: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Редактирование категории
        /// </summary>
        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int categoryId)
            {
                var category = _categories.FirstOrDefault(c => c.Id == categoryId);
                if (category != null)
                {
                    string newName = Microsoft.VisualBasic.Interaction.InputBox(
                        "Введите новое название категории:",
                        "Редактирование категории",
                        category.Name,
                        -1, -1);

                    if (!string.IsNullOrWhiteSpace(newName) && newName != category.Name)
                    {
                        try
                        {
                            using (var db = new AppDbContext())
                            {
                                // Проверяем, нет ли другой категории с таким именем
                                if (db.Categories.Any(c => c.UserId == _userId && c.Name == newName && c.Id != categoryId))
                                {
                                    MessageBox.Show("Категория с таким названием уже существует",
                                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }

                                var catToUpdate = db.Categories.Find(categoryId);
                                if (catToUpdate != null)
                                {
                                    catToUpdate.Name = newName;
                                    db.SaveChanges();

                                    LoadCategories(); // Перезагружаем список
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при обновлении категории: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Удаление категории
        /// </summary>
        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int categoryId)
            {
                var category = _categories.FirstOrDefault(c => c.Id == categoryId);
                if (category != null && !category.IsDefault)
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить категорию '{category.Name}'?\n\n" +
                        "Пароли в этой категории будут перемещены в 'Без категории'.",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            using (var db = new AppDbContext())
                            {
                                var catToDelete = db.Categories.Find(categoryId);
                                if (catToDelete != null)
                                {
                                    db.Categories.Remove(catToDelete);
                                    db.SaveChanges();

                                    LoadCategories(); // Перезагружаем список
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при удалении категории: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
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
