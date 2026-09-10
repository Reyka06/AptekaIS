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
using System.Windows.Shapes;

namespace AptekaIS.Views
{
    public partial class AddEditUserWindow : Window
    {
        private AptekaISDbEntities db;
        private Users _editingUser;

        // Добавление
        public AddEditUserWindow()
        {
            InitializeComponent();

            db = new AptekaISDbEntities();

            LoadRoles();
        }

        // Редактирование
        public AddEditUserWindow(Users user) : this()
        {
            // Загружаем пользователя из текущего контекста
            _editingUser = db.Users.FirstOrDefault(u => u.Id == user.Id);

            LoadUserData();
        }

        private void LoadRoles()
        {
            RoleComboBox.ItemsSource = db.Roles.ToList();
        }

        private void LoadUserData()
        {
            if (_editingUser == null)
                return;

            LoginTextBox.Text = _editingUser.Login;
            PasswordTextBox.Text = _editingUser.Password;
            FullNameTextBox.Text = _editingUser.FullName;

            RoleComboBox.SelectedValue = _editingUser.RoleId;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordTextBox.Text.Trim();
            string fullName = FullNameTextBox.Text.Trim();

            // Проверка полей
            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fullName))
            {
                MessageBox.Show(
                    "Заполните все поля",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // Проверка роли
            if (RoleComboBox.SelectedValue == null)
            {
                MessageBox.Show(
                    "Выберите роль",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            int roleId = (int)RoleComboBox.SelectedValue;

            try
            {
                // Добавление
                if (_editingUser == null)
                {
                    Users newUser = new Users
                    {
                        Login = login,
                        Password = password,
                        FullName = fullName,
                        RoleId = roleId
                    };

                    db.Users.Add(newUser);
                }
                // Редактирование
                else
                {
                    _editingUser.Login = login;
                    _editingUser.Password = password;
                    _editingUser.FullName = fullName;
                    _editingUser.RoleId = roleId;
                }

                db.SaveChanges();

                DialogResult = true;

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при сохранении:\n" + ex.Message,
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
}