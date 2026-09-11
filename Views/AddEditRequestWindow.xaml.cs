using AptekaIS.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AptekaIS.Views
{
    public partial class AddEditRequestWindow : Window
    {
        private AptekaISDbEntities db;
        private ObservableCollection<ProductRequestItem> products;

        public AddEditRequestWindow()
        {
            InitializeComponent();
            db = new AptekaISDbEntities();
            LoadSuppliers();
        }

        private void LoadSuppliers()
        {
            SupplierComboBox.ItemsSource = db.Suppliers.ToList();
        }

        private void LoadProducts(int supplierId, string searchText = "")
        {
            var supplier = db.Suppliers.Include("Manufacturers").FirstOrDefault(s => s.Id == supplierId);
            if (supplier == null) return;

            var manufacturerIds = supplier.Manufacturers.Select(m => m.Id).ToList();

            var query = db.Products
                .Where(p => p.ManufacturerId != null && manufacturerIds.Contains(p.ManufacturerId.Value))
                .Select(p => new ProductRequestItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    Quantity = p.Quantity,
                    ManufacturerName = p.Manufacturers.Name,
                    Dosage = p.Dosage,
                    Volume = p.Volume,
                    IsSelected = false,
                    RequestedQuantity = 0
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(p => p.Name.ToLower().Contains(searchText.ToLower())).ToList();
            }

            products = new ObservableCollection<ProductRequestItem>(
                query.OrderByDescending(p => p.Quantity <= 5)
                     .ThenBy(p => p.Name)
            );

            ProductsItemsControl.ItemsSource = products;
        }

        private void SupplierComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SupplierComboBox.SelectedValue == null) return;
            int supplierId = (int)SupplierComboBox.SelectedValue;
            LoadProducts(supplierId, SearchTextBox.Text);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SupplierComboBox.SelectedValue == null) return;
            int supplierId = (int)SupplierComboBox.SelectedValue;
            LoadProducts(supplierId, SearchTextBox.Text);
        }

        private void CreateRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (SupplierComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите поставщика", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedProducts = products.Where(p => p.IsSelected && p.RequestedQuantity > 0).ToList();
            if (!selectedProducts.Any())
            {
                MessageBox.Show("Выберите хотя бы один товар и укажите количество", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int supplierId = (int)SupplierComboBox.SelectedValue;
                int? currentUserId = App.currentUser?.Id;

                var request = new Requests
                {
                    RequestDate = DateTime.Now,
                    SupplierId = supplierId,
                    CreatedAt = DateTime.Now,
                    CreatedByUserId = currentUserId
                };
                db.Requests.Add(request);
                db.SaveChanges();

                // Автоматически создаём приёмку со статусом "В обработке"
                var receipt = new Receipts
                {
                    ReceiptDate = DateTime.Now,
                    SupplierId = supplierId,
                    RequestId = request.Id,
                    Status = "В обработке"
                };
                db.Receipts.Add(receipt);
                db.SaveChanges();

                foreach (var item in selectedProducts)
                {
                    db.RequestItems.Add(new RequestItems
                    {
                        RequestId = request.Id,
                        ProductId = item.Id,
                        RequestedQuantity = item.RequestedQuantity
                    });
                }

                db.SaveChanges();
                MessageBox.Show($"Заявка №{request.Id} успешно создана", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class ProductRequestItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public string ManufacturerName { get; set; }
        public string Dosage { get; set; }
        public string Volume { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                if (!value) RequestedQuantity = 0;
            }
        }

        private int _requestedQuantity;
        public int RequestedQuantity
        {
            get => _requestedQuantity;
            set
            {
                _requestedQuantity = value;
                OnPropertyChanged(nameof(RequestedQuantity));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}