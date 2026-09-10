using System.Collections.ObjectModel;
using WarehousePOS.Application.Reports;
using WarehousePOS.Application.Suppliers;
using WarehousePOS.Desktop.Services;

namespace WarehousePOS.Desktop.ViewModels.Reports;

public sealed class ReportsViewModel : ViewModelBase
{
    private readonly IReportService _reportService;
    private readonly ISupplierService _supplierService;
    private readonly SessionContext _session;

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

    // Detail Panel Selection State
    private LowStockItemDto? _selectedLowStockItem;
    private GrnRecordDto? _selectedGrnRecord;
    private SupplierBalanceReportDto? _selectedSupplierBalance;
    private CustomerReportDto? _selectedCustomerReport;

    // Report DTO Data Properties
    private GeneralAnalyticsDto? _generalAnalytics;
    private DailySalesReportDto? _dailySales;
    private StockValuationReportDto? _stockValuation;
    private SalesSummaryDto? _salesSummary;
    private GrnReportDto? _grnReport;
    private ClaimItemReportDto? _claimReport;
    private SupplierBalanceSummaryDto? _supplierBalanceSummary;
    private CustomerReportSummaryDto? _customerSummary;

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
                if (SelectedTabIndex == 4) // GRN tab
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

    // Reports Bindings
    public GeneralAnalyticsDto? GeneralAnalytics
    {
        get => _generalAnalytics;
        private set => SetField(ref _generalAnalytics, value);
    }

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
    public ObservableCollection<LowStockItemDto> FilteredLowStockItems => _filteredLowStockItems;
    public ObservableCollection<GrnRecordDto> FilteredGrnRecords => _filteredGrnRecords;
    public ObservableCollection<ClaimRecordDto> FilteredClaimRecords => _filteredClaimRecords;
    public ObservableCollection<SupplierBalanceReportDto> FilteredSupplierBalances => _filteredSupplierBalances;
    public ObservableCollection<CustomerReportDto> FilteredCustomerReports => _filteredCustomerReports;

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
    public RelayCommand ClearSupplierSelectionCommand { get; }
    public RelayCommand ClearCustomerSelectionCommand { get; }

    public ReportsViewModel(
        IReportService reportService,
        ISupplierService supplierService,
        SessionContext session)
    {
        if (!session.IsAdmin)
            throw new UnauthorizedAccessException("Only an Admin can access Reports.");

        _reportService = reportService;
        _supplierService = supplierService;
        _session = session;

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
        ClearSupplierSelectionCommand = new RelayCommand(() => SelectedSupplierBalance = null);
        ClearCustomerSelectionCommand = new RelayCommand(() => SelectedCustomerReport = null);

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
                case 0: // General Analytics
                    GeneralAnalytics = await _reportService.GetGeneralAnalyticsAsync(FromDate, ToDate, ct);
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
                    foreach (var t in trendPoints) _salesTrend.Add(t);
                    var hourly = await _reportService.GetHourlySalesAsync(ToDate, ct);
                    _hourlySales.Clear();
                    foreach (var h in hourly) _hourlySales.Add(h);
                    OnPropertyChanged(nameof(MaxHourlyRevenue));
                    break;

                case 2: // Inventory
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

                case 3: // Sales Summary
                    SalesSummary = await _reportService.GetSalesSummaryAsync(FromDate, ToDate, ct);
                    if (SalesSummary != null)
                    {
                        _categoryPerformance.Clear();
                        foreach (var c in SalesSummary.CategoryPerformance) _categoryPerformance.Add(c);
                        _fastMovingItems.Clear();
                        foreach (var fm in SalesSummary.TopProducts) _fastMovingItems.Add(fm);
                        _salesTrend.Clear();
                        foreach (var t in SalesSummary.SalesTrend) _salesTrend.Add(t);
                        OnPropertyChanged(nameof(MaxCategoryRevenue));
                        OnPropertyChanged(nameof(MaxSalesTrendRevenue));
                    }
                    break;

                case 4: // GRN Reports
                    await LoadGrnReportAsync(ct);
                    break;

                case 5: // Claim Items
                    ClaimReport = await _reportService.GetClaimItemsReportAsync(FromDate, ToDate, ct);
                    _claimRecords.Clear();
                    if (ClaimReport != null)
                    {
                        foreach (var cr in ClaimReport.Items) _claimRecords.Add(cr);
                    }
                    ApplyClaimFilters();
                    break;

                case 6: // Supplier Balances
                    SupplierBalanceSummary = await _reportService.GetSupplierBalanceReportSummaryAsync(FromDate, ToDate, ct);
                    _supplierBalances.Clear();
                    if (SupplierBalanceSummary != null)
                    {
                        foreach (var sb in SupplierBalanceSummary.Suppliers) _supplierBalances.Add(sb);
                    }
                    ApplySupplierFilters();
                    OnPropertyChanged(nameof(MaxSupplierPayable));
                    break;

                case 7: // Customer Insights
                    CustomerSummary = await _reportService.GetCustomerReportSummaryAsync(FromDate, ToDate, ct);
                    _customerReports.Clear();
                    if (CustomerSummary != null)
                    {
                        foreach (var cr in CustomerSummary.Customers) _customerReports.Add(cr);
                    }
                    ApplyCustomerFilters();
                    OnPropertyChanged(nameof(MaxCustomerSpend));
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

    private void GenerateBusinessInsights()
    {
        _businessInsights.Clear();
        if (GeneralAnalytics == null) return;

        if (GeneralAnalytics.TotalRevenue > 0)
        {
            var retailShare = (GeneralAnalytics.RetailSalesRevenue / GeneralAnalytics.TotalRevenue) * 100m;
            if (retailShare >= 50)
                _businessInsights.Add($"✓ Retail sales generated {retailShare:F1}% of overall revenue.");
            else
                _businessInsights.Add($"✓ Wholesale sales generated {(100m - retailShare):F1}% of overall revenue.");
        }

        if (GeneralAnalytics.NetProfit > 0)
        {
            var profitMargin = GeneralAnalytics.TotalRevenue > 0 ? (GeneralAnalytics.NetProfit / GeneralAnalytics.TotalRevenue) * 100m : 0m;
            _businessInsights.Add($"📈 Net Profit margin stands at {profitMargin:F1}% for the selected period.");
        }

        if (StockValuation != null && StockValuation.LowStockCount > 0)
        {
            _businessInsights.Add($"⚠️ {StockValuation.LowStockCount} products are approaching or below reorder level.");
        }

        if (SupplierBalanceSummary != null && SupplierBalanceSummary.TotalSupplierPayables > 0)
        {
            _businessInsights.Add($"🏢 Outstanding supplier payables total Rs. {SupplierBalanceSummary.TotalSupplierPayables:N2}.");
        }
    }

    private void EnsureAdmin()
    {
        if (!_session.IsAdmin)
            throw new UnauthorizedAccessException("Only an Admin can access Reports.");
    }
}
