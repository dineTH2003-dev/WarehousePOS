namespace WarehousePOS.Application.Notifications;

public sealed record MonthlyReportSummaryDto(
    int Year,
    int Month,
    string MonthLabel,
    int TotalTransactions,
    decimal GrossSales,
    decimal TotalDiscounts,
    decimal NetSales,
    decimal TotalExpenses,
    decimal NetProfit,
    int TotalProducts,
    int TotalInventoryUnits,
    decimal InventoryCostValuation,
    decimal InventoryRetailValuation,
    IReadOnlyList<MonthlyTopProductDto> TopSellingProducts);

public sealed record MonthlyTopProductDto(
    string Name,
    string Sku,
    int QuantitySold,
    decimal TotalRevenue);
