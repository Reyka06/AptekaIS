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
    public partial class ReceiptView : UserControl
    {
        private AptekaISDbEntities db;

        public ReceiptView()
        {
            InitializeComponent();
            db = new AptekaISDbEntities();
            LoadReceipts();
        }

        private void LoadReceipts()
        {
            var query = db.Receipts
                .Include("Requests")
                .Include("Requests.Suppliers")
                .Select(r => new
                {
                    r.Id,
                    RequestId = r.RequestId,
                    RequestDate = r.Requests.RequestDate,
                    SupplierName = r.Requests.Suppliers.Name,
                    r.Status
                })
                .ToList();

            ReceiptsDataGrid.ItemsSource = query;
        }

        // 🔥 КНОПКА "ПРОСМОТРЕТЬ"
        private void OpenReceipt_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedReceipt();
        }

        private void OpenSelectedReceipt()
        {
            var selected = ReceiptsDataGrid.SelectedItem;

            if (selected == null)
            {
                MessageBox.Show("Выберите приход");
                return;
            }

            dynamic row = selected;
            int requestId = row.RequestId;

            var acceptWindow = new AcceptRequestWindow(requestId);
            acceptWindow.Owner = Window.GetWindow(this);

            if (acceptWindow.ShowDialog() == true)
            {
                RefreshData();

                if (Window.GetWindow(this) is MainWindow main)
                {
                    main.ProductsViewControl?.RefreshData();
                }
            }
        }

        public void RefreshData()
        {
            db.Dispose();
            db = new AptekaISDbEntities();
            LoadReceipts();
        }
    }
}
