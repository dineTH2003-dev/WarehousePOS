namespace WarehousePOS.Application.Reports;

public interface IReportService
{
    // General Analytics & Dashboard
    Task<GeneralAnalyticsDto> GetGeneralAnalyticsAsync(DateTime from, DateTime to, CancellationToken ct = default);

    // Daily Sales & Trend
    Task<DailySalesReportDto> GetDailySalesReportAsync(DateTime date, CancellationToken ct = default);
    Task<DailySalesReportDto> GetDailySalesReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<SalesTrendPointDto>> GetSalesTrendAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<HourlySalesPointDto>> GetHourlySalesAsync(DateTime date, CancellationToken ct = default);

    // Inventory & Stock
    Task<StockValuationReportDto> GetStockValuationReportAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LowStockItemDto>> GetLowStockReportAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LowStockItemDto>> GetAllInventoryItemsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FastMovingItemDto>> GetFastMovingItemsAsync(int topCount = 10, CancellationToken ct = default);
    Task<IReadOnlyList<FastMovingItemDto>> GetFastMovingItemsAsync(DateTime from, DateTime to, int topCount = 10, CancellationToken ct = default);

    // Sales Summary
    Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);

    // GRN Reports
    Task<GrnReportDto> GetGrnReportAsync(DateTime from, DateTime to, int? supplierId = null, CancellationToken ct = default);

    // Claim Items
    Task<ClaimItemReportDto> GetClaimItemsReportAsync(DateTime from, DateTime to, CancellationToken ct = default);

    // Supplier Balances
    Task<IReadOnlyList<SupplierBalanceReportDto>> GetSupplierBalanceReportAsync(CancellationToken ct = default);
    Task<SupplierBalanceSummaryDto> GetSupplierBalanceReportSummaryAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default);

    // Customer Insights
    Task<CustomerReportSummaryDto> GetCustomerReportSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);

    // Monthly Report (Notifications)
    Task<Notifications.MonthlyReportSummaryDto> GetMonthlyReportSummaryAsync(int year, int month, CancellationToken ct = default);
}

