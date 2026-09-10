using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Application.Reports;

public enum DateRangePreset
{
    Today = 1,
    Yesterday = 2,
    Last7Days = 3,
    Last30Days = 4,
    ThisMonth = 5,
    AllTime = 6,
    Custom = 7
}

// 1. General Analytics
public sealed record GeneralAnalyticsDto(
    DateTime FromDate,
    DateTime ToDate,
    decimal TotalRevenue,
    decimal NetRevenue,
    decimal DailySalesRevenue,
    int DailySalesCount,
    int TotalTransactions,
    decimal AverageOrderValue,
    decimal TotalDiscounts,
    decimal TotalOperationalExpenses,
    decimal CostOfGoodsSold,
    decimal NetProfit,
    decimal CashPaymentTotal,
    decimal BankPaymentTotal,
    decimal ChequePaymentTotal,
    decimal RetailSalesRevenue,
    decimal WholesaleSalesRevenue,
    int RetailSalesCount,
    int WholesaleSalesCount);

// 2. Daily Sales
public sealed record DailySalesReportDto(
    DateTime Date,
    int TotalSalesCount,
    decimal TotalRevenue,
    decimal TotalDiscounts,
    decimal NetSales,
    decimal TotalOperationalExpenses = 0m,
    decimal TrueNetProfit = 0m,
    decimal CashSales = 0m,
    decimal BankSales = 0m,
    decimal ChequeSales = 0m,
    decimal RetailSales = 0m,
    decimal WholesaleSales = 0m,
    decimal AverageTransactionValue = 0m);

public sealed record SalesTrendPointDto(
    DateTime Date,
    string DateLabel,
    decimal Revenue,
    int TransactionCount);

public sealed record HourlySalesPointDto(
    int Hour,
    string HourLabel,
    decimal Revenue,
    int TransactionCount);

// 3. Inventory & Stock Status
public sealed record StockValuationReportDto(
    int TotalActiveProducts,
    int TotalQuantityInStock,
    decimal TotalCostValue,
    decimal TotalRetailValuation,
    decimal PotentialProfitMargin,
    int LowStockCount = 0,
    int OutOfStockCount = 0);

public sealed record LowStockItemDto(
    int ProductId,
    string SKU,
    string ProductName,
    string CategoryName,
    int CurrentStock,
    int ReorderLevel,
    decimal WholesalePrice,
    decimal RetailPrice,
    string Status);

public sealed record FastMovingItemDto(
    int ProductId,
    string SKU,
    string ProductName,
    string CategoryName,
    int QuantitySold,
    decimal TotalRevenue,
    int Rank = 0);

// 4. Sales Summary
public sealed record SalesSummaryDto(
    decimal TotalRevenue,
    int TotalUnitsSold,
    int TotalTransactions,
    decimal AverageOrderValue,
    IReadOnlyList<CategoryPerformanceDto> CategoryPerformance,
    IReadOnlyList<FastMovingItemDto> TopProducts,
    IReadOnlyList<SalesTrendPointDto> SalesTrend);

public sealed record CategoryPerformanceDto(
    int CategoryId,
    string CategoryName,
    int UnitsSold,
    decimal Revenue,
    double PercentageOfTotal);

// 5. GRN Reports
public sealed record GrnReportDto(
    int TotalGrnsCount,
    decimal TotalReceivedValue,
    decimal TotalPaidAmount,
    decimal TotalOutstandingBalance,
    IReadOnlyList<GrnRecordDto> Items);

public sealed record GrnRecordDto(
    int PurchaseId,
    int SupplierId,
    string SupplierName,
    DateTime PurchaseDate,
    DateTime? ReceivedDate,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingBalance,
    string Status,
    IReadOnlyList<GrnLineItemDto> LineItems);

public sealed record GrnLineItemDto(
    int ProductId,
    string SKU,
    string ProductName,
    int Quantity,
    int FreeQuantity,
    int ClaimedQuantityReceived,
    decimal UnitCost,
    decimal LineTotal);

// 6. Claim Items
public sealed record ClaimItemReportDto(
    int TotalClaimQuantity,
    decimal TotalClaimValue,
    int SupplierClaimsCount,
    decimal SupplierClaimsValue,
    int CustomerReturnsCount,
    decimal CustomerReturnsValue,
    int SupplierReturnsCount,
    decimal SupplierReturnsValue,
    IReadOnlyList<ClaimRecordDto> Items);

public sealed record ClaimRecordDto(
    DateTime Date,
    string SKU,
    string ProductName,
    string ClaimSource,
    int Quantity,
    decimal UnitPriceOrCost,
    decimal TotalValue,
    string ReferenceNo,
    string Notes);

// 7. Supplier Balances
public sealed record SupplierBalanceReportDto(
    int SupplierId,
    string SupplierName,
    string Phone,
    decimal CurrentBalance,
    string ContactPerson = "",
    decimal TotalPurchases = 0m,
    decimal TotalPaid = 0m,
    DateTime? LastPurchaseDate = null,
    string Status = "Paid");

public sealed record SupplierBalanceSummaryDto(
    decimal TotalSupplierPayables,
    int SuppliersWithOutstandingBalanceCount,
    decimal TotalSupplierPurchases,
    IReadOnlyList<SupplierBalanceReportDto> Suppliers);

// 8. Customer Insights
public sealed record CustomerReportDto(
    int CustomerId,
    string Name,
    string Phone,
    string CustomerType,
    int TotalOrders,
    decimal TotalSpent,
    decimal AverageOrderValue,
    decimal OutstandingBalance);

public sealed record CustomerReportSummaryDto(
    int TotalCustomersCount,
    int ActiveCustomersCount,
    decimal TotalCustomerRevenue,
    decimal OutstandingCustomerBalance,
    IReadOnlyList<CustomerReportDto> Customers);

