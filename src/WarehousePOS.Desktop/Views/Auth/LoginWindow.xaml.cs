using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WarehousePOS.Desktop.ViewModels.Auth;

namespace WarehousePOS.Desktop.Views.Auth;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;
    private readonly TaskCompletionSource<bool> _loginCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool _isPasswordVisible;

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

    private string CurrentPassword => _isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;

    private void FocusActivePasswordBox()
    {
        if (_isPasswordVisible)
        {
            PasswordTextBox.Focus();
            PasswordTextBox.CaretIndex = PasswordTextBox.Text.Length;
        }
        else
        {
            PasswordBox.Focus();
        }
    }

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
        if (!_isPasswordVisible)
        {
            PasswordTextBox.Text = PasswordBox.Password;
        }
    }

    private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isPasswordVisible)
        {
            PasswordBox.Password = PasswordTextBox.Text;
        }
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.LoginCommand.Execute(CurrentPassword);
    }

    private void UsernameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Down)
        {
            FocusActivePasswordBox();
            e.Handled = true;
        }
    }

    private void PasswordControl_KeyDown(object sender, KeyEventArgs e)
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
            _vm.LoginCommand.Execute(CurrentPassword);
            e.Handled = true;
        }
    }

    private void LoginButton_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up)
        {
            FocusActivePasswordBox();
            e.Handled = true;
        }
    }

    private void OnLoginSucceeded()
    {
        _loginCompletion.TrySetResult(true);
    }
}

