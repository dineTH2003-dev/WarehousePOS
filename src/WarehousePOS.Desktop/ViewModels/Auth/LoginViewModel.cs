using System.Windows;
using WarehousePOS.Application.Authentication;
using WarehousePOS.Desktop.Services;

namespace WarehousePOS.Desktop.ViewModels.Auth;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly SessionContext _session;

    private string _username = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public string Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    // Password is passed directly from code-behind (PasswordBox.Password)
    // PasswordBox is not data-bindable for security reasons — this is acceptable.
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public event Action? LoginSucceeded;

    public RelayCommand<string> LoginCommand { get; }

    public LoginViewModel(IAuthService authService, SessionContext session)
    {
        _authService = authService;
        _session = session;
        LoginCommand = new RelayCommand<string>(ExecuteLogin, _ => !IsBusy);
    }

    private async void ExecuteLogin(string? password)
    {
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(HasError));

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Please enter your username and password.";
            OnPropertyChanged(nameof(HasError));
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _authService.LoginAsync(new LoginRequest(Username, password!));
            if (result is null)
            {
                ErrorMessage = "Invalid username or password.";
                OnPropertyChanged(nameof(HasError));
                return;
            }

            _session.SetUser(result);
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login error: {ex.Message}";
            OnPropertyChanged(nameof(HasError));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
