using WarehousePOS.Application.Sales;
using WarehousePOS.Desktop.ViewModels;
using WarehousePOS.Desktop.Validation;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Desktop.ViewModels.Sales;

public sealed class CustomerFormViewModel : ViewModelBase
{
    private readonly ICustomerService _service;

    private int? _editingId;
    private string _name         = string.Empty;
    private SaleType _type       = SaleType.Retail;
    private string _phone        = string.Empty;
    private string _email        = string.Empty;
    private string _address      = string.Empty;
    private string _errorMessage = string.Empty;
    private bool   _isBusy;

    public string Name         { get => _name;         set { if (SetField(ref _name, value)) RefreshValidation(); } }
    public SaleType Type       { get => _type;         set => SetField(ref _type, value); }
    public string Phone        { get => _phone;        set { if (SetField(ref _phone, value)) RefreshValidation(); } }
    public string Email        { get => _email;        set { if (SetField(ref _email, value)) RefreshValidation(); } }
    public string Address      { get => _address;      set => SetField(ref _address, value); }
    public string ErrorMessage { get => _errorMessage; set { SetField(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError       => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsBusy         { get => _isBusy;       set => SetField(ref _isBusy, value); }
    public string Title        => _editingId.HasValue ? "Edit Customer" : "New Customer";

    public Array SaleTypes => Enum.GetValues(typeof(SaleType));

    public event Action? SaveCompleted;

    public RelayCommand SaveCommand   { get; }
    public RelayCommand CancelCommand { get; }

    public CustomerFormViewModel(ICustomerService service)
    {
        _service = service;
        SaveCommand   = new RelayCommand(async () => await SaveAsync(), () => !IsBusy && GetValidationError() is null);
        CancelCommand = new RelayCommand(() => SaveCompleted?.Invoke());
    }

    public void Load(CustomerDto? dto = null)
    {
        _editingId   = dto?.Id;
        Name         = dto?.Name ?? string.Empty;
        Type         = dto?.Type ?? SaleType.Retail;
        Phone        = dto?.Phone ?? string.Empty;
        Email        = dto?.Email ?? string.Empty;
        Address      = dto?.Address ?? string.Empty;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(Title));
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        var validationError = GetValidationError();
        if (validationError is not null) { ErrorMessage = validationError; return; }

        IsBusy = true;
        try
        {
            if (_editingId.HasValue)
                await _service.UpdateAsync(new UpdateCustomerRequest(
                    _editingId.Value, Name, Type, Null(Phone), Null(Email), Null(Address)));
            else
                await _service.CreateAsync(new CreateCustomerRequest(
                    Name, Type, Null(Phone), Null(Email), Null(Address)));

            SaveCompleted?.Invoke();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    private static string? Null(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private void RefreshValidation()
    {
        ErrorMessage = GetValidationError() ?? string.Empty;
        SaveCommand.RaiseCanExecuteChanged();
    }

    private string? GetValidationError()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "Name is required.";
        return ContactValidation.GetPhoneError(Phone) ?? ContactValidation.GetEmailError(Email);
    }
}
