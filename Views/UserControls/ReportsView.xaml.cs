using AptekaIS.Models;
using ClosedXML.Excel;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AptekaIS.Views.UserControls
{
    public partial class ReportsView : UserControl
    {
        private AptekaISDbEntities db;

        public ReportsView()
        {
            InitializeComponent();
            db = new AptekaISDbEntities();

            // Устанавливаем даты по умолчанию
            DateFrom.SelectedDate = DateTime.Now.AddMonths(-1);
            DateTo.SelectedDate = DateTime.Now;
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ReportGrid.Items.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта");
                    return;
                }

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                string folder = System.IO.Path.Combine(
                    desktop,
                    "Аптека",
                    "Отчеты");

                System.IO.Directory.CreateDirectory(folder);

                string reportName =
                    (ReportTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()
                    ?? "Отчет";

                string filePath = System.IO.Path.Combine(
                    folder,
                    $"Отчет_{reportName}_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx");

                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Отчет");

                    // Заголовки
                    for (int i = 0; i < ReportGrid.Columns.Count; i++)
                    {
                        ws.Cell(1, i + 1).Value = ReportGrid.Columns[i].Header?.ToString();
                        ws.Cell(1, i + 1).Style.Font.Bold = true;
                    }

                    int row = 2;

                    foreach (var item in ReportGrid.Items)
                    {
                        if (item == null || item == CollectionView.NewItemPlaceholder)
                            continue;

                        for (int col = 0; col < ReportGrid.Columns.Count; col++)
                        {
                            var column = ReportGrid.Columns[col] as DataGridBoundColumn;
                            var binding = column?.Binding as System.Windows.Data.Binding;

                            if (binding != null)
                            {
                                var prop = item.GetType().GetProperty(binding.Path.Path);
                                var value = prop?.GetValue(item);

                                ws.Cell(row, col + 1).Value = value?.ToString() ?? "";
                            }
                        }

                        row++;
                    }

                    ws.Columns().AdjustToContents();
                    wb.SaveAs(filePath);
                }

                MessageBox.Show("Отчёт сохранён");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта: " + ex.Message);
            }
        }

        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            // Проверка выбора дат
            if (DateFrom.SelectedDate == null || DateTo.SelectedDate == null)
            {
                MessageBox.Show("Выберите период", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selected = (ReportTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            DateTime dateFrom = DateFrom.SelectedDate.Value;
            DateTime dateTo = DateTo.SelectedDate.Value;
            dateTo = dateTo.Date.AddDays(1).AddSeconds(-1);

            switch (selected)
            {
                case "Продажи":
                    LoadSalesReport(dateFrom, dateTo);
                    break;
                case "Приход товаров":
                    LoadReceiptsReport(dateFrom, dateTo);
                    break;
                case "Списания":
                    LoadWriteOffsReport(dateFrom, dateTo);
                    break;
                case "Заявки":
                    LoadRequestsReport(dateFrom, dateTo);
                    break;
            }
        }

        private void LoadSalesReport(DateTime dateFrom, DateTime dateTo)
        {
            try
            {
                var query = (from s in db.Sales
                             join si in db.SaleItems on s.Id equals si.SaleId
                             join p in db.Products on si.ProductId equals p.Id
                             join u in db.Users on s.UserId equals u.Id
                             where s.SaleDate >= dateFrom && s.SaleDate <= dateTo
                             select new
                             {
                                 Дата = s.SaleDate,
                                 Товар = p.Name,
                                 Количество = si.Quantity,
                                 Цена = si.UnitPrice,
                                 Сумма = si.Quantity * si.UnitPrice,
                                 Итого_по_чеку = s.TotalAmount,
                                 Тип_оплаты = s.PaymentType,
                                 Кассир = u.FullName
                             })
                             .OrderBy(r => r.Дата)
                             .ToList();

                ReportGrid.ItemsSource = query;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void LoadReceiptsReport(DateTime dateFrom, DateTime dateTo)
        {
            try
            {
                var query = (from r in db.Receipts
                             join ri in db.ReceiptItems on r.Id equals ri.ReceiptId
                             join p in db.Products on ri.ProductId equals p.Id
                             join s in db.Suppliers on r.SupplierId equals s.Id
                             where r.ReceiptDate >= dateFrom && r.ReceiptDate <= dateTo
                             select new
                             {
                                 Дата = r.ReceiptDate,
                                 Товар = p.Name,
                                 Количество = ri.Quantity,
                                 Цена_закупки = ri.PurchasePrice,
                                 Сумма = ri.Quantity * ri.PurchasePrice,
                                 Поставщик = s.Name
                             })
                             .OrderBy(r => r.Дата)
                             .ToList();

                ReportGrid.ItemsSource = query;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void LoadWriteOffsReport(DateTime dateFrom, DateTime dateTo)
        {
            try
            {
                var query = (from w in db.WriteOffs
                             join p in db.Products on w.ProductId equals p.Id
                             join u in db.Users on w.UserId equals u.Id
                             where w.WriteOffDate >= dateFrom && w.WriteOffDate <= dateTo
                             select new
                             {
                                 Дата = w.WriteOffDate,
                                 Товар = p.Name,
                                 Количество = w.Quantity,
                                 Причина = w.Reason,
                                 Кто_списал = u.FullName
                             })
                             .OrderByDescending(r => r.Дата)
                             .ToList();

                ReportGrid.ItemsSource = query;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void LoadRequestsReport(DateTime dateFrom, DateTime dateTo)
        {
            try
            {
                var query = (from r in db.Requests
                             join s in db.Suppliers on r.SupplierId equals s.Id
                             join u in db.Users on r.CreatedByUserId equals u.Id
                             where r.RequestDate >= dateFrom && r.RequestDate <= dateTo
                             select new
                             {
                                 Дата_заявки = r.RequestDate,
                                 Поставщик = s.Name,
                                 Создал = u.FullName
                             })
                             .OrderByDescending(r => r.Дата_заявки)
                             .ToList();

                ReportGrid.ItemsSource = query;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }
    }
}