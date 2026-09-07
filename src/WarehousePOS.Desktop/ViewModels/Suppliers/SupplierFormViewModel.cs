using System.Collections.ObjectModel;
using WarehousePOS.Application.Products;
using WarehousePOS.Application.Suppliers;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.Validation;
using WarehousePOS.Desktop.ViewModels;
using WarehousePOS.Desktop.ViewModels.Products;

namespace WarehousePOS.Desktop.ViewModels.Suppliers;

public sealed class SupplierFormViewModel : ViewModelBase
{
    private readonly ISupplierService  _service;
    private readonly IProductService   _productService;
    private readonly INavigationService _nav;

    private int? _editingId;
    private string _name          = string.Empty;
    private string _contactPerson = string.Empty;
    private string _phone         = string.Empty;
    private string _email         = string.Empty;
    private string _address       = string.Empty;
    private string _errorMessage  = string.Empty;
    private bool   _isBusy;
    private int?   _selectedProductIdToAdd;

    public ObservableCollection<ProductDto> AvailableProducts { get; } = [];
    public ObservableCollection<ProductDto> ProvidedProducts  { get; } = [];

    public string Name          { get => _name;          set { if (SetField(ref _name, value)) RefreshValidation(); } }
    public string ContactPerson { get => _contactPerson; set => SetField(ref _contactPerson, value); }
    public string Phone         { get => _phone;         set { if (SetField(ref _phone, value)) RefreshValidation(); } }
    public string Email         { get => _email;         set { if (SetField(ref _email, value)) RefreshValidation(); } }
    public string Address       { get => _address;       set => SetField(ref _address, value); }

    public string? NameError             => string.IsNullOrWhiteSpace(Name) ? "Supplier Name is required." : null;
    public string? PhoneError            => ContactValidation.GetPhoneError(Phone);
    public string? EmailError            => ContactValidation.GetEmailError(Email);
    public string? ProvidedProductsError => ProvidedProducts.Count == 0 ? "At least one product is mandatory." : null;

