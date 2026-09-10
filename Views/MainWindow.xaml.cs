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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AptekaIS.Views
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();

            if (App.currentUser != null)
            {
                string roleName = App.currentUser.Roles?.Name ?? "Фармацевт";
                UserInfoText.Text = $"{App.currentUser.FullName} — {roleName}";
            }

            // 👇 ДОБАВЛЯЕМ ОГРАНИЧЕНИЯ ПО РОЛЯМ
            SetPermissionsByRole();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            UpdateDateTime();
        }

        // 👇 НОВЫЙ МЕТОД ДЛЯ ОГРАНИЧЕНИЙ
        private void SetPermissionsByRole()
        {
            string roleName = App.currentUser?.Roles?.Name;

            // Заведующий - видит всё
            if (roleName == "Заведующий")
            {
                ShowAllTabs();
                return;
            }

            // Фармацевт - ограниченный доступ
            if (roleName == "Фармацевт")
            {
                // Скрываем запрещенные вкладки
                TabUsers.Visibility = Visibility.Collapsed;
                TabSuppliers.Visibility = Visibility.Collapsed;
                TabReports.Visibility = Visibility.Collapsed;

                // Оставляем видимыми:
                // TabCashier - Касса
                // TabReceipt - Приход
                // TabProducts - Товары
                return;
            }

            // Если роль неизвестна - только касса
            HideAllTabs();
            TabCashier.Visibility = Visibility.Visible;
        }

        private void HideAllTabs()
        {
            foreach (var item in MainTabControl.Items)
            {
                if (item is TabItem tab)
                {
                    tab.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ShowAllTabs()
        {
            foreach (var item in MainTabControl.Items)
            {
                if (item is TabItem tab)
                {
                    tab.Visibility = Visibility.Visible;
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDateTime();
        }

        private void UpdateDateTime()
        {
            TimeText.Text = DateTime.Now.ToString("g");
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTabControl.SelectedItem is TabItem tab)
            {
                var header = ((TextBlock)((StackPanel)tab.Header).Children[1]).Text;

                switch (header)
                {
                    case "Товары":
                        using (ProductsViewControl.BeginUiLock())
                        {
                            ProductsViewControl.RefreshData();
                        }
                        break;

                    case "Приход":
                        ReceiptViewControl?.RefreshData();
                        break;

                    case "Касса":
                        CashierViewControl?.RefreshData();
                        break;
                }
            }
        }
    }
}