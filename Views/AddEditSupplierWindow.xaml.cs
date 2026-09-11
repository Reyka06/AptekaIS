using AptekaIS.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace AptekaIS.Views
{
    public partial class AddEditSupplierWindow : Window
    {
        private AptekaISDbEntities db;
        private Suppliers _editingSupplier;
        private ObservableCollection<ManufacturerCheckItem> manufacturers;

        public AddEditSupplierWindow()
        {
            InitializeComponent();
            db = new AptekaISDbEntities();
            LoadManufacturers();
        }

        public AddEditSupplierWindow(Suppliers supplier) : this()
        {
            _editingSupplier = db.Suppliers.FirstOrDefault(s => s.Id == supplier.Id);
            LoadSupplierData();
        }

        private void LoadManufacturers()
        {
            var allManufacturers = db.Manufacturers.ToList();
            manufacturers = new ObservableCollection<ManufacturerCheckItem>();

            foreach (var m in allManufacturers)
            {
                manufacturers.Add(new ManufacturerCheckItem
                {
                    Id = m.Id,
                    Name = m.Name,
                    IsSelected = false
                });
            }

            ManufacturersList.ItemsSource = manufacturers;
        }

        private void LoadSupplierData()
        {
            if (_editingSupplier == null) return;

            NameTextBox.Text = _editingSupplier.Name;
            ContactInfoTextBox.Text = _editingSupplier.ContactInfo;

            // Загружаем выбранных производителей через навигационное свойство
            var selectedIds = _editingSupplier.Manufacturers.Select(m => m.Id).ToHashSet();

            foreach (var m in manufacturers)
            {
                m.IsSelected = selectedIds.Contains(m.Id);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            string contactInfo = ContactInfoTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите название поставщика", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_editingSupplier == null)
                {
                    _editingSupplier = new Suppliers
                    {
                        Name = name,
                        ContactInfo = contactInfo
                    };
                    db.Suppliers.Add(_editingSupplier);
                    db.SaveChanges();
                }
                else
                {
                    _editingSupplier.Name = name;
                    _editingSupplier.ContactInfo = contactInfo;
                }

                // Очищаем текущих производителей
                _editingSupplier.Manufacturers.Clear();

                // Добавляем выбранных
                foreach (var m in manufacturers.Where(m => m.IsSelected))
                {
                    var manufacturer = db.Manufacturers.Find(m.Id);
                    if (manufacturer != null)
                        _editingSupplier.Manufacturers.Add(manufacturer);
                }

                db.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class ManufacturerCheckItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }
}