    public string ErrorMessage  { get => _errorMessage;  set { SetField(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError        => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsBusy          { get => _isBusy;        set => SetField(ref _isBusy, value); }
    public string Title         => _editingId.HasValue ? "Edit Supplier" : "New Supplier";

    public int? SelectedProductIdToAdd
    {
        get => _selectedProductIdToAdd;
        set => SetField(ref _selectedProductIdToAdd, value);
    }

    public event Action? SaveCompleted;
    public event Action? CreateNewProductRequested;
    public event Action<ProductDto>? NavigateToProductRequested;

    public RelayCommand SaveCommand                        { get; }
    public RelayCommand CancelCommand                      { get; }
    public RelayCommand AddProductCommand                  { get; }
    public RelayCommand<ProductDto> RemoveProductCommand   { get; }
    public RelayCommand CreateNewProductCommand            { get; }
    public RelayCommand<ProductDto> NavigateToProductPageCommand { get; }

    public SupplierFormViewModel(
        ISupplierService service,
        IProductService productService,
        INavigationService nav)
    {
        _service        = service;
        _productService = productService;
        _nav            = nav;

        SaveCommand                  = new RelayCommand(async () => await SaveAsync(), () => !IsBusy && GetValidationError() is null);
        CancelCommand                = new RelayCommand(() => SaveCompleted?.Invoke());
        AddProductCommand            = new RelayCommand(AddProductToSupplier);
        RemoveProductCommand         = new RelayCommand<ProductDto>(RemoveProductFromSupplier);
        CreateNewProductCommand      = new RelayCommand(() => CreateNewProductRequested?.Invoke());
        NavigateToProductPageCommand = new RelayCommand<ProductDto>(NavigateToProductPage);

        ProvidedProducts.CollectionChanged += (_, _) => RefreshValidation();
    }

    public async Task LoadAsync(SupplierDto? dto = null)
    {
        _editingId     = dto?.Id;
        Name           = dto?.Name ?? string.Empty;
        ContactPerson  = dto?.ContactPerson ?? string.Empty;
        Phone          = dto?.Phone ?? string.Empty;
        Email          = dto?.Email ?? string.Empty;
        Address        = dto?.Address ?? string.Empty;
        ErrorMessage   = string.Empty;
        SelectedProductIdToAdd = null;

        ProvidedProducts.Clear();
        AvailableProducts.Clear();

        var allProducts = await _productService.GetAllAsync();
        foreach (var p in allProducts)
        {
            AvailableProducts.Add(p);
        }

        if (!string.IsNullOrWhiteSpace(dto?.ProvidedProducts))
        {
            var namesOrSkus = dto.ProvidedProducts.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var token in namesOrSkus)
            {
                var matched = allProducts.FirstOrDefault(p => p.Name.Equals(token, StringComparison.OrdinalIgnoreCase) || p.SKU.Equals(token, StringComparison.OrdinalIgnoreCase));
                if (matched is not null)
                {
                    if (!ProvidedProducts.Any(p => p.Id == matched.Id))
                        ProvidedProducts.Add(matched);
                }
                else
                {
                    // Fallback for custom text product
                    ProvidedProducts.Add(new ProductDto(0, token, token, null, null, 0, 0, 0, 5, true, false, 1, "General"));
                }
            }
        }

        OnPropertyChanged(nameof(Title));
        RefreshValidation();
    }

    public void AddProductToSupplier()
    {
        if (!SelectedProductIdToAdd.HasValue || SelectedProductIdToAdd.Value <= 0) return;

        var product = AvailableProducts.FirstOrDefault(p => p.Id == SelectedProductIdToAdd.Value);
        if (product is not null && !ProvidedProducts.Any(p => p.Id == product.Id))
        {
            ProvidedProducts.Add(product);
        }

        SelectedProductIdToAdd = null;
        RefreshValidation();
    }

    public void AddNewlyCreatedProduct(ProductDto product)
    {
        if (!AvailableProducts.Any(p => p.Id == product.Id))
            AvailableProducts.Add(product);

        if (!ProvidedProducts.Any(p => p.Id == product.Id))
            ProvidedProducts.Add(product);

        RefreshValidation();
    }

    private void RemoveProductFromSupplier(ProductDto? product)
    {
        if (product is null) return;
        ProvidedProducts.Remove(product);
        RefreshValidation();
    }

    private void NavigateToProductPage(ProductDto? product)
    {
        if (product is null) return;
        SaveCompleted?.Invoke();
        _nav.NavigateTo<ProductListViewModel>();
        NavigateToProductRequested?.Invoke(product);
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        var validationError = GetValidationError();
        if (validationError is not null) { ErrorMessage = validationError; return; }

        IsBusy = true;
        try
        {
            var providedProductsStr = string.Join(", ", ProvidedProducts.Select(p => p.Name));

            if (_editingId.HasValue)
                await _service.UpdateAsync(new UpdateSupplierRequest(
                    _editingId.Value, Name, Null(ContactPerson), Null(Phone), Null(Email), Null(Address), providedProductsStr));
            else
                await _service.CreateAsync(new CreateSupplierRequest(
                    Name, Null(ContactPerson), Null(Phone), Null(Email), Null(Address), providedProductsStr));

            SaveCompleted?.Invoke();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    private static string? Null(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private void RefreshValidation()
    {
        OnPropertyChanged(nameof(NameError));
        OnPropertyChanged(nameof(PhoneError));
        OnPropertyChanged(nameof(EmailError));
        OnPropertyChanged(nameof(ProvidedProductsError));
        ErrorMessage = GetValidationError() ?? string.Empty;
        SaveCommand.RaiseCanExecuteChanged();
    }

    private string? GetValidationError()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "Supplier Name is required.";

        if (ProvidedProducts.Count == 0)
            return "What product does this person provide? At least one product is mandatory.";

        return ContactValidation.GetPhoneError(Phone) ?? ContactValidation.GetEmailError(Email);
    }
}
