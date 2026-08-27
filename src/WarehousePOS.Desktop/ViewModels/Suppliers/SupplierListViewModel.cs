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
        Suppliers = new ObservableCollection<SupplierDto>(items);
    }

    private async Task ToggleAsync(SupplierDto? dto)
    {
        if (dto is null) return;
        if (dto.IsActive) await _supplierService.DeactivateAsync(dto.Id);
        else await _supplierService.ActivateAsync(dto.Id);
        await LoadAsync();
    }
}
