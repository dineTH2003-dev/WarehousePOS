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
    int WholesaleSalesCount,
    decimal CardPaymentTotal = 0m,
    decimal CreditSalesTotal = 0m,
    decimal TotalCustomerOutstanding = 0m,
    decimal GrossMarginPercentage = 0m,
    string TopCategoryName = "N/A",
    decimal TopCategoryRevenue = 0m,
    string TopProductName = "N/A",
    int TopProductQty = 0);


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
    IReadOnlyList<GrnLineItemDto> LineItems,
    string? ContactPerson = null);

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

// 9. Production & Product Profitability Report
public sealed record ProductProductionReportItemDto(
    int ProductId,
    string SKU,
    string ProductName,
    string CategoryName,
    decimal UnitCost,
    decimal WholesalePrice,
    decimal RetailPrice,
    int RetailQuantitySold,
    decimal RetailRevenue,
    decimal RetailProfit,
    int WholesaleQuantitySold,
    decimal WholesaleRevenue,
    decimal WholesaleProfit,
    int TotalQuantitySold,
    decimal TotalRevenue,
    decimal TotalCostOfGoodsSold,
    decimal TotalProfit,
    decimal ProfitMarginPercentage,
    int ClaimQuantity,
    decimal ClaimValue);

public sealed record ProductProductionReportSummaryDto(
    int TotalProductsCount,
    int TotalVolumeSold,
    decimal TotalRetailRevenue,
    decimal TotalRetailProfit,
    decimal TotalWholesaleRevenue,
    decimal TotalWholesaleProfit,
    decimal TotalCombinedRevenue,
    decimal TotalCombinedProfit,
    decimal OverallMarginPercentage,
    int TotalClaimQuantity,
    decimal TotalClaimValue,
    IReadOnlyList<ProductProductionReportItemDto> Items);

// 10. EXECUTIVE REPORTING SUITE (7 MODULES)

// [SYSTEM_TAG: OVERVIEW_DASHBOARD]
public sealed record OverviewDashboardReportDto(
    decimal GrossRevenue,
    decimal CostOfGoodsSold,
    decimal FixedExpenses,
    decimal VariableExpenses,
    decimal NetProfit,
    decimal AverageOrderValue,
    decimal MonthOverMonthGrowthPercentage,
    decimal GrossMarginPercentage);

// [SYSTEM_TAG: REVENUE_VELOCITY_REPORT]
public sealed record RevenueVelocityReportDto(
    decimal GrossSales,
    decimal NetSales,
    decimal SalesTaxCollected,
    IReadOnlyList<PaymentSplitDto> PaymentSplits,
    IReadOnlyList<HourlySalesPointDto> VelocitySpikes);

public sealed record PaymentSplitDto(
    string Method,
    decimal TotalAmount,
    int TransactionCount);

// [SYSTEM_TAG: INVENTORY_VALUATION_LEAN]
public sealed record InventoryLeanReportDto(
    decimal TotalStockValuation,
    int LowStockAlertsCount,
    decimal StockTurnoverRate,
    int DeadStockCount,
    IReadOnlyList<LowStockItemDto> DangerStockItems,
    IReadOnlyList<SupplierValuationDto> SupplierValuations);

public sealed record SupplierValuationDto(
    string SupplierName,
    decimal TotalValuation,
    int ProductCount);

// [SYSTEM_TAG: OPERATIONAL_EXPENSE_LEDGER]
public sealed record ExpenseLedgerReportDto(
    decimal FixedCostsTotal,
    decimal VariableCostsTotal,
    decimal FuelReceiptsTotal,
    decimal VehicleMaintenanceTotal,
    decimal TechToolsTotal,
    IReadOnlyList<ExpenseCategoryBreakdownDto> Categories);

public sealed record ExpenseCategoryBreakdownDto(
    string CategoryName,
    decimal Amount,
    string ExpenseType);

// [SYSTEM_TAG: FLEET_TECH_PAYROLL_ANALYTICS]
public sealed record FleetTechPayrollReportDto(
    decimal TotalAdminSalesSalary,
    decimal TotalSalesCommissions,
    decimal TotalDriverPayouts,
    decimal TotalTechBonuses,
    IReadOnlyList<PayrollConsolidationDto> Employees);

public sealed record PayrollConsolidationDto(
    int EmployeeId,
    string EmployeeName,
    string Role,
    decimal BaseSalary,
    decimal SalesCommission,
    decimal DriverTripBonus,
    decimal TechServiceBonus,
    decimal TotalEarnings);

// [SYSTEM_TAG: CUSTOMER_LIFETIME_VALUATION]
public sealed record CustomerValuationReportDto(
    int NewCustomersCount,
    int ReturningCustomersCount,
    decimal NewVsReturningRatio,
    IReadOnlyList<CustomerVipDto> TopVipClients,
    IReadOnlyList<AccountsReceivableAgingDto> AccountsReceivable);

public sealed record CustomerVipDto(
    int CustomerId,
    string CustomerName,
    decimal TotalSpend,
    int OrderCount);

public sealed record AccountsReceivableAgingDto(
    int CustomerId,
    string CustomerName,
    decimal OutstandingBalance,
    int DaysOverdue,
    decimal CollectionProgressPercentage);

// [SYSTEM_TAG: ITEM_PERFORMANCE_MATRIX]
public sealed record ItemPerformanceMatrixReportDto(
    IReadOnlyList<SkuScatterPointDto> SkuPoints,
    IReadOnlyList<ProductAffinityPairDto> AffinityPairs);

public sealed record SkuScatterPointDto(
    int ProductId,
    string SKU,
    string ProductName,
    decimal UnitMargin,
    decimal MarginPercentage,
    int SalesVelocity,
    double DefectRatePercentage,
    string Quadrant);

public sealed record ProductAffinityPairDto(
    string ItemA,
    string ItemB,
    int TimesBoughtTogether,
    double AffinityScore);



