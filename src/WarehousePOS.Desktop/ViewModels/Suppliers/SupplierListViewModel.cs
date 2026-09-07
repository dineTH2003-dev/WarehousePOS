using System.Collections.ObjectModel;
using WarehousePOS.Application.Suppliers;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Suppliers;

public sealed class SupplierListViewModel : ViewModelBase
{
    private readonly ISupplierService _supplierService;
    private ObservableCollection<SupplierDto> _suppliers = [];
    private SupplierDto? _selectedSupplier;
    private bool _showInactive;
    private List<SupplierDto> _allSuppliers = [];
    private string _searchText = string.Empty;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public ObservableCollection<SupplierDto> Suppliers
    {
        get => _suppliers;
        private set => SetField(ref _suppliers, value);
    }

    public SupplierDto? SelectedSupplier
    {
        get => _selectedSupplier;
        set => SetField(ref _selectedSupplier, value);
    }

    public bool ShowInactive
    {
        get => _showInactive;
        set { SetField(ref _showInactive, value); _ = LoadAsync(); }
    }

    public event Action<SupplierDto?>? EditRequested;

    public RelayCommand AddCommand     { get; }
    public RelayCommand<SupplierDto> EditCommand          { get; }
    public RelayCommand<SupplierDto> ToggleActiveCommand  { get; }
    public RelayCommand RefreshCommand { get; }

    public SupplierListViewModel(ISupplierService supplierService)
    {
        _supplierService = supplierService;
        AddCommand    = new RelayCommand(() => EditRequested?.Invoke(null));
        EditCommand   = new RelayCommand<SupplierDto>(dto => EditRequested?.Invoke(dto));
        ToggleActiveCommand = new RelayCommand<SupplierDto>(async dto => await ToggleAsync(dto));
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
    }

    public async Task LoadAsync()
    {
        var items = ShowInactive
            ? await _supplierService.GetAllAsync()
            : await _supplierService.GetActiveAsync();
        _allSuppliers = items.ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _allSuppliers.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim().ToLower();
            filtered = filtered.Where(s =>
                (s.Name != null && s.Name.ToLower().Contains(term)) ||
                (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(term)) ||
                (s.Phone != null && s.Phone.ToLower().Contains(term)) ||
                (s.ProvidedProducts != null && s.ProvidedProducts.ToLower().Contains(term)));
        }
        Suppliers = new ObservableCollection<SupplierDto>(filtered);
    }

    private async Task ToggleAsync(SupplierDto? dto)
    {
        if (dto is null) return;
        if (dto.IsActive) await _supplierService.DeactivateAsync(dto.Id);
        else await _supplierService.ActivateAsync(dto.Id);
        await LoadAsync();
    }
}
