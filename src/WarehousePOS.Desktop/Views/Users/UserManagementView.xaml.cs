using System.Windows;
using WarehousePOS.Desktop.ViewModels.Users;

namespace WarehousePOS.Desktop.Views.Users;

public partial class UserManagementView
{
    public UserManagementView(UserManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.ClearPasswordRequested += ClearPassword;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserManagementViewModel viewModel && sender is System.Windows.Controls.PasswordBox passwordBox)
            viewModel.NewPassword = passwordBox.Password;
    }

    private void ClearPassword()
    {
        PasswordBox.Clear();
    }
}
