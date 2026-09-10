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

namespace AptekaIS.Views.UserControls
{
    public partial class UsersView : UserControl
    {
        private AptekaISDbEntities db;

        public UsersView()
        {
            InitializeComponent();
            db = new AptekaISDbEntities();
            LoadUsers();
        }

        private void LoadUsers()
        {
            var query = from u in db.Users
                        join r in db.Roles on u.RoleId equals r.Id
                        select new
                        {
                            u.Id,
                            u.Login,
                            u.FullName,
                            RoleName = r.Name
                        };

            UsersGrid.ItemsSource = query.ToList();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddEditUserWindow();
            if (addWindow.ShowDialog() == true)
            {
                db = new AptekaISDbEntities(); // обновляем контекст
                LoadUsers();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя");
                return;
            }

            // Берём Id напрямую из выделенной строки
            var selectedRow = (dynamic)UsersGrid.SelectedItem;
            int userId = selectedRow.Id;
            using (var context = new AptekaISDbEntities())
            {
                var user = context.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null) return;

                var editWindow = new AddEditUserWindow(user);
                editWindow.Owner = Window.GetWindow(this);

                if (editWindow.ShowDialog() == true)
                {
                    // Обновляем локальный контекст
                    db = new AptekaISDbEntities();
                    LoadUsers();
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selected = UsersGrid.SelectedItem;
            var property = selected.GetType().GetProperty("Id");
            if (property == null) return;
            int userId = (int)property.GetValue(selected);

            var result = MessageBox.Show("Удалить пользователя?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                using (var context = new AptekaISDbEntities())
                {
                    var user = context.Users.FirstOrDefault(u => u.Id == userId);
                    if (user != null)
                    {
                        context.Users.Remove(user);
                        context.SaveChanges();
                    }
                }
                db = new AptekaISDbEntities();
                LoadUsers();
            }
        }
    }
}