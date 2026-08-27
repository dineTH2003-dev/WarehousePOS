using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Reports;

public sealed class ReportService(
    ISaleRepository saleRepo,
    IProductRepository productRepo,
    ISupplierRepository supplierRepo,
    IExpenseRepository expenseRepo) : IReportService
{
    public async Task<DailySalesReportDto> GetDailySalesReportAsync(DateTime date, CancellationToken ct = default)
    {
        var start = date.Date;
        var end   = start.AddDays(1).AddTicks(-1);

        var sales = await saleRepo.GetByDateRangeAsync(start, end, ct);
        var activeSales = sales.Where(s => s.Status == SaleStatus.Completed).ToList();

        var count     = activeSales.Count;
        var revenue   = activeSales.Sum(s => s.SubTotal);
        var discounts = activeSales.Sum(s => s.DiscountAmount);
        var netSales  = activeSales.Sum(s => s.TotalAmount);

        var expenses  = await expenseRepo.GetByDateRangeAsync(start, end, ct);
        var totalExp  = expenses.Sum(e => e.Amount);
        var trueProfit= netSales - totalExp;

        return new DailySalesReportDto(date.Date, count, revenue, discounts, netSales, totalExp, trueProfit);
    }

    public async Task<IReadOnlyList<FastMovingItemDto>> GetFastMovingItemsAsync(int topCount = 10, CancellationToken ct = default)
    {
        var raw = await saleRepo.GetTopSellingProductsAsync(topCount, ct);
        return raw.Select(x => new FastMovingItemDto(x.ProductId, x.Sku, x.Name, x.CategoryName, x.QuantitySold, x.TotalSales)).ToList();
    }

    public async Task<StockValuationReportDto> GetStockValuationReportAsync(CancellationToken ct = default)
    {
        var products = (await productRepo.GetAllAsync(ct)).Where(p => p.IsActive).ToList();

        int totalProducts = products.Count;
        int totalQty      = products.Sum(p => p.StockQuantity);
        decimal costVal   = products.Sum(p => p.StockQuantity * p.WholesalePrice);
        decimal retailVal = products.Sum(p => p.StockQuantity * p.RetailPrice);
        decimal margin    = retailVal - costVal;

        return new StockValuationReportDto(totalProducts, totalQty, costVal, retailVal, margin);
    }

    public async Task<IReadOnlyList<SupplierBalanceReportDto>> GetSupplierBalanceReportAsync(CancellationToken ct = default)
    {
        var suppliers = await supplierRepo.GetActiveAsync(ct);

        return suppliers.Select(s => new SupplierBalanceReportDto(
            s.Id,
            s.Name,
            s.Phone ?? string.Empty,
            s.Balance))
            .OrderByDescending(s => s.CurrentBalance)
            .ToList();
    }
}
