using AptekaIS.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace AptekaIS.Views.UserControls
{
    public partial class CashierView : UserControl
    {
        private AptekaISDbEntities db;
        private ObservableCollection<CartItem> cart;

        public CashierView()
        {
            InitializeComponent();
            db = new AptekaISDbEntities();
            cart = new ObservableCollection<CartItem>();
            CartItemsControl.ItemsSource = cart;
            LoadProducts();
            LoadFilters();
        }
        public void RefreshData()
        {
            db.Dispose();
            db = new AptekaISDbEntities();

            cart.Clear();
            UpdateTotal();

            LoadProducts();
        }

        private void LoadFilters()
        {
            var categories = db.MainCategories.ToList();
            categories.Insert(0, new MainCategories { Id = 0, Name = "Все категории" });
            CategoryFilterComboBox.ItemsSource = categories;
            CategoryFilterComboBox.SelectedIndex = 0; // <-- Добавлено

            SubCategoryFilterComboBox.ItemsSource = new[] { new SubCategories { Id = 0, Name = "Все подкатегории" } };
            SubCategoryFilterComboBox.SelectedIndex = 0; // <-- Добавлено
        }

        private void LoadProducts()
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var query = db.Products
                .Include("Manufacturers")
                .Include("SubCategories")
                .Include("ProductImages")
                .AsQueryable();

            string search = SearchTextBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchLower = search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchLower) ||
                    (p.Manufacturers != null &&
                     p.Manufacturers.Name.ToLower().Contains(searchLower))
                );
            }

            if (CategoryFilterComboBox.SelectedValue != null &&
                (int)CategoryFilterComboBox.SelectedValue != 0)
            {
                int catId = (int)CategoryFilterComboBox.SelectedValue;
                query = query.Where(p => p.SubCategories.MainCategoryId == catId);
            }

            if (SubCategoryFilterComboBox.SelectedValue != null &&
                (int)SubCategoryFilterComboBox.SelectedValue != 0)
            {
                int subId = (int)SubCategoryFilterComboBox.SelectedValue;
                query = query.Where(p => p.SubCategoryId == subId);
            }

            if (decimal.TryParse(PriceFromTextBox.Text, out decimal priceFrom))
                query = query.Where(p => p.Price >= priceFrom);

            if (decimal.TryParse(PriceToTextBox.Text, out decimal priceTo))
                query = query.Where(p => p.Price <= priceTo);

            var result = query
                .ToList()
                .Select(p => new ProductDisplayItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    Dosage = p.Dosage,
                    Volume = p.Volume,
                    PrescriptionRequired = p.PrescriptionRequired,
                    Manufacturers = p.Manufacturers,
                    SubCategories = p.SubCategories,
                    MainImage = p.ProductImages.FirstOrDefault()?.ImageData
                })
                .ToList();

            ProductsItemsControl.ItemsSource = result;
        }

        private void CategoryFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryFilterComboBox.SelectedValue != null && (int)CategoryFilterComboBox.SelectedValue != 0)
            {
                int catId = (int)CategoryFilterComboBox.SelectedValue;
                var subs = db.SubCategories.Where(s => s.MainCategoryId == catId).ToList();
                subs.Insert(0, new SubCategories { Id = 0, Name = "Все подкатегории" });
                SubCategoryFilterComboBox.ItemsSource = subs;
            }
            else
            {
                SubCategoryFilterComboBox.ItemsSource = new[] { new SubCategories { Id = 0, Name = "Все подкатегории" } };
            }
            SubCategoryFilterComboBox.SelectedIndex = 0;
            ApplyFilters();
        }

        private void SubCategoryFilter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void PriceFromTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void PriceToTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var productItem = button?.Tag as ProductDisplayItem;
            if (productItem == null) return;

            var existing = cart.FirstOrDefault(c => c.ProductId == productItem.Id);
            if (existing != null)
                existing.Quantity++;
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = productItem.Id,
                    Name = productItem.Name,
                    Price = productItem.Price,
                    Quantity = 1,
                    Dosage = productItem.Dosage,
                    Volume = productItem.Volume,
                    IsPrescription = productItem.PrescriptionRequired
                });
            }

            UpdateTotal();
        }

        private void Minus_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.Tag as CartItem;
            if (item == null) return;

            if (item.Quantity > 1)
                item.Quantity--;
            else
                cart.Remove(item);

            UpdateTotal();
        }

        private void Plus_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.Tag as CartItem;
            if (item == null) return;

            item.Quantity++;
            UpdateTotal();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.Tag as CartItem;
            if (item == null) return;

            cart.Remove(item);
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            foreach (var item in cart)
                item.Sum = item.Price * item.Quantity;

            TotalText.Text = $"{cart.Sum(i => i.Sum):F2} ₽";
        }

        private void Pay_Click(object sender, RoutedEventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Корзина пуста");
                return;
            }
            var rxItems = cart.Where(i => i.IsPrescription).ToList();

            if (rxItems.Any())
            {
                var result = MessageBox.Show(
                    "В чеке есть рецептурные товары:\n\n" +
                    string.Join("\n", rxItems.Select(i => i.Name)) +
                    "\n\nПодтвердить наличие рецептов?",
                    "Проверка рецептов",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            var paymentWindow = new PaymentWindow();
            paymentWindow.Owner = Window.GetWindow(this);

            if (paymentWindow.ShowDialog() != true)
                return;

            string paymentType = paymentWindow.PaymentType;

            try
            {
                // проверка остатков
                foreach (var item in cart)
                {
                    var product = db.Products.First(p => p.Id == item.ProductId);

                    if (product.Quantity < item.Quantity)
                    {
                        MessageBox.Show($"Недостаточно товара: {item.Name}");
                        return;
                    }
                }

                // создаём продажу
                var sale = new Sales
                {
                    SaleDate = DateTime.Now,
                    TotalAmount = cart.Sum(i => i.Price * i.Quantity),
                    UserId = App.currentUser?.Id ?? 1,
                    PaymentType = paymentType
                };

                db.Sales.Add(sale);
                db.SaveChanges();

                // списание + SaleItems
                foreach (var item in cart)
                {
                    var product = db.Products.First(p => p.Id == item.ProductId);
                    product.Quantity -= item.Quantity;

                    db.SaleItems.Add(new SaleItems
                    {
                        SaleId = sale.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    });
                }

                db.SaveChanges();

                var receiptItems = cart.Select(i => new ReceiptItem
                {
                    Name = i.Name,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    Sum = i.Price * i.Quantity
                }).ToList();

                CreatePdfReceipt(sale, receiptItems);

                cart.Clear();
                UpdateTotal();
                LoadProducts();

                if (Window.GetWindow(this) is MainWindow main)
                    main.ProductsViewControl?.RefreshData();

                MessageBox.Show($"Чек №{sale.Id} оформлен");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }
        private void CreatePdfReceipt(Sales sale, List<ReceiptItem> items)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            string folderPath = Path.Combine(
                desktop,
                "Аптека",
                "Чеки",
                sale.SaleDate.ToString("yyyy-MM-dd")
            );

            Directory.CreateDirectory(folderPath);

            string fileName = $"Чек_{sale.Id}_{sale.SaleDate:HHmmss}.pdf";
            string path = Path.Combine(folderPath, fileName);

            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                Document doc = new Document(PageSize.A4, 30, 30, 30, 30);
                PdfWriter.GetInstance(doc, fs);

                doc.Open();

                string fontPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Fonts",
                    "DejaVuSans.ttf"
                );

                BaseFont bf = BaseFont.CreateFont(
                    fontPath,
                    BaseFont.IDENTITY_H,
                    BaseFont.EMBEDDED
                );

                Font titleFont = new Font(bf, 16, Font.BOLD);
                Font textFont = new Font(bf, 11);
                Font smallFont = new Font(bf, 9);

                doc.Add(new Paragraph("КАССОВЫЙ ЧЕК", titleFont));
                doc.Add(new Paragraph($"Дата: {sale.SaleDate:dd.MM.yyyy HH:mm:ss}", textFont));
                doc.Add(new Paragraph($"Чек №: {sale.Id}", textFont));
                doc.Add(new Paragraph($"Продавец: {App.currentUser?.FullName}", textFont));
                doc.Add(new Paragraph($"Оплата: {sale.PaymentType}", textFont));
                doc.Add(new Paragraph(" "));

                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 4f, 1.5f, 1.5f, 2f });

                table.AddCell(new Phrase("Товар", textFont));
                table.AddCell(new Phrase("Цена", textFont));
                table.AddCell(new Phrase("Кол-во", textFont));
                table.AddCell(new Phrase("Сумма", textFont));

                foreach (var i in items)
                {
                    table.AddCell(new Phrase(i.Name ?? "", textFont));
                    table.AddCell(new Phrase(i.Price.ToString("F2"), textFont));
                    table.AddCell(new Phrase(i.Quantity.ToString(), textFont));
                    table.AddCell(new Phrase(i.Sum.ToString("F2"), textFont));
                }

                doc.Add(table);

                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph($"ИТОГО: {items.Sum(x => x.Sum):F2} ₽", titleFont));

                doc.Add(new Paragraph(" "));

                doc.Close();
            }

            MessageBox.Show("Чек сохранён");
        }

    }
    public class ReceiptItem
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Sum { get; set; }
    }
    public class ProductDisplayItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Dosage { get; set; }
        public string Volume { get; set; }
        public bool PrescriptionRequired { get; set; }
        public Manufacturers Manufacturers { get; set; }
        public SubCategories SubCategories { get; set; }
        public byte[] MainImage { get; set; }
    }

    public class CartItem : System.ComponentModel.INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Dosage { get; set; }
        public string Volume { get; set; }
        public bool IsPrescription { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                Sum = Price * Quantity;
            }
        }

        private decimal _sum;
        public decimal Sum
        {
            get => _sum;
            set
            {
                _sum = value;
                OnPropertyChanged(nameof(Sum));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}