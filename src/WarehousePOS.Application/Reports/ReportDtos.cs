namespace WarehousePOS.Application.Reports;

public sealed record DailySalesReportDto(
    DateTime Date,
    int TotalSalesCount,
    decimal TotalRevenue,
    decimal TotalDiscounts,
    decimal NetSales);

public sealed record FastMovingItemDto(
    int ProductId,
    string SKU,
    string ProductName,
    string CategoryName,
    int QuantitySold,
    decimal TotalRevenue);

public sealed record StockValuationReportDto(
    int TotalActiveProducts,
    int TotalQuantityInStock,
    decimal TotalCostValue,
    decimal TotalRetailValuation,
    decimal PotentialProfitMargin);

public sealed record SupplierBalanceReportDto(
    int SupplierId,
    string SupplierName,
    string Phone,
    decimal CurrentBalance);
