using System.Windows;
using WarehousePOS.Desktop.ViewModels.Auth;

namespace WarehousePOS.Desktop.Views.Auth;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.LoginSucceeded += OnLoginSucceeded;
        Loaded += (_, _) => UsernameBox.Focus();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        // Pass password manually — PasswordBox is not data-bindable for security
        _vm.LoginCommand.Execute(PasswordBox.Password);
    }

    private void OnLoginSucceeded()
    {
        DialogResult = true;
        Close();
    }
}
