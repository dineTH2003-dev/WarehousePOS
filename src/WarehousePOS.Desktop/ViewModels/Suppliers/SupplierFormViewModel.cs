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
        CancelCommand                = new RelayCommand(() => { ClearDraft(); SaveCompleted?.Invoke(); });
        AddProductCommand            = new RelayCommand(AddProductToSupplier);
        RemoveProductCommand         = new RelayCommand<ProductDto>(RemoveProductFromSupplier);
        CreateNewProductCommand      = new RelayCommand(() => CreateNewProductRequested?.Invoke());
        NavigateToProductPageCommand = new RelayCommand<ProductDto>(NavigateToProductPage);

        ProvidedProducts.CollectionChanged += (_, _) => RefreshValidation();
    }

    public bool HasDraft() =>
        !string.IsNullOrWhiteSpace(_name) ||
        !string.IsNullOrWhiteSpace(_contactPerson) ||
        !string.IsNullOrWhiteSpace(_phone) ||
        !string.IsNullOrWhiteSpace(_email) ||
        !string.IsNullOrWhiteSpace(_address) ||
        ProvidedProducts.Count > 0;

    private List<ProductDto> _allMasterProducts = [];

    public void ClearDraft()
    {
        _editingId = null;
        _name = string.Empty;
        _contactPerson = string.Empty;
        _phone = string.Empty;
        _email = string.Empty;
        _address = string.Empty;
        ErrorMessage = string.Empty;
        SelectedProductIdToAdd = null;
        ProvidedProducts.Clear();
        RefreshAvailableProducts();
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(ContactPerson));
        OnPropertyChanged(nameof(Phone));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(Address));
        OnPropertyChanged(nameof(Title));
        RefreshValidation();
    }

    public async Task LoadAsync(SupplierDto? dto = null)
    {
        var allProducts = await _productService.GetAllAsync();
        _allMasterProducts = allProducts.ToList();

        if (dto is not null)
        {
            _editingId     = dto.Id;
            Name           = dto.Name;
            ContactPerson  = dto.ContactPerson ?? string.Empty;
            Phone          = dto.Phone ?? string.Empty;
            Email          = dto.Email ?? string.Empty;
            Address        = dto.Address ?? string.Empty;
            ErrorMessage   = string.Empty;
            SelectedProductIdToAdd = null;

            ProvidedProducts.Clear();

            if (!string.IsNullOrWhiteSpace(dto.ProvidedProducts))
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
                        ProvidedProducts.Add(new ProductDto(0, token, token, null, null, 0, 0, 0, 5, 0, 0, 0, 0, true, false, 1, "General"));
                    }
                }
            }
        }
        else if (_editingId is null && HasDraft())
        {
            // Preserve user-entered draft fields when returning from creating/viewing products!
        }
        else
        {
            ClearDraft();
        }

        RefreshAvailableProducts();
        OnPropertyChanged(nameof(Title));
        RefreshValidation();
    }

    public void AddProductToSupplier()
    {
        if (!SelectedProductIdToAdd.HasValue || SelectedProductIdToAdd.Value <= 0) return;

        var product = _allMasterProducts.FirstOrDefault(p => p.Id == SelectedProductIdToAdd.Value)
                      ?? AvailableProducts.FirstOrDefault(p => p.Id == SelectedProductIdToAdd.Value);

        if (product is not null && !ProvidedProducts.Any(p => p.Id == product.Id))
        {
            ProvidedProducts.Add(product);
        }

        SelectedProductIdToAdd = null;
        RefreshAvailableProducts();
        RefreshValidation();
    }

    public void AddNewlyCreatedProduct(ProductDto product)
    {
        if (product is null) return;

        var existingMaster = _allMasterProducts.FirstOrDefault(p => p.Id == product.Id || (product.Id > 0 && p.Id == product.Id));
        if (existingMaster is null)
        {
            _allMasterProducts.Add(product);
        }
        else
        {
            var idx = _allMasterProducts.IndexOf(existingMaster);
            _allMasterProducts[idx] = product;
        }

        var existingInProvided = ProvidedProducts.FirstOrDefault(p => p.Id == product.Id || (product.Id > 0 && p.Id == product.Id));
        if (existingInProvided is null)
        {
            ProvidedProducts.Add(product);
        }
        else
        {
            var idx = ProvidedProducts.IndexOf(existingInProvided);
            ProvidedProducts[idx] = product;
        }

        SelectedProductIdToAdd = null;
        RefreshAvailableProducts();
        RefreshValidation();
    }

    private void RemoveProductFromSupplier(ProductDto? product)
    {
        if (product is null) return;
        ProvidedProducts.Remove(product);
        RefreshAvailableProducts();
        RefreshValidation();
    }

    private void RefreshAvailableProducts()
    {
        var currentlySelectedId = SelectedProductIdToAdd;
        AvailableProducts.Clear();

        foreach (var p in _allMasterProducts)
        {
            bool isAlreadyProvided = ProvidedProducts.Any(pp =>
                (p.Id > 0 && pp.Id == p.Id) ||
                string.Equals(pp.Name, p.Name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pp.SKU, p.SKU, StringComparison.OrdinalIgnoreCase));

            if (!isAlreadyProvided)
            {
                AvailableProducts.Add(p);
            }
        }

        if (currentlySelectedId.HasValue && !AvailableProducts.Any(p => p.Id == currentlySelectedId.Value))
        {
            SelectedProductIdToAdd = null;
        }
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

            ClearDraft();
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
