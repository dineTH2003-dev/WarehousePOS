using System.Windows;
using System.Windows.Input;
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

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        // Allow pressing Enter in the password box to submit
        if (e.Key == Key.Enter)
        {
            _vm.LoginCommand.Execute(PasswordBox.Password);
            e.Handled = true;
        }
    }

    private void OnLoginSucceeded()
    {
        DialogResult = true;
        Close();
    }
}
