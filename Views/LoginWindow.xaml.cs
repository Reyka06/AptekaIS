using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AptekaIS.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            try
            {
                App.DBApteka = new Models.AptekaISDbEntities();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось подключиться к базе данных:\n" + ex.Message,"Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);
                Application.Current.Shutdown();
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
