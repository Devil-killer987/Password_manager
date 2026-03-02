using Microsoft.EntityFrameworkCore;
using Password_manager;
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


namespace Manager_password
{

    /// <summary>
    /// Логика взаимодействия для addProf.xaml
    /// </summary>
    public partial class addProf : Window
    {

        public addProf()
        {
            InitializeComponent();
            
        }
      

        private void back_Click(object sender, RoutedEventArgs e)
        {
            Window window = new MainWindow();
            window.Show();
            this.Close();
        }

        private void CreateProf_Click(object sender, RoutedEventArgs e)
        {
            

            
            using (var dbContext = new AppDbContext())
            {
                dbContext.Database.EnsureCreated();

                var nam = name.Text;
            var ps = pass.Text;
            var newUser = new User { Name = nam, Password = ps };
            dbContext.Add(newUser);
                dbContext.SaveChanges();
                MessageBox.Show("профиль добавлен");
        }
    }
       
    }
   
   
}
