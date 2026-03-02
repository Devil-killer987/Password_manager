using Microsoft.EntityFrameworkCore;
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
        public MainWindow()
        {
            InitializeComponent();
            loadcomb();
        }

        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            Window window = new addProf();
            window.Show();
            this.Close();
        }
        private void loadcomb()
        {
            using (var db = new AppDbContext())
            {
                try
                {
                    db.Database.EnsureCreated();

                    // Загружаем всех пользователей
                    var users = db.Users
                        .OrderBy(u => u.Name)
                        .ToList();

                    // Привязываем к ComboBox
                    nameProfile.ItemsSource = users;
                    nameProfile.DisplayMemberPath = "Name"; // Отображаем имя
                    nameProfile.SelectedValuePath = "Id";
                }
                catch { }
                          
                }
            }
        }
    }

