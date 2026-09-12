using System.Windows;
using System.Windows.Input;
using WarehousePOS.Desktop.ViewModels.Auth;

namespace WarehousePOS.Desktop.Views.Auth;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;
    private readonly TaskCompletionSource<bool> _loginCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.LoginSucceeded += OnLoginSucceeded;
        Loaded += (_, _) => UsernameBox.Focus();
        Closed += (_, _) => _loginCompletion.TrySetResult(false);
    }

    public Task<bool> WaitForLoginAsync() => _loginCompletion.Task;

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        // Pass password manually — PasswordBox is not data-bindable for security
        _vm.LoginCommand.Execute(PasswordBox.Password);
    }

    private void UsernameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Down)
        {
            PasswordBox.Focus();
            e.Handled = true;
        }
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up)
        {
            UsernameBox.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            LoginButton.Focus();
            e.Handled = true;
        }
        // Allow pressing Enter in the password box to submit
        else if (e.Key == Key.Enter)
        {
            _vm.LoginCommand.Execute(PasswordBox.Password);
            e.Handled = true;
        }
    }

    private void LoginButton_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up)
        {
            PasswordBox.Focus();
            e.Handled = true;
        }
    }

    private void OnLoginSucceeded()
    {
        _loginCompletion.TrySetResult(true);
    }
}
