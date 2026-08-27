namespace WarehousePOS.Application.Reports;

public interface IReportService
{
    Task<DailySalesReportDto> GetDailySalesReportAsync(DateTime date, CancellationToken ct = default);
    Task<IReadOnlyList<FastMovingItemDto>> GetFastMovingItemsAsync(int topCount = 10, CancellationToken ct = default);
    Task<StockValuationReportDto> GetStockValuationReportAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SupplierBalanceReportDto>> GetSupplierBalanceReportAsync(CancellationToken ct = default);
}
