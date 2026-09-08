using System.Windows;
using WarehousePOS.Desktop.ViewModels.Users;

namespace WarehousePOS.Desktop.Views.Users;

public partial class UserManagementView
{
    private readonly UserManagementViewModel _vm;

    public UserManagementView(UserManagementViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;
        viewModel.ClearPasswordRequested += ClearPassword;
    }

    public async Task InitAsync() => await _vm.LoadAsync();

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
