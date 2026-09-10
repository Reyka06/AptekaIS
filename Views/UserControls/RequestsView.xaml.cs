using AptekaIS.Models;
using ClosedXML.Excel;
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
    public partial class RequestsView : UserControl
    {
        private AptekaISDbEntities db;

        public RequestsView()
        {
            InitializeComponent();
            db = new AptekaISDbEntities();
            LoadRequests();
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

            // Фармацевт - скрываем кнопки Экспорт и Удалить
            if (roleName == "Фармацевт")
            {
                ExportRequestsButton.Visibility = Visibility.Collapsed;
                DeleteRequestButton.Visibility = Visibility.Collapsed;

                return;
            }

        }

        private void ShowAllButtons()
        {
            NewRequestButton.Visibility = Visibility.Visible;
            ExportRequestsButton.Visibility = Visibility.Visible;
            DeleteRequestButton.Visibility = Visibility.Visible;
        }

        // ✔ НОРМАЛЬНАЯ МОДЕЛЬ ДЛЯ GRID
        private class RequestRow
        {
            public int Id { get; set; }
            public DateTime RequestDate { get; set; }
            public string SupplierName { get; set; }
            public DateTime CreatedAt { get; set; }
            public string CreatedByUserName { get; set; }
        }
        private void ExportRequestDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = RequestsGrid.SelectedItem as RequestRow;

                if (selected == null)
                {
                    MessageBox.Show("Выберите заявку");
                    return;
                }

                // получаем полные данные заявки
                var request = db.Requests
                    .Include("Suppliers")
                    .Include("RequestItems.Products")
                    .FirstOrDefault(r => r.Id == selected.Id);

                if (request == null)
                {
                    MessageBox.Show("Заявка не найдена");
                    return;
                }

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                string folder = System.IO.Path.Combine(
                    desktop,
                    "Аптека",
                    "Заявки");

                System.IO.Directory.CreateDirectory(folder);

                string filePath = System.IO.Path.Combine(
                    folder,
                    $"Заявка_{request.Id}_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx");

                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Заявка");

                    // 📌 ШАПКА
                    ws.Cell(1, 1).Value = "Заявка №";
                    ws.Cell(1, 2).Value = request.Id;

                    ws.Cell(2, 1).Value = "Поставщик";
                    ws.Cell(2, 2).Value = request.Suppliers?.Name;

                    ws.Cell(3, 1).Value = "Дата";
                    ws.Cell(3, 2).Value = request.RequestDate.ToString("dd.MM.yyyy");

                    // 📌 ТАБЛИЦА ТОВАРОВ
                    int row = 5;

                    ws.Cell(row, 1).Value = "Товар";
                    ws.Cell(row, 2).Value = "Количество";

                    ws.Range(row, 1, row, 2).Style.Font.Bold = true;
                    row++;

                    foreach (var item in request.RequestItems)
                    {
                        ws.Cell(row, 1).Value = item.Products.Name;
                        ws.Cell(row, 2).Value = item.RequestedQuantity;
                        row++;
                    }

                    ws.Columns().AdjustToContents();

                    wb.SaveAs(filePath);
                }

                MessageBox.Show("Заявка экспортирована");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }
        private void LoadRequests()
        {
            var query = db.Requests
                .Include("Suppliers")
                .Include("Users")
                .Select(r => new RequestRow
                {
                    Id = r.Id,
                    RequestDate = r.RequestDate,
                    SupplierName = r.Suppliers.Name,
                    CreatedAt = r.CreatedAt,
                    CreatedByUserName = r.Users.FullName
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            RequestsGrid.ItemsSource = query;
        }

        private void NewRequestButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddEditRequestWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (addWindow.ShowDialog() == true)
            {
                ReloadDb();
                LoadRequests();
            }
        }

        private void DeleteRequestButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = RequestsGrid.SelectedItem as RequestRow;

            if (selected == null)
            {
                MessageBox.Show(
                    "Выберите заявку для удаления",
                    "Внимание",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить заявку №{selected.Id}?\nТакже будут удалены связанные приходы",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var request = db.Requests
                    .FirstOrDefault(r => r.Id == selected.Id);

                if (request == null)
                    return;

                // 🔥 удаляем зависимые Receipts
                var receipts = db.Receipts
                    .Where(r => r.RequestId == request.Id)
                    .ToList();

                if (receipts.Any())
                    db.Receipts.RemoveRange(receipts);

                // 🔥 удаляем заявку
                db.Requests.Remove(request);

                db.SaveChanges();

                LoadRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка удаления: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ReloadDb()
        {
            db.Dispose();
            db = new AptekaISDbEntities();
        }
    }
}