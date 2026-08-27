using Microsoft.EntityFrameworkCore;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Application.Reports;

public sealed class ReportService(
    AppDbContext db,
    ISaleRepository saleRepo,
    IProductRepository productRepo,
    ISupplierRepository supplierRepo) : IReportService
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

        return new DailySalesReportDto(date.Date, count, revenue, discounts, netSales);
    }

    public async Task<IReadOnlyList<FastMovingItemDto>> GetFastMovingItemsAsync(int topCount = 10, CancellationToken ct = default)
    {
        var query = await db.SaleItems
            .Include(i => i.Product)
            .ThenInclude(p => p.Category)
            .Where(i => i.Product.IsActive)
            .GroupBy(i => new { i.ProductId, i.Product.Name, i.Product.SKU, CategoryName = i.Product.Category.Name })
            .Select(g => new FastMovingItemDto(
                g.Key.ProductId,
                g.Key.SKU,
                g.Key.Name,
                g.Key.CategoryName,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.LineTotal)))
            .OrderByDescending(x => x.QuantitySold)
            .Take(topCount)
            .ToListAsync(ct);

        return query;
    }

    public async Task<StockValuationReportDto> GetStockValuationReportAsync(CancellationToken ct = default)
    {
        var products = (await productRepo.GetAllAsync(ct)).Where(p => p.IsActive).ToList();

        int totalProducts = products.Count;
        int totalQty      = products.Sum(p => p.StockQuantity);
        decimal costVal   = products.Sum(p => p.StockQuantity * p.StockCost);
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
