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
    public partial class WriteOffView : UserControl
    {
        private AptekaISDbEntities db;

        public WriteOffView()
        {
            InitializeComponent();
            db = new AptekaISDbEntities();
            LoadWriteOffs();
            SetPermissions();
        }
        private void SetPermissions()
        {
            string roleName = App.currentUser?.Roles?.Name;

            // Заведующий - видит всё
            if (roleName == "Заведующий")
            {
                ShowAllButtons();
                return;
            }

            // Фармацевт - скрываем кнопки Редактировать и Удалить
            if (roleName == "Фармацевт")
            {
                EditWriteOffButton.Visibility = Visibility.Collapsed;
                DeleteWriteOffButton.Visibility = Visibility.Collapsed;

                return;
            }

        }

        private void ShowAllButtons()
        {
            NewWriteOffButton.Visibility = Visibility.Visible;
            EditWriteOffButton.Visibility = Visibility.Visible;
            DeleteWriteOffButton.Visibility = Visibility.Visible;
        }
        private void LoadWriteOffs()
        {
            var query = db.WriteOffs
                .Include("Products")
                .Include("Users")
                .Select(w => new
                {
                    w.Id,
                    w.WriteOffDate,
                    ProductName = w.Products.Name,
                    w.Quantity,
                    w.Reason,
                    UserName = w.Users.FullName
                })
                .OrderByDescending(w => w.WriteOffDate)
                .ToList();

            WriteOffsGrid.ItemsSource = query;
        }

        private void NewWriteOffButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddEditWriteOffWindow();
            if (window.ShowDialog() == true)
            {
                LoadWriteOffs();

                if (Window.GetWindow(this) is MainWindow main)
                {
                    main.ProductsViewControl?.RefreshData();
                }
            }
        }

        private void EditWriteOff_Click(object sender, RoutedEventArgs e)
        {
            if (WriteOffsGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для редактирования.");
                return;
            }

            dynamic selected = WriteOffsGrid.SelectedItem;
            int writeOffId = selected.Id;

            var window = new AddEditWriteOffWindow(writeOffId);

            if (window.ShowDialog() == true)
            {
                LoadWriteOffs();

                if (Window.GetWindow(this) is MainWindow main)
                {
                    main.ProductsViewControl?.RefreshData();
                }
            }
        }

        private void DeleteWriteOff_Click(object sender, RoutedEventArgs e)
        {
            if (WriteOffsGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления.");
                return;
            }

            dynamic selected = WriteOffsGrid.SelectedItem;
            int writeOffId = selected.Id;

            var result = MessageBox.Show(
                "Удалить запись о списании?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            var writeOff = db.WriteOffs.FirstOrDefault(w => w.Id == writeOffId);

            if (writeOff == null)
                return;

            // Возвращаем товар на склад
            var product = db.Products.First(p => p.Id == writeOff.ProductId);
            product.Quantity += writeOff.Quantity;

            db.WriteOffs.Remove(writeOff);
            db.SaveChanges();
            if (Window.GetWindow(this) is MainWindow main)
            {
                main.ProductsViewControl?.RefreshData();
            }

            LoadWriteOffs();

            MessageBox.Show(
                "Запись удалена, остаток восстановлен.",
                "Готово",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
