using AptekaIS.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace AptekaIS.Views
{
    public partial class AcceptRequestWindow : Window
    {
        private AptekaISDbEntities db;
        private int requestId;
        private ObservableCollection<ReceiptItemTemp> items;

        public AcceptRequestWindow(int requestId)
        {
            InitializeComponent();

            this.requestId = requestId;
            db = new AptekaISDbEntities();

            LoadRequestData();
        }

        private void LoadRequestData()
        {
            var request = db.Requests
                .Include("Suppliers")
                .FirstOrDefault(r => r.Id == requestId);

            if (request == null)
                return;

            RequestNumberText.Text = request.Id.ToString();
            SupplierNameText.Text = request.Suppliers?.Name ?? "—";

            var requestItems = db.RequestItems
                .Include("Products")
                .Where(r => r.RequestId == requestId)
                .ToList();

            items = new ObservableCollection<ReceiptItemTemp>();

            foreach (var ri in requestItems)
            {
                int alreadyAccepted = db.ReceiptItems
                    .Join(
                        db.Receipts.Where(r => r.RequestId == requestId),
                        item => item.ReceiptId,
                        receipt => receipt.Id,
                        (item, receipt) => item)
                    .Where(x => x.ProductId == ri.ProductId)
                    .Sum(x => (int?)x.Quantity) ?? 0;

                items.Add(new ReceiptItemTemp
                {
                    ProductId = ri.ProductId,
                    ProductName = ri.Products.Name,
                    RequestedQuantity = ri.RequestedQuantity,

                    // сколько уже приняли
                    AlreadyAccepted = alreadyAccepted,

                    // сколько осталось довезти
                    ActualQuantity = ri.RequestedQuantity - alreadyAccepted
                });
            }

            ItemsControlProducts.ItemsSource = items;
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var receipt = db.Receipts
                    .FirstOrDefault(r => r.RequestId == requestId);

                if (receipt == null)
                {
                    MessageBox.Show("Приход не найден.");
                    return;
                }

                bool allReceived = true;
                bool anyReceived = false;

                foreach (var item in items)
                {
                    if (item.ActualQuantity < 0)
                    {
                        MessageBox.Show(
                            $"Количество для товара \"{item.ProductName}\" не может быть отрицательным.");

                        return;
                    }

                    if (item.ActualQuantity > 0)
                    {
                        anyReceived = true;

                        db.ReceiptItems.Add(new ReceiptItems
                        {
                            ReceiptId = receipt.Id,
                            ProductId = item.ProductId,
                            Quantity = item.ActualQuantity,
                            PurchasePrice = 0
                        });

                        var product = db.Products
                            .FirstOrDefault(p => p.Id == item.ProductId);

                        if (product != null)
                        {
                            product.Quantity += item.ActualQuantity;
                        }
                    }

                    int totalAccepted =
                        item.AlreadyAccepted +
                        item.ActualQuantity;

                    if (totalAccepted < item.RequestedQuantity)
                    {
                        allReceived = false;
                    }
                }

                if (!anyReceived)
                {
                    receipt.Status = "В обработке";
                }
                else if (allReceived)
                {
                    receipt.Status = "Проведена";
                }
                else
                {
                    receipt.Status = "Частично";
                }

                receipt.ReceiptDate = DateTime.Now;

                db.SaveChanges();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class ReceiptItemTemp : INotifyPropertyChanged
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int RequestedQuantity { get; set; }

        public int AlreadyAccepted { get; set; }

        private int _actualQuantity;

        public int ActualQuantity
        {
            get => _actualQuantity;
            set
            {
                if (_actualQuantity != value)
                {
                    _actualQuantity = value;
                    OnPropertyChanged(nameof(ActualQuantity));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}