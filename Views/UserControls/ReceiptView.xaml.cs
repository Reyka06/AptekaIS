using AptekaIS.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
