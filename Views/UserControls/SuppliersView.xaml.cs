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
    public partial class SuppliersView : UserControl
    {
        private AptekaISDbEntities db;

        public SuppliersView()
        {
            InitializeComponent();
            db = new AptekaISDbEntities();
            LoadSuppliers();
        }

        private void LoadSuppliers(string searchText = "")
        {
            var query = db.Suppliers.AsQueryable();

            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(s => s.Name.Contains(searchText));
            }

            SuppliersGrid.ItemsSource = query.ToList();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadSuppliers(SearchTextBox.Text);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddEditSupplierWindow();
            if (addWindow.ShowDialog() == true)
            {
                db = new AptekaISDbEntities();
                LoadSuppliers();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SuppliersGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите поставщика", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedRow = (dynamic)SuppliersGrid.SelectedItem;
            int supplierId = selectedRow.Id;

            using (var context = new AptekaISDbEntities())
            {
                var supplier = context.Suppliers.FirstOrDefault(s => s.Id == supplierId);
                if (supplier == null) return;

                var editWindow = new AddEditSupplierWindow(supplier);
                editWindow.Owner = Window.GetWindow(this);

                if (editWindow.ShowDialog() == true)
                {
                    db = new AptekaISDbEntities();
                    LoadSuppliers();
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SuppliersGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите поставщика для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selected = SuppliersGrid.SelectedItem;
            var property = selected.GetType().GetProperty("Id");
            if (property == null) return;
            int supplierId = (int)property.GetValue(selected);

            var result = MessageBox.Show("Удалить поставщика?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                using (var context = new AptekaISDbEntities())
                {
                    // Загружаем поставщика с его производителями
                    var supplier = context.Suppliers
                        .Include("Manufacturers")
                        .FirstOrDefault(s => s.Id == supplierId);

                    if (supplier != null)
                    {
                        // Очищаем связи с производителями
                        supplier.Manufacturers.Clear();

                        // Удаляем поставщика
                        context.Suppliers.Remove(supplier);

                        context.SaveChanges();
                    }
                }

                db = new AptekaISDbEntities();
                LoadSuppliers();
            }
        }
    }
}
