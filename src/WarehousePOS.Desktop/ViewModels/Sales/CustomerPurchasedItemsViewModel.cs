using System.Collections.ObjectModel;
using WarehousePOS.Application.Sales;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Sales;

public sealed record CustomerPurchasedItemDisplayModel(
    int SaleId,
    string InvoiceNumber,
    string ProductName,
    string SKU,
    int Quantity,
    DateTime PurchaseDate,
    string PurchaseDateFormatted,
    string PurchaseTimeFormatted,
    string WarrantyPeriodText,
    string WarrantyExpiryText,
    string WarrantyStatus,
    string WarrantyStatusColor);

public sealed class CustomerPurchasedItemsViewModel : ViewModelBase
{
    public static CustomerDto? PendingCustomer { get; set; }

    private readonly ISaleService _saleService;
    private CustomerDto? _customer;
    private ObservableCollection<CustomerPurchasedItemDisplayModel> _purchasedItems = [];
    private bool _isBusy;
    private string _errorMessage = string.Empty;

    public CustomerDto? Customer
    {
        get => _customer;
        private set => SetField(ref _customer, value);
    }

    public ObservableCollection<CustomerPurchasedItemDisplayModel> PurchasedItems
    {
        get => _purchasedItems;
        private set => SetField(ref _purchasedItems, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set { SetField(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasNoItems => !IsBusy && PurchasedItems.Count == 0;

    public event Action? BackRequested;

    public RelayCommand BackCommand { get; }

    public CustomerPurchasedItemsViewModel(ISaleService saleService)
    {
        _saleService = saleService;
        BackCommand = new RelayCommand(() => BackRequested?.Invoke());
    }

    public async Task LoadAsync(CustomerDto customer)
    {
        Customer = customer;
        PurchasedItems.Clear();
        ErrorMessage = string.Empty;
        IsBusy = true;
        OnPropertyChanged(nameof(HasNoItems));

        try
        {
            var sales = await _saleService.GetByCustomerAsync(customer.Id);
            var list = new List<CustomerPurchasedItemDisplayModel>();

            foreach (var sale in sales)
            {
                var localSaleDate = sale.SaleDate.ToLocalTime();
                var saleDateStr = localSaleDate.ToString("yyyy-MM-dd");
                var saleTimeStr = localSaleDate.ToString("hh:mm tt");

                foreach (var item in sale.Items)
                {
                    string periodText;
                    string expiryText;
                    string statusText;
                    string statusColor;

                    if (item.WarrantyYears == 0 && item.WarrantyMonths == 0 && item.WarrantyDays == 0)
                    {
                        periodText = "None";
                        expiryText = "N/A";
                        statusText = "No Warranty";
                        statusColor = "#64748B"; // Slate Gray
                    }
                    else
                    {
                        var parts = new List<string>();
                        if (item.WarrantyYears > 0) parts.Add($"{item.WarrantyYears} Yrs");
                        if (item.WarrantyMonths > 0) parts.Add($"{item.WarrantyMonths} Mos");
                        if (item.WarrantyDays > 0) parts.Add($"{item.WarrantyDays} Days");
                        periodText = string.Join(" ", parts);

                        var expiryDate = localSaleDate
                            .AddYears(item.WarrantyYears)
                            .AddMonths(item.WarrantyMonths)
                            .AddDays(item.WarrantyDays);

                        expiryText = expiryDate.ToString("yyyy-MM-dd");

                        if (DateTime.Now <= expiryDate)
                        {
                            statusText = "Active";
                            statusColor = "#16A34A"; // Green
                        }
                        else
                        {
                            statusText = "Expired";
                            statusColor = "#DC2626"; // Red
                        }
                    }

                    list.Add(new CustomerPurchasedItemDisplayModel(
                        sale.Id,
                        $"INV-{sale.Id:D5}",
                        item.ProductName,
                        item.SKU,
                        item.Quantity,
                        localSaleDate,
                        saleDateStr,
                        saleTimeStr,
                        periodText,
                        expiryText,
                        statusText,
                        statusColor));
                }
            }

            PurchasedItems = new ObservableCollection<CustomerPurchasedItemDisplayModel>(
                list.OrderByDescending(x => x.PurchaseDate));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load customer purchases: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(HasNoItems));
        }
    }
}
