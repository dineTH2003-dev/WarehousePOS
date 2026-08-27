using System.Collections.ObjectModel;
using WarehousePOS.Application.Sales;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Sales;

public sealed class CustomerListViewModel : ViewModelBase
{
    private readonly ICustomerService _customerService;
    private ObservableCollection<CustomerDto> _customers = [];
    private CustomerDto? _selectedCustomer;
    private string _searchText = string.Empty;
    private bool _showInactive;

    public ObservableCollection<CustomerDto> Customers
    {
        get => _customers;
        private set => SetField(ref _customers, value);
    }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetField(ref _selectedCustomer, value);
    }

    public string SearchText
    {
        get => _searchText;
        set { SetField(ref _searchText, value); _ = LoadAsync(); }
    }

    public bool ShowInactive
    {
        get => _showInactive;
        set { SetField(ref _showInactive, value); _ = LoadAsync(); }
    }

    public event Action<CustomerDto?>? EditRequested;

    public RelayCommand AddCommand              { get; }
    public RelayCommand<CustomerDto> EditCommand   { get; }
    public RelayCommand<CustomerDto> ToggleActiveCommand { get; }
    public RelayCommand RefreshCommand          { get; }

    public CustomerListViewModel(ICustomerService customerService)
    {
        _customerService = customerService;
        AddCommand          = new RelayCommand(() => EditRequested?.Invoke(null));
        EditCommand         = new RelayCommand<CustomerDto>(dto => EditRequested?.Invoke(dto));
        ToggleActiveCommand = new RelayCommand<CustomerDto>(async dto => await ToggleAsync(dto));
        RefreshCommand      = new RelayCommand(async () => await LoadAsync());
    }

    public async Task LoadAsync()
    {
        IReadOnlyList<CustomerDto> items;
        if (!string.IsNullOrWhiteSpace(SearchText))
            items = await _customerService.SearchAsync(SearchText);
        else
            items = ShowInactive ? await _customerService.GetAllAsync() : await _customerService.GetActiveAsync();

        Customers = new ObservableCollection<CustomerDto>(items);
    }

    private async Task ToggleAsync(CustomerDto? dto)
    {
        if (dto is null) return;
        if (dto.IsActive) await _customerService.DeactivateAsync(dto.Id);
        else await _customerService.ActivateAsync(dto.Id);
        await LoadAsync();
    }
}
