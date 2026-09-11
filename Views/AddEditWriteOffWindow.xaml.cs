using AptekaIS.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AptekaIS.Views
{
    public partial class AddEditWriteOffWindow : Window
    {
        private AptekaISDbEntities db;
        private WriteOffs _editingWriteOff;

        public AddEditWriteOffWindow()
        {
            InitializeComponent();
            db = new AptekaISDbEntities();
            LoadProducts();
        }

        public AddEditWriteOffWindow(int writeOffId) : this()
        {
            _editingWriteOff = db.WriteOffs.FirstOrDefault(w => w.Id == writeOffId);
            if (_editingWriteOff != null)
                LoadWriteOffData();
        }

        private void LoadProducts()
        {
            ProductComboBox.ItemsSource = db.Products.ToList();
        }

        private void LoadWriteOffData()
        {
            ProductComboBox.IsEnabled = false;
            ProductComboBox.SelectedValue = _editingWriteOff.ProductId;
            QuantityTextBox.Text = _editingWriteOff.Quantity.ToString();
            var reasonItem = ReasonComboBox.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == _editingWriteOff.Reason);
            if (reasonItem != null)
                ReasonComboBox.SelectedItem = reasonItem;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите товар", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ReasonComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите причину списания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string reason = (ReasonComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            try
            {
                int productId = (int)ProductComboBox.SelectedValue;
                var product = db.Products.First(p => p.Id == productId);

                if (_editingWriteOff == null)
                {
                    // Новое списание
                    if (product.Quantity < quantity)
                    {
                        MessageBox.Show($"Недостаточно товара. Доступно: {product.Quantity}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    product.Quantity -= quantity;

                    db.WriteOffs.Add(new WriteOffs
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        Reason = reason,
                        WriteOffDate = DateTime.Now,
                        UserId = App.currentUser?.Id ?? 1
                    });
                }
                else
                {
                    // Редактирование
                    int oldQuantity = _editingWriteOff.Quantity;
                    int quantityDiff = quantity - oldQuantity;

                    if (quantityDiff > 0 && product.Quantity < quantityDiff)
                    {
                        MessageBox.Show($"Недостаточно товара для увеличения списания. Доступно: {product.Quantity}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    product.Quantity -= quantityDiff;

                    _editingWriteOff.Quantity = quantity;
                    _editingWriteOff.Reason = reason;
                }

                db.SaveChanges();
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
}
