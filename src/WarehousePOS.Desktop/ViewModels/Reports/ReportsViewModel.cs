using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using WarehousePOS.Application.Printing;
using WarehousePOS.Application.Products;
using WarehousePOS.Application.Reports;
using WarehousePOS.Application.Sales;
using WarehousePOS.Application.Suppliers;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.Views.Sales;

namespace WarehousePOS.Desktop.ViewModels.Reports;

public sealed class ReportsViewModel : ViewModelBase
{
    private readonly IReportService _reportService;
    private readonly ISupplierService _supplierService;
    private readonly SessionContext _session;
    private readonly ISaleService? _saleService;
    private readonly IProductService? _productService;
    private readonly ICustomerService? _customerService;
    private readonly IReceiptPrinter? _printer;

    private bool _isPrinting;
    private string? _printStatusMessage;

    public bool IsPrinting { get => _isPrinting; private set => SetField(ref _isPrinting, value); }
    public string? PrintStatusMessage { get => _printStatusMessage; private set { SetField(ref _printStatusMessage, value); OnPropertyChanged(nameof(HasPrintStatus)); } }
    public bool HasPrintStatus => !string.IsNullOrWhiteSpace(PrintStatusMessage);
    public RelayCommand PrintCurrentReportCommand { get; }

    private CancellationTokenSource? _cts;

    private int _selectedTabIndex;
    private DateTime _fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _toDate = DateTime.Today;
    private DateRangePreset _selectedPreset = DateRangePreset.ThisMonth;
    private int? _selectedSupplierId;

    private bool _isBusy;
    private string? _errorMessage;
    private bool _isEmpty;

    // Search and Filter State
    private string _inventorySearchText = string.Empty;
    private string _selectedStockFilter = "All"; // All, LowStock, OutOfStock

    private string _grnSearchText = string.Empty;
    private string _selectedGrnStatusFilter = "All"; // All, Paid, Partial, Unpaid

    private string _claimSearchText = string.Empty;
    private string _selectedClaimSourceFilter = "All"; // All, Warranty, CustomerReturn, SupplierReturn, StockAdjustment

    private string _supplierSearchText = string.Empty;

    private string _customerSearchText = string.Empty;
    private string _selectedCustomerTypeFilter = "All"; // All, Retail, Wholesale

    private string _productionSearchText = string.Empty;
    private string _selectedProductionCategoryFilter = "All";

    // Detail Panel Selection State
    private LowStockItemDto? _selectedLowStockItem;
    private GrnRecordDto? _selectedGrnRecord;
    private ClaimRecordDto? _selectedClaimRecord;
    private SupplierBalanceReportDto? _selectedSupplierBalance;
    private CustomerReportDto? _selectedCustomerReport;
    private ProductProductionReportItemDto? _selectedProductionItem;

    // Report DTO Data Properties
    private GeneralAnalyticsDto? _generalAnalytics;
    private DailySalesReportDto? _dailySales;
    private StockValuationReportDto? _stockValuation;
    private SalesSummaryDto? _salesSummary;
    private GrnReportDto? _grnReport;
    private ClaimItemReportDto? _claimReport;
    private SupplierBalanceSummaryDto? _supplierBalanceSummary;
    private CustomerReportSummaryDto? _customerSummary;
    private ProductProductionReportSummaryDto? _productionReportSummary;

    // Raw Collections
    private readonly ObservableCollection<SalesTrendPointDto> _salesTrend = [];
    private readonly ObservableCollection<HourlySalesPointDto> _hourlySales = [];
    private readonly ObservableCollection<LowStockItemDto> _lowStockItems = [];
    private readonly ObservableCollection<FastMovingItemDto> _fastMovingItems = [];
    private readonly ObservableCollection<CategoryPerformanceDto> _categoryPerformance = [];
    private readonly ObservableCollection<GrnRecordDto> _grnRecords = [];
    private readonly ObservableCollection<ClaimRecordDto> _claimRecords = [];
    private readonly ObservableCollection<SupplierBalanceReportDto> _supplierBalances = [];
    private readonly ObservableCollection<CustomerReportDto> _customerReports = [];
    private readonly ObservableCollection<SupplierDto> _suppliersList = [];
    private readonly ObservableCollection<string> _businessInsights = [];
    private readonly ObservableCollection<ProductProductionReportItemDto> _productionItems = [];
    private readonly ObservableCollection<ProductProductionReportItemDto> _filteredProductionItems = [];
    private readonly ObservableCollection<string> _productionCategoryList = [];

