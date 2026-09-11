using System.Windows;

namespace AptekaIS.Views
{
    public partial class PaymentWindow : Window
    {
        public string PaymentType { get; private set; }

        public PaymentWindow()
        {
            InitializeComponent();
            Loaded += PaymentWindow_Loaded;
        }

        private void PaymentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // гарантируем нормальный owner-контекст
            if (Owner == null && Application.Current.MainWindow != null)
                Owner = Application.Current.MainWindow;
        }

        private void CashButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentType = "Наличные";
            DialogResult = true;
            Close();
        }

        private void CardButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentType = "Карта";
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentType = null;
            DialogResult = false;
            Close();
        }
    }
}