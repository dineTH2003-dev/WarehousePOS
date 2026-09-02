using Serilog;
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

        // Track login result separately so we can invoke the event outside the try block,
        // after IsBusy has been reset in the finally clause. This prevents the LoginSucceeded
        // event from firing while the busy indicator is still shown, and ensures any exception
        // thrown BY the event handler itself is not masked by the catch block below.
        AuthResult? loginResult = null;

        try
        {
            loginResult = await _authService.LoginAsync(new LoginRequest(Username, password!));

            if (loginResult is null)
            {
                ErrorMessage = "Invalid username or password.";
                OnPropertyChanged(nameof(HasError));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Login attempt failed for user {Username}", Username);
            ErrorMessage = $"Login error: {ex.Message}";
            OnPropertyChanged(nameof(HasError));
        }
        finally
        {
            IsBusy = false;
        }

        // Only proceed with session + event AFTER IsBusy is cleared and outside try/catch,
        // so any exception from SetUser or LoginSucceeded is surfaced clearly.
        if (loginResult is not null)
        {
            _session.SetUser(loginResult);
            LoginSucceeded?.Invoke();
        }
    }
}
