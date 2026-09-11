using AptekaIS.Models;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.IO;

namespace AptekaIS.Views
{
    public partial class AddEditProductWindow : Window
    {
        private AptekaISDbEntities db;
        private Products editingProduct;
        private ObservableCollection<SpecialPropertyCheck> specialProperties;
        private byte[] selectedImageData;

        public AddEditProductWindow()
        {
            InitializeComponent();
            db = new AptekaISDbEntities();
            LoadData();
            LoadSpecialProperties();
        }

        public AddEditProductWindow(int productId) : this()
        {
            editingProduct = db.Products.FirstOrDefault(p => p.Id == productId);

            if (editingProduct != null)
                LoadProductData();
        }

        private void LoadData()
        {
            ManufacturerComboBox.ItemsSource = db.Manufacturers.ToList();
            FormComboBox.ItemsSource = db.ProductForms.ToList();
            PackageComboBox.ItemsSource = db.PackageTypes.ToList();
            MainCategoryComboBox.ItemsSource = db.MainCategories.ToList();
        }

        private void LoadSpecialProperties()
        {
            var allProperties = db.SpecialProperties.ToList();
            specialProperties = new ObservableCollection<SpecialPropertyCheck>();

            foreach (var prop in allProperties)
            {
                specialProperties.Add(new SpecialPropertyCheck
                {
                    Id = prop.Id,
                    Name = prop.Name,
                    IsSelected = false
                });
            }

            SpecialPropertiesItemsControl.ItemsSource = specialProperties;
        }

        private void LoadProductData()
        {
            NameTextBox.Text = editingProduct.Name;
            PriceTextBox.Text = editingProduct.Price.ToString();
            QuantityTextBox.Text = editingProduct.Quantity.ToString();
            DescriptionTextBox.Text = editingProduct.Description;

            ManufacturerComboBox.SelectedValue = editingProduct.ManufacturerId;
            FormComboBox.SelectedValue = editingProduct.FormId;
            PackageComboBox.SelectedValue = editingProduct.PackageTypeId;

            ExpiryDatePicker.SelectedDate = editingProduct.ExpiryDate;
            PrescriptionCheckBox.IsChecked = editingProduct.PrescriptionRequired;

            DosageTextBox.Text = editingProduct.Dosage;
            VolumeTextBox.Text = editingProduct.Volume;
            SubstanceTextBox.Text = editingProduct.Substance;

            // Загружаем выбранные специальные свойства
            var selectedIds = editingProduct.SpecialProperties.Select(sp => sp.Id).ToHashSet();

            foreach (var item in specialProperties)
            {
                item.IsSelected = selectedIds.Contains(item.Id);
            }

            // Загружаем информацию о картинке
            var mainImage = editingProduct.ProductImages.FirstOrDefault(i => i.IsMain == true);
            if (mainImage != null)
            {
                selectedImageData = mainImage.ImageData;
                ImageFileNameTextBox.Text = $"{editingProduct.Name}_{mainImage.Id}.img";
            }

            if (editingProduct.SubCategoryId != null)
            {
                var sub = db.SubCategories
                    .FirstOrDefault(s => s.Id == editingProduct.SubCategoryId);

                if (sub != null)
                {
                    MainCategoryComboBox.SelectedValue = sub.MainCategoryId;

                    SubCategoryComboBox.ItemsSource = db.SubCategories
                        .Where(x => x.MainCategoryId == sub.MainCategoryId)
                        .ToList();

                    SubCategoryComboBox.SelectedValue = sub.Id;
                }
            }
        }

        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
            dialog.Title = "Выберите изображение для товара";

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    selectedImageData = File.ReadAllBytes(dialog.FileName);
                    ImageFileNameTextBox.Text = Path.GetFileName(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке изображения: " + ex.Message);
                }
            }
        }

        private void MainCategoryComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (MainCategoryComboBox.SelectedValue == null)
                return;

            int id = (int)MainCategoryComboBox.SelectedValue;

            SubCategoryComboBox.ItemsSource = db.SubCategories
                .Where(x => x.MainCategoryId == id)
                .ToList();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название");
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price))
            {
                MessageBox.Show("Цена неверная");
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity))
            {
                MessageBox.Show("Количество неверное");
                return;
            }

            Products product = editingProduct ?? new Products();

            if (editingProduct == null)
                db.Products.Add(product);

            product.Name = NameTextBox.Text.Trim();
            product.Price = price;
            product.Quantity = quantity;

            product.ManufacturerId = (int?)ManufacturerComboBox.SelectedValue;
            product.FormId = (int?)FormComboBox.SelectedValue;
            product.PackageTypeId = (int?)PackageComboBox.SelectedValue;
            product.SubCategoryId = (int?)SubCategoryComboBox.SelectedValue;

            product.Dosage = DosageTextBox.Text.Trim();
            product.Volume = VolumeTextBox.Text.Trim();
            product.Substance = SubstanceTextBox.Text.Trim();

            product.Description = DescriptionTextBox.Text.Trim();
            product.ExpiryDate = ExpiryDatePicker.SelectedDate;
            product.PrescriptionRequired = PrescriptionCheckBox.IsChecked == true;

            db.SaveChanges();

            // Обновляем специальные свойства (многие-ко-многим)
            product.SpecialProperties.Clear();
            foreach (var item in specialProperties.Where(x => x.IsSelected))
            {
                var property = db.SpecialProperties.Find(item.Id);
                if (property != null)
                    product.SpecialProperties.Add(property);
            }

            // Обновляем картинку
            var existingMainImage = product.ProductImages.FirstOrDefault(i => i.IsMain == true);

            if (selectedImageData != null)
            {
                if (existingMainImage != null)
                {
                    existingMainImage.ImageData = selectedImageData;
                    existingMainImage.ContentType = "image/jpeg";
                }
                else
                {
                    product.ProductImages.Add(new ProductImages
                    {
                        ImageData = selectedImageData,
                        ContentType = "image/jpeg",
                        IsMain = true,
                        UploadedAt = DateTime.Now
                    });
                }
            }

            db.SaveChanges();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class SpecialPropertyCheck
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }
}