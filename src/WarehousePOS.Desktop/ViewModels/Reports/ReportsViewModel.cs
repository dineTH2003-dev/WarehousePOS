using System.Collections.ObjectModel;
using WarehousePOS.Application.Reports;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Reports;

public sealed class ReportsViewModel : ViewModelBase
{
    private readonly IReportService _reportService;
    private readonly SessionContext _session;

    private DateTime _selectedDate = DateTime.Today;
    private DailySalesReportDto? _dailySales;
    private StockValuationReportDto? _stockValuation;
    private ObservableCollection<FastMovingItemDto> _fastMovingItems = [];
    private ObservableCollection<SupplierBalanceReportDto> _supplierBalances = [];
    private bool _isBusy;

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetField(ref _selectedDate, value))
                _ = LoadDailySalesAsync();
        }
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

    public ObservableCollection<FastMovingItemDto> FastMovingItems => _fastMovingItems;
    public ObservableCollection<SupplierBalanceReportDto> SupplierBalances => _supplierBalances;

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public RelayCommand RefreshCommand { get; }

    public ReportsViewModel(IReportService reportService, SessionContext session)
    {
        if (!session.IsAdmin)
            throw new UnauthorizedAccessException("Only an Admin can access Reports.");

        _reportService = reportService;
        _session = session;
        RefreshCommand = new RelayCommand(async () => await LoadAllReportsAsync());
    }

    public async Task LoadAllReportsAsync()
    {
        EnsureAdmin();
        IsBusy = true;
        try
        {
            await LoadDailySalesAsync();

            var valuation = await _reportService.GetStockValuationReportAsync();
            StockValuation = valuation;

            var fastMoving = await _reportService.GetFastMovingItemsAsync(10);
            _fastMovingItems.Clear();
            foreach (var item in fastMoving) _fastMovingItems.Add(item);

            var supplierBal = await _reportService.GetSupplierBalanceReportAsync();
            _supplierBalances.Clear();
            foreach (var item in supplierBal) _supplierBalances.Add(item);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadDailySalesAsync()
    {
        EnsureAdmin();
        DailySales = await _reportService.GetDailySalesReportAsync(SelectedDate);
    }

    private void EnsureAdmin()
    {
        if (!_session.IsAdmin)
            throw new UnauthorizedAccessException("Only an Admin can access Reports.");
    }
}
