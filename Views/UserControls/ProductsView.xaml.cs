using AptekaIS.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AptekaIS.Views.UserControls
{
    public partial class ProductsView : UserControl
    {
        private AptekaISDbEntities db;
        private bool _suppressRefresh = false;

        public ProductsView()
        {
            InitializeComponent();

            db = new AptekaISDbEntities();

            LoadFilters();
            LoadProducts();
            SetPermissions();
        }

        private void SetPermissions()
        {
            string roleName = App.currentUser?.Roles?.Name;

            // Заведующий - видит всё
            if (roleName == "Заведующий")
            {
                ShowAllButtons();
            }

            // Фармацевт - скрываем кнопки
            if (roleName == "Фармацевт")
            {
                AddButton.Visibility = Visibility.Collapsed;
                EditButton.Visibility = Visibility.Collapsed;
                DeleteButton.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowAllButtons()
        {
            AddButton.Visibility = Visibility.Visible;
            EditButton.Visibility = Visibility.Visible;
            DeleteButton.Visibility = Visibility.Visible;
        }

        private void LoadFilters()
        {
            var mainCategories = db.MainCategories.ToList();
            mainCategories.Insert(0, new MainCategories { Id = 0, Name = "Все категории" });

            MainCategoryFilterComboBox.ItemsSource = mainCategories;
            MainCategoryFilterComboBox.SelectedIndex = 0;

            SubCategoryFilterComboBox.ItemsSource = new[]
            {
                new SubCategories { Id = 0, Name = "Все подкатегории" }
            };

            SubCategoryFilterComboBox.SelectedIndex = 0;
        }

        private void LoadProducts(string search = "", int? mainCatId = null, int? subCatId = null)
        {
            var query = db.Products
                .Include(p => p.Manufacturers)
                .Include(p => p.ProductForms)
                .Include(p => p.PackageTypes)
                .Include(p => p.SubCategories)
                .Include("SubCategories.MainCategories")
                .Include(p => p.SpecialProperties)
                .Include(p => p.ProductImages)
                .AsQueryable();

            // ПОИСК
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    (p.Name != null && p.Name.Contains(search)) ||
                    (p.Substance != null && p.Substance.Contains(search)) ||
                    (p.Description != null && p.Description.Contains(search)) ||
                    (p.Manufacturers != null && p.Manufacturers.Name.Contains(search)) ||
                    (p.ProductForms != null && p.ProductForms.Name.Contains(search)) ||
                    (p.PackageTypes != null && p.PackageTypes.Name.Contains(search))
                );
            }

            // ФИЛЬТР ПО ГЛАВНОЙ КАТЕГОРИИ
            if (mainCatId.HasValue && mainCatId.Value != 0)
            {
                query = query.Where(p =>
                    p.SubCategories.MainCategoryId == mainCatId.Value);
            }

            // ФИЛЬТР ПО ПОДКАТЕГОРИИ
            if (subCatId.HasValue && subCatId.Value != 0)
            {
                query = query.Where(p =>
                    p.SubCategoryId == subCatId.Value);
            }

            var list = query.ToList();

            var result = list.Select(p => new ProductWithSpecialProperties
            {
                Id = p.Id,
                Name = p.Name,
                Manufacturers = p.Manufacturers,
                ProductForms = p.ProductForms,
                PackageTypes = p.PackageTypes,
                SubCategories = p.SubCategories,
                Dosage = p.Dosage,
                Volume = p.Volume,
                Price = p.Price,
                Quantity = p.Quantity,
                ExpiryDate = p.ExpiryDate,
                PrescriptionRequired = p.PrescriptionRequired,
                Substance = p.Substance,
                Description = p.Description,
                SpecialPropertiesList = string.Join(", ", p.SpecialProperties.Select(sp => sp.Name)),
                MainImage = p.ProductImages.FirstOrDefault(i => i.IsMain == true)?.ImageData
            }).ToList();

            ProductsGrid.ItemsSource = result;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void MainCategoryFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            var selected = MainCategoryFilterComboBox.SelectedValue as int?;

            if (selected.HasValue && selected.Value != 0)
            {
                var subs = db.SubCategories
                    .Where(s => s.MainCategoryId == selected.Value)
                    .ToList();

                subs.Insert(0, new SubCategories { Id = 0, Name = "Все подкатегории" });

                SubCategoryFilterComboBox.ItemsSource = subs;
                SubCategoryFilterComboBox.SelectedIndex = 0;
            }
            else
            {
                SubCategoryFilterComboBox.ItemsSource = new[]
                {
                    new SubCategories { Id = 0, Name = "Все подкатегории" }
                };

                SubCategoryFilterComboBox.SelectedIndex = 0;
            }

            ApplyFilters();
        }

        private void SubCategoryFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            int? mainCat = MainCategoryFilterComboBox.SelectedValue as int?;
            int? subCat = SubCategoryFilterComboBox.SelectedValue as int?;

            if (mainCat == 0) mainCat = null;
            if (subCat == 0) subCat = null;

            LoadProducts(SearchTextBox.Text, mainCat, subCat);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var w = new AddEditProductWindow();
            w.Owner = Window.GetWindow(this);

            if (w.ShowDialog() == true)
            {
                db = new AptekaISDbEntities();
                LoadProducts();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = ProductsGrid.SelectedItem as ProductWithSpecialProperties;

            if (selected == null)
            {
                MessageBox.Show("Выберите товар");
                return;
            }

            var w = new AddEditProductWindow(selected.Id);
            w.Owner = Window.GetWindow(this);

            if (w.ShowDialog() == true)
            {
                db = new AptekaISDbEntities();
                LoadProducts();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = ProductsGrid.SelectedItem as ProductWithSpecialProperties;

            if (selected == null)
            {
                MessageBox.Show("Выберите товар");
                return;
            }

            var product = db.Products.FirstOrDefault(p => p.Id == selected.Id);

            if (product != null)
            {
                db.Products.Remove(product);
                db.SaveChanges();
                LoadProducts();
            }
        }

        public void RefreshData()
        {
            if (_suppressRefresh)
                return;

            db.Dispose();
            db = new AptekaISDbEntities();

            LoadProducts();
        }

        public IDisposable BeginUiLock()
        {
            _suppressRefresh = true;

            return new ActionOnDispose(() =>
            {
                _suppressRefresh = false;
            });
        }

        public class ActionOnDispose : IDisposable
        {
            private Action _action;

            public ActionOnDispose(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action?.Invoke();
            }
        }
    }

    // Вспомогательный класс для отображения специальных свойств и картинки
    public class ProductWithSpecialProperties : Products
    {
        public string SpecialPropertiesList { get; set; }
        public byte[] MainImage { get; set; }
    }
}