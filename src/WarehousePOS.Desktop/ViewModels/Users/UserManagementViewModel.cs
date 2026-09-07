using System.Collections.ObjectModel;
using System.Windows;
using WarehousePOS.Application.Authentication;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Desktop.Services;

namespace WarehousePOS.Desktop.ViewModels.Users;

public sealed class UserManagementViewModel : ViewModelBase
{
    private readonly IUserManagementService _userService;
    private readonly SessionContext _session;

    private ObservableCollection<UserDto> _users = [];
    private UserDto? _selectedUser;
    private string _username = string.Empty;
    private string _fullName = string.Empty;
    private string _newPassword = string.Empty;
    private UserRole _role = UserRole.Worker;
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private bool _isEditing;
    private bool _isBusy;

    public ObservableCollection<UserDto> Users => _users;
    public IReadOnlyList<UserRole> Roles { get; } = [UserRole.Admin, UserRole.Worker];

    public UserDto? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetField(ref _selectedUser, value) && value is not null)
                LoadForEdit(value);
        }
    }

    public string Username { get => _username; set => SetField(ref _username, value); }
    public string FullName { get => _fullName; set => SetField(ref _fullName, value); }
    public string NewPassword { get => _newPassword; set => SetField(ref _newPassword, value); }
    public UserRole Role { get => _role; set => SetField(ref _role, value); }
    public bool IsEditing { get => _isEditing; private set => SetField(ref _isEditing, value); }
    public bool IsBusy { get => _isBusy; private set => SetField(ref _isBusy, value); }
    public string ErrorMessage { get => _errorMessage; private set { if (SetField(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public string SuccessMessage { get => _successMessage; private set { if (SetField(ref _successMessage, value)) OnPropertyChanged(nameof(HasSuccess)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);
    public string FormTitle => IsEditing ? "Edit User" : "Add User";
    public string SaveButtonText => IsEditing ? "Save Changes" : "Add User";

    public RelayCommand SaveCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand<UserDto> ToggleActiveCommand { get; }
    public event Action? ClearPasswordRequested;

    public UserManagementViewModel(IUserManagementService userService, SessionContext session)
    {
        if (!session.IsAdmin)
            throw new UnauthorizedAccessException("Only an Admin can access User Management.");

        _userService = userService;
        _session = session;
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        ClearCommand = new RelayCommand(ClearForm);
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        ToggleActiveCommand = new RelayCommand<UserDto>(async user => await ToggleActiveAsync(user));
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var users = await _userService.GetAllAsync(_session.CurrentUser.UserId);
            _users = new ObservableCollection<UserDto>(users);
            OnPropertyChanged(nameof(Users));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadForEdit(UserDto user)
    {
        IsEditing = true;
        Username = user.Username;
        FullName = user.FullName;
        Role = user.Role;
        NewPassword = string.Empty;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(SaveButtonText));
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FullName))
        {
            ErrorMessage = "Full name is required.";
            return;
        }

        if (!IsEditing && string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Username is required.";
            return;
        }

        if (!IsEditing && string.IsNullOrWhiteSpace(NewPassword))
        {
            ErrorMessage = "Password is required for a new user.";
            return;
        }

        try
        {
            if (IsEditing && SelectedUser is not null)
            {
                await _userService.UpdateAsync(
                    _session.CurrentUser.UserId,
                    new UpdateUserRequest(SelectedUser.Id, FullName, Role, NewPassword));
                SuccessMessage = $"User '{SelectedUser.Username}' updated successfully.";
            }
            else
            {
                await _userService.CreateAsync(
                    _session.CurrentUser.UserId,
                    new CreateUserRequest(Username, FullName, NewPassword, Role));
                SuccessMessage = $"User '{Username}' added successfully.";
            }

            ClearForm();
            ClearPasswordRequested?.Invoke();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task ToggleActiveAsync(UserDto? user)
    {
        if (user is null) return;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        var action = user.IsActive ? "deactivate" : "activate";
        var confirmation = MessageBox.Show(
            $"Are you sure you want to {action} '{user.Username}'?",
            "Confirm user change",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes)
            return;

        try
        {
            if (user.IsActive)
                await _userService.DeactivateAsync(_session.CurrentUser.UserId, user.Id);
            else
                await _userService.ActivateAsync(_session.CurrentUser.UserId, user.Id);

            SuccessMessage = user.IsActive
                ? $"User '{user.Username}' deactivated."
                : $"User '{user.Username}' activated.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void ClearForm()
    {
        IsEditing = false;
        SelectedUser = null;
        Username = string.Empty;
        FullName = string.Empty;
        NewPassword = string.Empty;
        Role = UserRole.Worker;
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(SaveButtonText));
    }
}
