using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WarehousePOS.Desktop.ViewModels.Users;

namespace WarehousePOS.Desktop.Views.Users;

public partial class UserManagementView
{
    private readonly UserManagementViewModel _vm;
    private bool _isPasswordVisible;

    public UserManagementView(UserManagementViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;
        viewModel.ClearPasswordRequested += ClearPassword;
    }

    public async Task InitAsync() => await _vm.LoadAsync();

    private void PasswordControl_GotFocus(object sender, RoutedEventArgs e)
    {
        PasswordContainer.BorderBrush = (Brush)FindResource("PrimaryBrush");
    }

    private void PasswordControl_LostFocus(object sender, RoutedEventArgs e)
    {
        PasswordContainer.BorderBrush = (Brush)FindResource("BorderBrush");
    }

    private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;

        if (_isPasswordVisible)
        {
            PasswordTextBox.Text = PasswordBox.Password;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordTextBox.Visibility = Visibility.Visible;
            EyeIconPath.Data = (Geometry)FindResource("EyeSlashIcon");
            TogglePasswordButton.ToolTip = "Hide password";
            PasswordTextBox.Focus();
            PasswordTextBox.CaretIndex = PasswordTextBox.Text.Length;
        }
        else
        {
            PasswordBox.Password = PasswordTextBox.Text;
            PasswordTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
            EyeIconPath.Data = (Geometry)FindResource("EyeOpenIcon");
            TogglePasswordButton.ToolTip = "Show password";
            PasswordBox.Focus();
        }
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.NewPassword = passwordBox.Password;
            if (!_isPasswordVisible)
            {
                PasswordTextBox.Text = passwordBox.Password;
            }
        }
    }

    private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is UserManagementViewModel viewModel && sender is TextBox textBox)
        {
            viewModel.NewPassword = textBox.Text;
            if (_isPasswordVisible)
            {
                PasswordBox.Password = textBox.Text;
            }
        }
    }

    private void ClearPassword()
    {
        PasswordBox.Clear();
        PasswordTextBox.Clear();
    }
}