    // Filtered Collections for Views
    private readonly ObservableCollection<LowStockItemDto> _filteredLowStockItems = [];
    private readonly ObservableCollection<GrnRecordDto> _filteredGrnRecords = [];
    private readonly ObservableCollection<ClaimRecordDto> _filteredClaimRecords = [];
    private readonly ObservableCollection<SupplierBalanceReportDto> _filteredSupplierBalances = [];
    private readonly ObservableCollection<CustomerReportDto> _filteredCustomerReports = [];

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetField(ref _selectedTabIndex, value))
                _ = LoadActiveTabReportAsync();
        }
    }

    public DateTime Today => DateTime.Today;

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            var val = value.Date > DateTime.Today ? DateTime.Today : value;
            if (SetField(ref _fromDate, val))
            {
                if (_selectedPreset != DateRangePreset.Custom)
                    SetField(ref _selectedPreset, DateRangePreset.Custom, nameof(SelectedPreset));
                ValidateDates();
                OnPropertyChanged(nameof(ActiveFilterSummaryText));
                OnPropertyChanged(nameof(IsValidDateRange));
            }
        }
    }

    public DateTime ToDate
    {
        get => _toDate;
        set
        {
            var val = value.Date > DateTime.Today ? DateTime.Today : value;
            if (SetField(ref _toDate, val))
            {
                if (_selectedPreset != DateRangePreset.Custom)
                    SetField(ref _selectedPreset, DateRangePreset.Custom, nameof(SelectedPreset));
                ValidateDates();
                OnPropertyChanged(nameof(ActiveFilterSummaryText));
                OnPropertyChanged(nameof(IsValidDateRange));
            }
        }
    }

    public IEnumerable<DateRangePreset> PresetOptions => Enum.GetValues<DateRangePreset>();

    public DateRangePreset SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (SetField(ref _selectedPreset, value))
            {
                ApplyPresetDates(value);
                _ = LoadAllReportsAsync();
            }
        }
    }

    public int? SelectedSupplierId
    {
        get => _selectedSupplierId;
        set
        {
            if (SetField(ref _selectedSupplierId, value))
            {
                if (SelectedTabIndex == 3) // GRN tab
                    _ = LoadGrnReportAsync();
            }
        }
    }

    public bool IsValidDateRange => FromDate <= ToDate;

    public string ActiveFilterSummaryText =>
        FromDate > ToDate
            ? "⚠️ From Date cannot be later than To Date."
            : FromDate.Date == ToDate.Date
                ? $"Showing: {FromDate:MMM d, yyyy}"
                : $"Showing: {FromDate:MMM d, yyyy} → {ToDate:MMM d, yyyy}";

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetField(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetField(ref _isEmpty, value);
    }

    // Search and Filter Properties
    public string InventorySearchText
    {
        get => _inventorySearchText;
        set
        {
            if (SetField(ref _inventorySearchText, value))
                ApplyInventoryFilters();
        }
    }

    public string SelectedStockFilter
    {
        get => _selectedStockFilter;
        set
        {
            if (SetField(ref _selectedStockFilter, value))
            {
                ApplyInventoryFilters();
                OnPropertyChanged(nameof(IsAllStockFilterSelected));
                OnPropertyChanged(nameof(IsLowStockFilterSelected));
                OnPropertyChanged(nameof(IsOutOfStockFilterSelected));
            }
        }
    }

    public bool IsAllStockFilterSelected => SelectedStockFilter == "All";
    public bool IsLowStockFilterSelected => SelectedStockFilter == "LowStock";
    public bool IsOutOfStockFilterSelected => SelectedStockFilter == "OutOfStock";

    public string GrnSearchText
    {
        get => _grnSearchText;
        set
        {
            if (SetField(ref _grnSearchText, value))
                ApplyGrnFilters();
        }
    }

    public string SelectedGrnStatusFilter
    {
        get => _selectedGrnStatusFilter;
        set
        {
            if (SetField(ref _selectedGrnStatusFilter, value))
                ApplyGrnFilters();
        }
    }

    public string ClaimSearchText
    {
        get => _claimSearchText;
        set
        {
            if (SetField(ref _claimSearchText, value))
                ApplyClaimFilters();
        }
    }

    public string SelectedClaimSourceFilter
    {
        get => _selectedClaimSourceFilter;
        set
        {
            if (SetField(ref _selectedClaimSourceFilter, value))
                ApplyClaimFilters();
        }
    }

    public string SupplierSearchText
    {
        get => _supplierSearchText;
        set
        {
            if (SetField(ref _supplierSearchText, value))
                ApplySupplierFilters();
        }
    }

    public string CustomerSearchText
    {
        get => _customerSearchText;
        set
        {
            if (SetField(ref _customerSearchText, value))
                ApplyCustomerFilters();
        }
    }

    public string SelectedCustomerTypeFilter
    {
        get => _selectedCustomerTypeFilter;
        set
        {
            if (SetField(ref _selectedCustomerTypeFilter, value))
                ApplyCustomerFilters();
        }
    }

    public string ProductionSearchText
    {
        get => _productionSearchText;
        set
        {
            if (SetField(ref _productionSearchText, value))
                ApplyProductionFilters();
        }
    }

    public string SelectedProductionCategoryFilter
    {
        get => _selectedProductionCategoryFilter;
        set
        {
            if (SetField(ref _selectedProductionCategoryFilter, value))
                ApplyProductionFilters();
        }
    }

    // Detail Panel Selection Properties
    public LowStockItemDto? SelectedLowStockItem
    {
        get => _selectedLowStockItem;
        set => SetField(ref _selectedLowStockItem, value);
    }

    public GrnRecordDto? SelectedGrnRecord
    {
        get => _selectedGrnRecord;
        set => SetField(ref _selectedGrnRecord, value);
    }

    public ClaimRecordDto? SelectedClaimRecord
    {
        get => _selectedClaimRecord;
        set => SetField(ref _selectedClaimRecord, value);
    }

    public SupplierBalanceReportDto? SelectedSupplierBalance
    {
        get => _selectedSupplierBalance;
        set => SetField(ref _selectedSupplierBalance, value);
    }

    public CustomerReportDto? SelectedCustomerReport
    {
        get => _selectedCustomerReport;
        set => SetField(ref _selectedCustomerReport, value);
    }

    public ProductProductionReportItemDto? SelectedProductionItem
    {
        get => _selectedProductionItem;
        set => SetField(ref _selectedProductionItem, value);
    }

    // Reports Bindings
    public GeneralAnalyticsDto? GeneralAnalytics
    {
        get => _generalAnalytics;
        private set
        {
            if (SetField(ref _generalAnalytics, value))
            {
                OnPropertyChanged(nameof(CashPaymentPercentage));
                OnPropertyChanged(nameof(CardPaymentPercentage));
                OnPropertyChanged(nameof(BankPaymentPercentage));
                OnPropertyChanged(nameof(ChequePaymentPercentage));
                OnPropertyChanged(nameof(CreditSalesPercentage));
                OnPropertyChanged(nameof(RetailSalesPercentage));
                OnPropertyChanged(nameof(WholesaleSalesPercentage));
            }
        }
    }

    // Computed Analytics Percentages for UI Visualization
    public decimal CashPaymentPercentage => GeneralAnalytics?.NetRevenue > 0 ? Math.Round((GeneralAnalytics.CashPaymentTotal / GeneralAnalytics.NetRevenue) * 100m, 1) : 0m;
    public decimal CardPaymentPercentage => GeneralAnalytics?.NetRevenue > 0 ? Math.Round((GeneralAnalytics.CardPaymentTotal / GeneralAnalytics.NetRevenue) * 100m, 1) : 0m;
    public decimal BankPaymentPercentage => GeneralAnalytics?.NetRevenue > 0 ? Math.Round((GeneralAnalytics.BankPaymentTotal / GeneralAnalytics.NetRevenue) * 100m, 1) : 0m;
    public decimal ChequePaymentPercentage => GeneralAnalytics?.NetRevenue > 0 ? Math.Round((GeneralAnalytics.ChequePaymentTotal / GeneralAnalytics.NetRevenue) * 100m, 1) : 0m;
    public decimal CreditSalesPercentage => GeneralAnalytics?.NetRevenue > 0 ? Math.Round((GeneralAnalytics.CreditSalesTotal / GeneralAnalytics.NetRevenue) * 100m, 1) : 0m;

    public decimal RetailSalesPercentage => GeneralAnalytics?.NetRevenue > 0 ? Math.Round((GeneralAnalytics.RetailSalesRevenue / GeneralAnalytics.NetRevenue) * 100m, 1) : 0m;
    public decimal WholesaleSalesPercentage => GeneralAnalytics?.NetRevenue > 0 ? Math.Round((GeneralAnalytics.WholesaleSalesRevenue / GeneralAnalytics.NetRevenue) * 100m, 1) : 0m;


    public DailySalesReportDto? DailySales
    {
        get => _dailySales;
        private set => SetField(ref _dailySales, value);
    }

    public StockValuationReportDto? StockValuation
    {
        get => _stockValuation;
        private set => SetField(ref _stockValuation, value);
    }

    public SalesSummaryDto? SalesSummary
    {
        get => _salesSummary;
        private set => SetField(ref _salesSummary, value);
    }

    public GrnReportDto? GrnReport
    {
        get => _grnReport;
        private set => SetField(ref _grnReport, value);
    }

    public ClaimItemReportDto? ClaimReport
    {
        get => _claimReport;
        private set => SetField(ref _claimReport, value);
    }

    public SupplierBalanceSummaryDto? SupplierBalanceSummary
    {
        get => _supplierBalanceSummary;
        private set => SetField(ref _supplierBalanceSummary, value);
    }

    public CustomerReportSummaryDto? CustomerSummary
    {
        get => _customerSummary;
        private set => SetField(ref _customerSummary, value);
    }

    public ProductProductionReportSummaryDto? ProductionReportSummary
    {
        get => _productionReportSummary;
        private set => SetField(ref _productionReportSummary, value);
    }

    // Collections
    public ObservableCollection<SalesTrendPointDto> SalesTrend => _salesTrend;
    public ObservableCollection<HourlySalesPointDto> HourlySales => _hourlySales;
    public ObservableCollection<LowStockItemDto> LowStockItems => _lowStockItems;
    public ObservableCollection<FastMovingItemDto> FastMovingItems => _fastMovingItems;
    public ObservableCollection<CategoryPerformanceDto> CategoryPerformance => _categoryPerformance;
    public ObservableCollection<GrnRecordDto> GrnRecords => _grnRecords;
    public ObservableCollection<ClaimRecordDto> ClaimRecords => _claimRecords;
    public ObservableCollection<SupplierBalanceReportDto> SupplierBalances => _supplierBalances;
    public ObservableCollection<CustomerReportDto> CustomerReports => _customerReports;
    public ObservableCollection<SupplierDto> SuppliersList => _suppliersList;
    public ObservableCollection<string> BusinessInsights => _businessInsights;

    // Filtered Collections
    private readonly ObservableCollection<ProductClaimSliceDto> _productClaimDistribution = [];

    public ObservableCollection<LowStockItemDto> FilteredLowStockItems => _filteredLowStockItems;
    public ObservableCollection<GrnRecordDto> FilteredGrnRecords => _filteredGrnRecords;
    public ObservableCollection<ClaimRecordDto> FilteredClaimRecords => _filteredClaimRecords;
    public ObservableCollection<SupplierBalanceReportDto> FilteredSupplierBalances => _filteredSupplierBalances;
    public ObservableCollection<CustomerReportDto> FilteredCustomerReports => _filteredCustomerReports;
    public ObservableCollection<ProductProductionReportItemDto> ProductionItems => _productionItems;
    public ObservableCollection<ProductProductionReportItemDto> FilteredProductionItems => _filteredProductionItems;
    public ObservableCollection<string> ProductionCategoryList => _productionCategoryList;
    public ObservableCollection<ProductClaimSliceDto> ProductClaimDistribution => _productClaimDistribution;

    // Chart Maximum Scales
    public decimal MaxSalesTrendRevenue => _salesTrend.Count > 0 ? Math.Max(1m, _salesTrend.Max(x => x.Revenue)) : 1m;
    public decimal MaxHourlyRevenue => _hourlySales.Count > 0 ? Math.Max(1m, _hourlySales.Max(x => x.Revenue)) : 1m;
    public decimal MaxCategoryRevenue => _categoryPerformance.Count > 0 ? Math.Max(1m, _categoryPerformance.Max(x => x.Revenue)) : 1m;
    public decimal MaxSupplierPayable => _supplierBalances.Count > 0 ? Math.Max(1m, _supplierBalances.Max(x => x.CurrentBalance)) : 1m;
    public decimal MaxCustomerSpend => _customerReports.Count > 0 ? Math.Max(1m, _customerReports.Max(x => x.TotalSpent)) : 1m;

    // Commands
    public RelayCommand ApplyFilterCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand<object> NavigateToTabCommand { get; }
    public RelayCommand<string> SetStockFilterCommand { get; }
    public RelayCommand ClearLowStockSelectionCommand { get; }
    public RelayCommand ClearGrnSelectionCommand { get; }
    public RelayCommand ClearClaimSelectionCommand { get; }
    public RelayCommand<ClaimRecordDto> SelectClaimCommand { get; }
    public RelayCommand ClearSupplierSelectionCommand { get; }
    public RelayCommand ClearCustomerSelectionCommand { get; }
    public RelayCommand ClearProductionSelectionCommand { get; }
    public RelayCommand<object> ProcessCustomerClaimCommand { get; }

    public ReportsViewModel(
        IReportService reportService,
        ISupplierService supplierService,
        SessionContext session,
        ISaleService? saleService = null,
        IProductService? productService = null,
        ICustomerService? customerService = null,
        IReceiptPrinter? printer = null)
    {
        if (!session.IsAdmin)
            throw new UnauthorizedAccessException("Only an Admin can access Reports.");

        _reportService = reportService;
        _supplierService = supplierService;
        _session = session;
        _saleService = saleService;
        _productService = productService;
        _customerService = customerService;
        _printer = printer;

        ApplyFilterCommand = new RelayCommand(async () => await LoadAllReportsAsync(), () => IsValidDateRange);
        RefreshCommand = new RelayCommand(async () => await LoadAllReportsAsync(), () => IsValidDateRange);
        NavigateToTabCommand = new RelayCommand<object>(param =>
        {
            if (param != null && int.TryParse(param.ToString(), out var tabIndex))
                SelectedTabIndex = tabIndex;
        });
        SetStockFilterCommand = new RelayCommand<string>(filter => SelectedStockFilter = filter ?? "All");

        ClearLowStockSelectionCommand = new RelayCommand(() => SelectedLowStockItem = null);
        ClearGrnSelectionCommand = new RelayCommand(() => SelectedGrnRecord = null);
        ClearClaimSelectionCommand = new RelayCommand(() => SelectedClaimRecord = null);
        SelectClaimCommand = new RelayCommand<ClaimRecordDto>(claim => SelectedClaimRecord = claim);
        ClearSupplierSelectionCommand = new RelayCommand(() => SelectedSupplierBalance = null);
        ClearCustomerSelectionCommand = new RelayCommand(() => SelectedCustomerReport = null);
        ClearProductionSelectionCommand = new RelayCommand(() => SelectedProductionItem = null);
        ProcessCustomerClaimCommand = new RelayCommand<object>(async param => await ExecuteProcessCustomerClaimAsync(param as CustomerReportDto));
        PrintCurrentReportCommand = new RelayCommand(async () => await PrintCurrentReportAsync(), () => !IsPrinting && _printer is not null);

        _ = InitialiseAsync();
    }

    private async Task InitialiseAsync()
    {
        try
        {
            var suppliers = await _supplierService.GetAllAsync();
            _suppliersList.Clear();
            foreach (var s in suppliers) _suppliersList.Add(s);
        }
        catch { /* soft handling */ }

        await LoadAllReportsAsync();
    }

    public bool ValidateDates()
    {
        if (FromDate.Date > DateTime.Today || ToDate.Date > DateTime.Today)
        {
            ErrorMessage = "Date cannot be in the future.";
            return false;
        }

        if (FromDate > ToDate)
        {
            ErrorMessage = "From Date cannot be later than To Date.";
            return false;
        }

        ErrorMessage = null;
        return true;
    }

    private void ApplyPresetDates(DateRangePreset preset)
    {
        var today = DateTime.Today;
        switch (preset)
        {
            case DateRangePreset.Today:
                _fromDate = today;
                _toDate = today;
                break;
            case DateRangePreset.Yesterday:
                _fromDate = today.AddDays(-1);
                _toDate = today.AddDays(-1);
                break;
            case DateRangePreset.Last7Days:
                _fromDate = today.AddDays(-6);
                _toDate = today;
                break;
            case DateRangePreset.Last30Days:
                _fromDate = today.AddDays(-29);
                _toDate = today;
                break;
            case DateRangePreset.ThisMonth:
                _fromDate = new DateTime(today.Year, today.Month, 1);
                _toDate = today;
                break;
            case DateRangePreset.AllTime:
                _fromDate = new DateTime(2000, 1, 1);
                _toDate = today;
                break;
        }
        ValidateDates();
        OnPropertyChanged(nameof(FromDate));
        OnPropertyChanged(nameof(ToDate));
        OnPropertyChanged(nameof(ActiveFilterSummaryText));
        OnPropertyChanged(nameof(IsValidDateRange));
    }

    public async Task LoadAllReportsAsync()
    {
        EnsureAdmin();

        if (!ValidateDates())
            return;

        ErrorMessage = null;
        IsBusy = true;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            await LoadActiveTabReportAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Expected on fast tab/filter switches
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to load report metrics: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadActiveTabReportAsync(CancellationToken ct = default)
    {
        EnsureAdmin();

        if (!ValidateDates())
            return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            switch (SelectedTabIndex)
            {
                case 0: // Sales Summary Overview (Executive Dashboard)
                    GeneralAnalytics = await _reportService.GetGeneralAnalyticsAsync(FromDate, ToDate, ct);
                    SalesSummary = await _reportService.GetSalesSummaryAsync(FromDate, ToDate, ct);
                    if (SalesSummary != null)
                    {
                        _categoryPerformance.Clear();
                        foreach (var c in SalesSummary.CategoryPerformance) _categoryPerformance.Add(c);
                        _fastMovingItems.Clear();
                        foreach (var fm in SalesSummary.TopProducts) _fastMovingItems.Add(fm);
                        OnPropertyChanged(nameof(MaxCategoryRevenue));
                    }
                    var trend = await _reportService.GetSalesTrendAsync(FromDate, ToDate, ct);
                    _salesTrend.Clear();
                    foreach (var t in trend) _salesTrend.Add(t);
                    OnPropertyChanged(nameof(MaxSalesTrendRevenue));
                    GenerateBusinessInsights();
                    break;

                case 1: // Daily Sales
                    DailySales = await _reportService.GetDailySalesReportAsync(FromDate, ToDate, ct);
                    var trendPoints = await _reportService.GetSalesTrendAsync(FromDate, ToDate, ct);
                    _salesTrend.Clear();
                    foreach (var t in trendPoints.OrderByDescending(x => x.Date)) _salesTrend.Add(t);
                    var hourly = await _reportService.GetHourlySalesAsync(ToDate, ct);
                    _hourlySales.Clear();
                    foreach (var h in hourly) _hourlySales.Add(h);
                    OnPropertyChanged(nameof(MaxHourlyRevenue));
                    break;

                case 2: // Inventory & Stock
                    StockValuation = await _reportService.GetStockValuationReportAsync(ct);
                    var allInventory = await _reportService.GetAllInventoryItemsAsync(ct);
                    _lowStockItems.Clear();
                    foreach (var item in allInventory) _lowStockItems.Add(item);
                    ApplyInventoryFilters();
                    var fastMoving = await _reportService.GetFastMovingItemsAsync(FromDate, ToDate, 10, ct);
                    if (fastMoving.Count == 0)
                        fastMoving = await _reportService.GetFastMovingItemsAsync(10, ct);
                    _fastMovingItems.Clear();
                    foreach (var fm in fastMoving) _fastMovingItems.Add(fm);
                    break;

                case 3: // GRN Reports
                    await LoadGrnReportAsync(ct);
                    break;

                case 4: // Claim Items
                    ClaimReport = await _reportService.GetClaimItemsReportAsync(FromDate, ToDate, ct);
                    _claimRecords.Clear();
                    if (ClaimReport != null)
                    {
                        foreach (var cr in ClaimReport.Items) _claimRecords.Add(cr);
                    }
                    ApplyClaimFilters();
                    break;

                case 5: // Supplier Balances
                    SupplierBalanceSummary = await _reportService.GetSupplierBalanceReportSummaryAsync(FromDate, ToDate, ct);
                    _supplierBalances.Clear();
                    if (SupplierBalanceSummary != null)
                    {
                        foreach (var sb in SupplierBalanceSummary.Suppliers) _supplierBalances.Add(sb);
                    }
                    ApplySupplierFilters();
                    OnPropertyChanged(nameof(MaxSupplierPayable));
                    break;

                case 6: // Customer Insights
                    CustomerSummary = await _reportService.GetCustomerReportSummaryAsync(FromDate, ToDate, ct);
                    _customerReports.Clear();
                    if (CustomerSummary != null)
                    {
                        foreach (var cr in CustomerSummary.Customers) _customerReports.Add(cr);
                    }
                    ApplyCustomerFilters();
                    OnPropertyChanged(nameof(MaxCustomerSpend));
                    break;

                case 7: // Production & Product Profitability
                    ProductionReportSummary = await _reportService.GetProductProductionReportAsync(FromDate, ToDate, null, ct);
                    _productionItems.Clear();
                    _productionCategoryList.Clear();
                    _productionCategoryList.Add("All");
                    if (ProductionReportSummary != null)
                    {
                        foreach (var item in ProductionReportSummary.Items)
                        {
                            _productionItems.Add(item);
                            if (!_productionCategoryList.Contains(item.CategoryName))
                                _productionCategoryList.Add(item.CategoryName);
                        }
                    }
                    ApplyProductionFilters();
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to load report: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadGrnReportAsync(CancellationToken ct = default)
    {
        if (!ValidateDates()) return;

        GrnReport = await _reportService.GetGrnReportAsync(FromDate, ToDate, SelectedSupplierId, ct);
        _grnRecords.Clear();
        if (GrnReport != null)
        {
            foreach (var g in GrnReport.Items) _grnRecords.Add(g);
        }
        ApplyGrnFilters();
    }

    // Filter Logic Methods
    private void ApplyInventoryFilters()
    {
        _filteredLowStockItems.Clear();
        var query = _lowStockItems.AsEnumerable();

        if (SelectedStockFilter == "LowStock")
            query = query.Where(x => x.CurrentStock > 0 && x.CurrentStock <= x.ReorderLevel);
        else if (SelectedStockFilter == "OutOfStock")
            query = query.Where(x => x.CurrentStock <= 0);

        if (!string.IsNullOrWhiteSpace(InventorySearchText))
        {
            var search = InventorySearchText.Trim();
            query = query.Where(x =>
                x.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.SKU.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.CategoryName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
            _filteredLowStockItems.Add(item);
    }

    private void ApplyGrnFilters()
    {
        _filteredGrnRecords.Clear();
        var query = _grnRecords.AsEnumerable();

        if (SelectedGrnStatusFilter == "Paid")
            query = query.Where(x => x.RemainingBalance <= 0);
        else if (SelectedGrnStatusFilter == "Unpaid")
            query = query.Where(x => x.PaidAmount == 0 && x.RemainingBalance > 0);
        else if (SelectedGrnStatusFilter == "Partial")
            query = query.Where(x => x.PaidAmount > 0 && x.RemainingBalance > 0);

        if (!string.IsNullOrWhiteSpace(GrnSearchText))
        {
            var search = GrnSearchText.Trim();
            query = query.Where(x =>
                x.PurchaseId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.SupplierName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
            _filteredGrnRecords.Add(item);
    }

    private void ApplyClaimFilters()
    {
        _filteredClaimRecords.Clear();
        var query = _claimRecords.AsEnumerable();

        if (SelectedClaimSourceFilter != "All")
        {
            query = query.Where(x => x.ClaimSource.Equals(SelectedClaimSourceFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(ClaimSearchText))
        {
            var search = ClaimSearchText.Trim();
            query = query.Where(x =>
                x.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.SKU.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (x.ReferenceNo != null && x.ReferenceNo.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var item in query)
            _filteredClaimRecords.Add(item);

        BuildProductClaimDistribution();
    }

    private void BuildProductClaimDistribution()
    {
        _productClaimDistribution.Clear();
        if (_claimRecords.Count == 0) return;

        var palette = new[] { "#D97706", "#2563EB", "#059669", "#7C3AED", "#DC2626", "#0284C7", "#64748B" };

        var grouped = _claimRecords
            .GroupBy(x => x.ProductName)
            .Select(g => new
            {
                ProductName = g.Key,
                SKU = g.First().SKU,
                Quantity = g.Sum(x => x.Quantity),
                TotalValue = g.Sum(x => x.TotalValue)
            })
            .OrderByDescending(x => x.Quantity)
            .ToList();

        int totalQty = grouped.Sum(x => x.Quantity);
        if (totalQty <= 0) return;

        double currentAngle = 0;
        int colorIndex = 0;

        foreach (var g in grouped)
        {
            double pct = (double)g.Quantity / totalQty;
            double sweep = pct * 360.0;
            double startAngle = currentAngle;
            double endAngle = currentAngle + sweep;
            currentAngle = endAngle;

            string color = palette[colorIndex % palette.Length];
            colorIndex++;

            var geom = CreatePieSliceGeometry(startAngle, endAngle);

            _productClaimDistribution.Add(new ProductClaimSliceDto(
                g.ProductName,
                g.SKU,
                g.Quantity,
                g.TotalValue,
                Math.Round(pct * 100.0, 1),
                startAngle,
                endAngle,
                color,
                geom));
        }
    }

    private static Geometry CreatePieSliceGeometry(double startAngleDegrees, double endAngleDegrees, double radius = 68, Point center = default)
    {
        if (center == default) center = new Point(77.5, 77.5);

        double sweepAngle = endAngleDegrees - startAngleDegrees;
        if (sweepAngle >= 360 || sweepAngle <= -360)
        {
            var fullCircle = new EllipseGeometry(center, radius, radius);
            fullCircle.Freeze();
            return fullCircle;
        }

        double startRad = (startAngleDegrees - 90) * Math.PI / 180.0;
        double endRad = (endAngleDegrees - 90) * Math.PI / 180.0;

        Point p1 = new Point(center.X + radius * Math.Cos(startRad), center.Y + radius * Math.Sin(startRad));
        Point p2 = new Point(center.X + radius * Math.Cos(endRad), center.Y + radius * Math.Sin(endRad));

        bool isLargeArc = sweepAngle > 180.0;

        var figure = new PathFigure
        {
            StartPoint = center,
            IsClosed = true,
            IsFilled = true
        };
        figure.Segments.Add(new LineSegment(p1, true));
        figure.Segments.Add(new ArcSegment(p2, new Size(radius, radius), 0, isLargeArc, SweepDirection.Clockwise, true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        geometry.Freeze();
        return geometry;
    }

    private void ApplySupplierFilters()
    {
        _filteredSupplierBalances.Clear();
        var query = _supplierBalances.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SupplierSearchText))
        {
            var search = SupplierSearchText.Trim();
            query = query.Where(x =>
                x.SupplierName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (x.ContactPerson != null && x.ContactPerson.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (x.Phone != null && x.Phone.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var item in query)
            _filteredSupplierBalances.Add(item);
    }

    private void ApplyCustomerFilters()
    {
        _filteredCustomerReports.Clear();
        var query = _customerReports.AsEnumerable();

        if (SelectedCustomerTypeFilter != "All")
        {
            query = query.Where(x => x.CustomerType.Equals(SelectedCustomerTypeFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(CustomerSearchText))
        {
            var search = CustomerSearchText.Trim();
            query = query.Where(x =>
                x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (x.Phone != null && x.Phone.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var item in query)
            _filteredCustomerReports.Add(item);
    }

    private void ApplyProductionFilters()
    {
        _filteredProductionItems.Clear();
        var query = _productionItems.AsEnumerable();

        if (SelectedProductionCategoryFilter != "All" && !string.IsNullOrWhiteSpace(SelectedProductionCategoryFilter))
        {
            query = query.Where(x => x.CategoryName.Equals(SelectedProductionCategoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(ProductionSearchText))
        {
            var search = ProductionSearchText.Trim();
            query = query.Where(x =>
                x.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.SKU.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
            _filteredProductionItems.Add(item);
    }

    private void GenerateBusinessInsights()
    {
        _businessInsights.Clear();
        if (GeneralAnalytics == null) return;

        if (GeneralAnalytics.TotalRevenue > 0)
        {
            var retailShare = (GeneralAnalytics.RetailSalesRevenue / GeneralAnalytics.TotalRevenue) * 100m;
            if (retailShare >= 50)
                _businessInsights.Add($"✓ Retail sales generated {retailShare:F1}% of overall revenue ({GeneralAnalytics.RetailSalesCount} transactions).");
            else
                _businessInsights.Add($"✓ Wholesale sales generated {(100m - retailShare):F1}% of overall revenue ({GeneralAnalytics.WholesaleSalesCount} transactions).");
        }

        if (GeneralAnalytics.NetProfit != 0)
        {
            var profitMargin = GeneralAnalytics.TotalRevenue > 0 ? (GeneralAnalytics.NetProfit / GeneralAnalytics.TotalRevenue) * 100m : 0m;
            if (GeneralAnalytics.NetProfit > 0)
                _businessInsights.Add($"📈 Net Profit margin stands at {profitMargin:F1}% (Rs. {GeneralAnalytics.NetProfit:N2}) for the selected period.");
            else
                _businessInsights.Add($"📉 Period operates at a net margin deficit of {Math.Abs(profitMargin):F1}% (Rs. {GeneralAnalytics.NetProfit:N2}).");
        }

        if (!string.IsNullOrEmpty(GeneralAnalytics.TopCategoryName) && GeneralAnalytics.TopCategoryName != "N/A")
        {
            _businessInsights.Add($"🏆 Top performing category is '{GeneralAnalytics.TopCategoryName}' generating Rs. {GeneralAnalytics.TopCategoryRevenue:N2}.");
        }

        if (!string.IsNullOrEmpty(GeneralAnalytics.TopProductName) && GeneralAnalytics.TopProductName != "N/A")
        {
            _businessInsights.Add($"⭐ Highest moving item is '{GeneralAnalytics.TopProductName}' with {GeneralAnalytics.TopProductQty} units sold.");
        }

        if (GeneralAnalytics.CreditSalesTotal > 0)
        {
            _businessInsights.Add($"💳 Unpaid customer credit issued in period totals Rs. {GeneralAnalytics.CreditSalesTotal:N2}.");
        }

        if (GeneralAnalytics.TotalCustomerOutstanding > 0)
        {
            _businessInsights.Add($"👥 Total outstanding customer debt balance across all accounts is Rs. {GeneralAnalytics.TotalCustomerOutstanding:N2}.");
        }

        if (StockValuation != null && StockValuation.LowStockCount > 0)
        {
            _businessInsights.Add($"⚠️ {StockValuation.LowStockCount} products are approaching or below reorder level.");
        }
    }

    private void EnsureAdmin()
    {
        if (!_session.IsAdmin)
            throw new UnauthorizedAccessException("Only an Admin can access Reports.");
    }

    private async Task ExecuteProcessCustomerClaimAsync(CustomerReportDto? preselectedCustomer = null)
    {
        EnsureAdmin();

        try
        {
            var products = _productService != null ? await _productService.GetAllAsync() : [];
            var customers = _customerService != null ? await _customerService.GetActiveAsync() : [];

            CustomerDto? targetCustomer = null;
            if (preselectedCustomer != null && customers.Count > 0)
            {
                targetCustomer = customers.FirstOrDefault(c => c.Id == preselectedCustomer.CustomerId);
            }

            var activeWindow = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                               ?? System.Windows.Application.Current?.MainWindow;

            var dialog = new ProcessCustomerClaimDialog(products, customers, targetCustomer);
            if (activeWindow != null)
            {
                dialog.Owner = activeWindow;
            }

            if (dialog.ShowDialog() == true && dialog.Request != null && _saleService != null)
            {
                IsBusy = true;
                await _saleService.ProcessCustomerClaimAsync(dialog.Request);

                PrintStatusMessage = $"✅ Customer claim for Rs. {dialog.Request.ClaimAmount:N2} processed successfully!";
                await LoadAllReportsAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to process customer claim: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task PrintCurrentReportAsync()
    {
        if (_printer is null || IsPrinting) return;
        IsPrinting = true;
        PrintStatusMessage = "🖨️ Sending report to Epson LQ-310...";
        try
        {
            var (title, content) = BuildActiveReportText();
            await _printer.PrintReportAsync(title, content);
            PrintStatusMessage = $"✅ Report '{title}' printed successfully!";
        }
        catch (Exception ex)
        {
            PrintStatusMessage = $"❌ Print error: {ex.Message}";
        }
        finally
        {
            IsPrinting = false;
        }
    }

    private (string Title, string Content) BuildActiveReportText()
    {
        var sb = new System.Text.StringBuilder();

        switch (SelectedTabIndex)
        {
            case 1: // Daily Sales
                if (_dailySales is not null)
                {
                    sb.AppendLine($"Date                     : {Today:yyyy-MM-dd}");
                    sb.AppendLine($"Gross Sales Revenue      : Rs. {_dailySales.TotalRevenue:N2}");
                    sb.AppendLine($"Total Invoices Processed : {_dailySales.TotalSalesCount}");
                    sb.AppendLine($"Discounts Granted        : Rs. {_dailySales.TotalDiscounts:N2}");
                    sb.AppendLine($"Net Revenue (after disc) : Rs. {_dailySales.NetSales:N2}");
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    sb.AppendLine("TENDER / PAYMENT BREAKDOWN:");
                    sb.AppendLine($"Cash Collections         : Rs. {_dailySales.CashSales:N2}");
                    sb.AppendLine($"Bank Transfers           : Rs. {_dailySales.BankSales:N2}");
                    sb.AppendLine($"Retail Sales             : Rs. {_dailySales.RetailSales:N2}");
                    sb.AppendLine($"Wholesale Sales          : Rs. {_dailySales.WholesaleSales:N2}");
                    return ("Daily Sales Report", sb.ToString());
                }
                break;

            case 2: // Stock Valuation & Low Stock
                if (_stockValuation is not null)
                {
                    sb.AppendLine($"Stock Valuation at Cost  : Rs. {_stockValuation.TotalCostValue:N2}");
                    sb.AppendLine($"Stock Valuation at Retail: Rs. {_stockValuation.TotalRetailValuation:N2}");
                    sb.AppendLine($"Projected Gross Profit % : {_stockValuation.PotentialProfitMargin:N2}%");
                    sb.AppendLine($"Low Stock Items          : {_stockValuation.LowStockCount}");
                    sb.AppendLine($"Out of Stock Items       : {_stockValuation.OutOfStockCount}");
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    sb.AppendLine("LOW STOCK REORDER WARNINGS:");
                    sb.AppendLine(string.Format("{0,-12} {1,-36} {2,10} {3,10}", "SKU", "Product Name", "In Stock", "Reorder"));
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    foreach (var item in _lowStockItems.Take(30))
                    {
                        string name = item.ProductName.Length > 36 ? item.ProductName[..36] : item.ProductName;
                        sb.AppendLine(string.Format("{0,-12} {1,-36} {2,10} {3,10}", item.SKU, name, item.CurrentStock, item.ReorderLevel));
                    }
                    return ("Stock Valuation & Low Stock Report", sb.ToString());
                }
                break;

            case 3: // Sales Summary
                if (_salesSummary is not null)
                {
                    sb.AppendLine($"Reporting Period         : {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd}");
                    sb.AppendLine($"Total Sales Turnover     : Rs. {_salesSummary.TotalRevenue:N2}");
                    sb.AppendLine($"Total Units Sold         : {_salesSummary.TotalUnitsSold}");
                    sb.AppendLine($"Total Transactions Count : {_salesSummary.TotalTransactions}");
                    sb.AppendLine($"Average Invoice Value    : Rs. {_salesSummary.AverageOrderValue:N2}");
                    return ("Sales Summary Report", sb.ToString());
                }
                break;

            case 4: // GRN Purchases
                sb.AppendLine($"Reporting Period         : {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd}");
                sb.AppendLine(string.Format("{0,-10} {1,-28} {2,16} {3,16}", "GRN #", "Supplier", "Total Amount", "Status"));
                sb.AppendLine("--------------------------------------------------------------------------------");
                foreach (var g in _grnRecords.Take(30))
                {
                    string sup = g.SupplierName.Length > 28 ? g.SupplierName[..28] : g.SupplierName;
                    sb.AppendLine(string.Format("{0,-10} {1,-28} {2,16:N2} {3,16}", g.PurchaseId, sup, g.TotalAmount, g.Status));
                }
                return ("Goods Received Notes (GRN) Report", sb.ToString());

            case 5: // Supplier Balances
                if (_supplierBalanceSummary is not null)
                {
                    sb.AppendLine($"Total Outstanding Payable: Rs. {_supplierBalanceSummary.TotalSupplierPayables:N2}");
                    sb.AppendLine($"Total Supplier Purchases : Rs. {_supplierBalanceSummary.TotalSupplierPurchases:N2}");
                    sb.AppendLine($"Suppliers with Balance   : {_supplierBalanceSummary.SuppliersWithOutstandingBalanceCount}");
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    sb.AppendLine(string.Format("{0,-30} {1,-16} {2,16} {3,14}", "Supplier Name", "Phone", "Total Purchases", "Balance Due"));
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    foreach (var s in _supplierBalanceSummary.Suppliers.Take(30))
                    {
                        string sName = s.SupplierName.Length > 30 ? s.SupplierName[..30] : s.SupplierName;
                        sb.AppendLine(string.Format("{0,-30} {1,-16} {2,16:N2} {3,14:N2}", sName, s.Phone ?? "-", s.TotalPurchases, s.CurrentBalance));
                    }
                    return ("Supplier Balances Report", sb.ToString());
                }
                break;

            case 6: // Customer Credit Balances
                if (_customerSummary is not null)
                {
                    sb.AppendLine($"Total Outstanding Credit : Rs. {_customerSummary.OutstandingCustomerBalance:N2}");
                    sb.AppendLine($"Total Customer Revenue   : Rs. {_customerSummary.TotalCustomerRevenue:N2}");
                    sb.AppendLine($"Total Customers          : {_customerSummary.TotalCustomersCount}");
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    sb.AppendLine(string.Format("{0,-30} {1,-16} {2,16} {3,14}", "Customer Name", "Phone", "Total Invoiced", "Balance Due"));
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    foreach (var c in _customerSummary.Customers.Take(30))
                    {
                        string cName = c.Name.Length > 30 ? c.Name[..30] : c.Name;
                        sb.AppendLine(string.Format("{0,-30} {1,-16} {2,16:N2} {3,14:N2}", cName, c.Phone ?? "-", c.TotalSpent, c.OutstandingBalance));
                    }
                    return ("Customer Balances Report", sb.ToString());
                }
                break;

            case 7: // Production & Product Profitability Report
                if (_productionReportSummary is not null)
                {
                    sb.AppendLine($"Reporting Period         : {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd}");
                    sb.AppendLine($"Total Volume Sold        : {_productionReportSummary.TotalVolumeSold} units");
                    sb.AppendLine($"Total Gross Revenue      : Rs. {_productionReportSummary.TotalCombinedRevenue:N2}");
                    sb.AppendLine($"Retail Profit            : Rs. {_productionReportSummary.TotalRetailProfit:N2}");
                    sb.AppendLine($"Wholesale Profit         : Rs. {_productionReportSummary.TotalWholesaleProfit:N2}");
                    sb.AppendLine($"Total Overall Profit     : Rs. {_productionReportSummary.TotalCombinedProfit:N2}");
                    sb.AppendLine($"Overall Margin           : {_productionReportSummary.OverallMarginPercentage:N2}%");
                    sb.AppendLine($"Total Claims Quantity    : {_productionReportSummary.TotalClaimQuantity} units (Rs. {_productionReportSummary.TotalClaimValue:N2})");
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    sb.AppendLine(string.Format("{0,-10} {1,-24} {2,8} {3,12} {4,12} {5,8}", "SKU", "Product Name", "Tot Vol", "Ret Profit", "Whs Profit", "Claims"));
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    foreach (var p in _filteredProductionItems.Take(30))
                    {
                        string pName = p.ProductName.Length > 24 ? p.ProductName[..24] : p.ProductName;
                        sb.AppendLine(string.Format("{0,-10} {1,-24} {2,8} {3,12:N2} {4,12:N2} {5,8}", p.SKU, pName, p.TotalQuantitySold, p.RetailProfit, p.WholesaleProfit, p.ClaimQuantity));
                    }
                    return ("Production & Product Profitability Report", sb.ToString());
                }
                break;
        }

        // Default Overview Analytics
        sb.AppendLine($"Reporting Period         : {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd}");
        if (_generalAnalytics is not null)
        {
            sb.AppendLine($"Total Revenue            : Rs. {_generalAnalytics.TotalRevenue:N2}");
            sb.AppendLine($"Net Revenue              : Rs. {_generalAnalytics.NetRevenue:N2}");
            sb.AppendLine($"Total Transactions       : {_generalAnalytics.TotalTransactions}");
            sb.AppendLine($"Average Order Value      : Rs. {_generalAnalytics.AverageOrderValue:N2}");
            sb.AppendLine($"Total Discounts          : Rs. {_generalAnalytics.TotalDiscounts:N2}");
            sb.AppendLine($"Net Profit               : Rs. {_generalAnalytics.NetProfit:N2}");
        }
        return ("Executive Analytics Overview", sb.ToString());
    }
}

public sealed record ProductClaimSliceDto(
    string ProductName,
    string SKU,
    int Quantity,
    decimal TotalValue,
    double Percentage,
    double StartAngle,
    double EndAngle,
    string ColorHex,
    Geometry SliceGeometry);
