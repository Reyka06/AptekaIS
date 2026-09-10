using AptekaIS.Models;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AptekaIS.Views
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
                App.DBApteka = new Models.AptekaISDbEntities();
                //MessageBox.Show("Ok");
            }
            catch (Exception)
            {
                //MessageBox.Show("Not");
            }
        }

        private void BtnInput_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;

            if (login == "")
            {
                MessageBox.Show("Вы не ввели логин", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Вы не ввели пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            App.currentUser = App.DBApteka.Users
                .FirstOrDefault(u => u.Login == login && u.Password == password);

            if (App.currentUser != null)
            {
                this.Hide();
                MainWindow mainWindow = new MainWindow();
                mainWindow.ShowDialog();
                this.Show();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
         }
        }
    }
}
