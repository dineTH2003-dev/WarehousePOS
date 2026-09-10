using System.Globalization;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Reports;

public sealed class ReportService(
    ISaleRepository saleRepo,
    IProductRepository productRepo,
    ISupplierRepository supplierRepo,
    IExpenseRepository expenseRepo,
    IPurchaseRepository purchaseRepo,
    ICustomerRepository customerRepo,
    IInventoryMovementRepository movementRepo,
    ICategoryRepository categoryRepo) : IReportService
{
    // ── 1. General Analytics ──────────────────────────────────────────────────

    public async Task<GeneralAnalyticsDto> GetGeneralAnalyticsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var start = from.Date;
        var end   = to.Date.AddDays(1).AddTicks(-1);

        var sales = await saleRepo.GetByDateRangeAsync(start, end, ct);
        var activeSales = sales.Where(s => s.Status == SaleStatus.Completed).ToList();

        decimal totalRevenue   = activeSales.Sum(s => s.SubTotal);
        decimal totalDiscounts = activeSales.Sum(s => s.DiscountAmount);
        decimal netRevenue     = activeSales.Sum(s => s.TotalAmount);
        int totalTransactions  = activeSales.Count;
        decimal aov            = totalTransactions > 0 ? netRevenue / totalTransactions : 0m;

        // Daily Sales KPI (today's sales within date range context)
        var todayStart = DateTime.Today;
        var todayEnd   = todayStart.AddDays(1).AddTicks(-1);
        var todaySales = activeSales.Where(s => s.SaleDate >= todayStart && s.SaleDate <= todayEnd).ToList();
        if (todaySales.Count == 0 && (start <= DateTime.Today && end >= DateTime.Today))
        {
            // Fetch directly if not in filtered list
            var todayDirect = await saleRepo.GetByDateRangeAsync(todayStart, todayEnd, ct);
            todaySales = todayDirect.Where(s => s.Status == SaleStatus.Completed).ToList();
        }
        decimal dailySalesRevenue = todaySales.Sum(s => s.TotalAmount);
        int dailySalesCount       = todaySales.Count;

        // Expenses & COGS
        var expenses = await expenseRepo.GetByDateRangeAsync(start, end, ct);
        decimal totalExpenses = expenses.Sum(e => e.Amount);

        var productIds = activeSales.SelectMany(s => s.Items).Select(i => i.ProductId).Distinct().ToList();
        var allProducts = await productRepo.GetAllAsync(ct);
        var productMap  = allProducts.Where(p => productIds.Contains(p.Id)).ToDictionary(p => p.Id);

        decimal cogs = 0m;
        foreach (var sale in activeSales)
        {
            foreach (var item in sale.Items)
            {
                var prod = item.Product ?? (productMap.TryGetValue(item.ProductId, out var p) ? p : null);
                var cost = prod?.WholesalePrice ?? 0m;
                cogs += item.Quantity * cost;
            }
        }
        decimal netProfit = netRevenue - cogs - totalExpenses;

        // Retail vs Wholesale
        var retailSales    = activeSales.Where(s => s.SaleType == SaleType.Retail).ToList();
        var wholesaleSales = activeSales.Where(s => s.SaleType == SaleType.Wholesale).ToList();

        decimal retailRev    = retailSales.Sum(s => s.TotalAmount);
        decimal wholesaleRev = wholesaleSales.Sum(s => s.TotalAmount);

        // Payment Breakdown (using actual payment amounts / method approximations)
        decimal cashTotal   = activeSales.Sum(s => s.AmountPaid);
        decimal bankTotal   = 0m;
        decimal chequeTotal = 0m;

        return new GeneralAnalyticsDto(
            start,
            end,
            totalRevenue,
            netRevenue,
            dailySalesRevenue,
            dailySalesCount,
            totalTransactions,
            aov,
            totalDiscounts,
            totalExpenses,
            cogs,
            netProfit,
            cashTotal,
            bankTotal,
            chequeTotal,
            retailRev,
            wholesaleRev,
            retailSales.Count,
            wholesaleSales.Count);
    }

    // ── 2. Daily Sales & Trend ────────────────────────────────────────────────

    public async Task<DailySalesReportDto> GetDailySalesReportAsync(DateTime date, CancellationToken ct = default)
    {
        return await GetDailySalesReportAsync(date.Date, date.Date, ct);
    }

    public async Task<DailySalesReportDto> GetDailySalesReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var start = from.Date;
        var end   = to.Date.AddDays(1).AddTicks(-1);

        var sales = await saleRepo.GetByDateRangeAsync(start, end, ct);
        var activeSales = sales.Where(s => s.Status == SaleStatus.Completed).ToList();

        int count        = activeSales.Count;
        decimal revenue  = activeSales.Sum(s => s.SubTotal);
        decimal discounts= activeSales.Sum(s => s.DiscountAmount);
        decimal netSales = activeSales.Sum(s => s.TotalAmount);
        decimal aov      = count > 0 ? netSales / count : 0m;

        var expenses = await expenseRepo.GetByDateRangeAsync(start, end, ct);
        decimal totalExp = expenses.Sum(e => e.Amount);
        decimal trueProfit = netSales - totalExp;

        decimal retail   = activeSales.Where(s => s.SaleType == SaleType.Retail).Sum(s => s.TotalAmount);
        decimal wholesale= activeSales.Where(s => s.SaleType == SaleType.Wholesale).Sum(s => s.TotalAmount);

        return new DailySalesReportDto(
            start,
            count,
            revenue,
            discounts,
            netSales,
            totalExp,
            trueProfit,
            netSales,
            0m,
            0m,
            retail,
            wholesale,
            aov);
    }

    public async Task<IReadOnlyList<SalesTrendPointDto>> GetSalesTrendAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var start = from.Date;
        var end   = to.Date.AddDays(1).AddTicks(-1);

        var sales = await saleRepo.GetByDateRangeAsync(start, end, ct);
        var activeSales = sales.Where(s => s.Status == SaleStatus.Completed).ToList();

        var grouped = activeSales
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new SalesTrendPointDto(
                g.Key,
                g.Key.ToString("MMM dd", CultureInfo.InvariantCulture),
                g.Sum(s => s.TotalAmount),
                g.Count()))
            .OrderBy(p => p.Date)
            .ToList();

        // Fill missing dates if within a reasonable range (up to 31 days)
        var days = (to.Date - from.Date).Days + 1;
        if (days is > 1 and <= 31 && grouped.Count < days)
        {
            var map = grouped.ToDictionary(p => p.Date.Date);
            var result = new List<SalesTrendPointDto>();
            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                if (map.TryGetValue(d, out var existing))
                {
                    result.Add(existing);
                }
                else
                {
                    result.Add(new SalesTrendPointDto(d, d.ToString("MMM dd", CultureInfo.InvariantCulture), 0m, 0));
                }
            }
            return result;
        }

        return grouped;
    }

    public async Task<IReadOnlyList<HourlySalesPointDto>> GetHourlySalesAsync(DateTime date, CancellationToken ct = default)
    {
        var start = date.Date;
        var end   = start.AddDays(1).AddTicks(-1);

        var sales = await saleRepo.GetByDateRangeAsync(start, end, ct);
        var activeSales = sales.Where(s => s.Status == SaleStatus.Completed).ToList();

        var map = activeSales.GroupBy(s => s.SaleDate.Hour)
            .ToDictionary(g => g.Key, g => (Revenue: g.Sum(s => s.TotalAmount), Count: g.Count()));

        var result = new List<HourlySalesPointDto>();
        for (int h = 8; h <= 21; h++) // business hours 08:00 to 21:00
        {
            string label = $"{h:D2}:00";
            if (map.TryGetValue(h, out var data))
            {
                result.Add(new HourlySalesPointDto(h, label, data.Revenue, data.Count));
            }
            else
            {
                result.Add(new HourlySalesPointDto(h, label, 0m, 0));
            }
        }
        return result;
    }

    // ── 3. Inventory & Stock Status ───────────────────────────────────────────

    public async Task<StockValuationReportDto> GetStockValuationReportAsync(CancellationToken ct = default)
    {
        var products = (await productRepo.GetAllAsync(ct)).Where(p => p.IsActive).ToList();

        int totalProducts = products.Count;
        int totalQty      = products.Sum(p => p.StockQuantity);
        decimal costVal   = products.Sum(p => p.StockQuantity * p.WholesalePrice);
        decimal retailVal = products.Sum(p => p.StockQuantity * p.RetailPrice);
        decimal margin    = retailVal - costVal;

        int lowStockCount = products.Count(p => p.StockQuantity <= p.ReorderLevel && p.StockQuantity > 0);
        int outOfStock    = products.Count(p => p.StockQuantity <= 0);

        return new StockValuationReportDto(totalProducts, totalQty, costVal, retailVal, margin, lowStockCount, outOfStock);
    }

    public async Task<IReadOnlyList<LowStockItemDto>> GetLowStockReportAsync(CancellationToken ct = default)
    {
        var products = (await productRepo.GetAllAsync(ct))
            .Where(p => p.IsActive && p.StockQuantity <= p.ReorderLevel)
            .OrderBy(p => p.StockQuantity)
            .ToList();

        return products.Select(p => new LowStockItemDto(
            p.Id,
            p.SKU,
            p.Name,
            p.Category?.Name ?? "Uncategorized",
            p.StockQuantity,
            p.ReorderLevel,
            p.WholesalePrice,
            p.RetailPrice,
            p.StockQuantity <= 0 ? "Out of Stock" : "Low Stock"))
            .ToList();
    }

    public async Task<IReadOnlyList<LowStockItemDto>> GetAllInventoryItemsAsync(CancellationToken ct = default)
    {
        var products = (await productRepo.GetAllAsync(ct))
            .Where(p => p.IsActive)
            .OrderBy(p => p.StockQuantity <= 0 ? 0 : (p.StockQuantity <= p.ReorderLevel ? 1 : 2))
            .ThenBy(p => p.Name)
            .ToList();

        return products.Select(p => new LowStockItemDto(
            p.Id,
            p.SKU,
            p.Name,
            p.Category?.Name ?? "Uncategorized",
            p.StockQuantity,
            p.ReorderLevel,
            p.WholesalePrice,
            p.RetailPrice,
            p.StockQuantity <= 0 ? "Out of Stock" : (p.StockQuantity <= p.ReorderLevel ? "Low Stock" : "Normal")))
            .ToList();
    }

    public async Task<IReadOnlyList<FastMovingItemDto>> GetFastMovingItemsAsync(int topCount = 10, CancellationToken ct = default)
    {
        var raw = await saleRepo.GetTopSellingProductsAsync(topCount, ct);
        int rank = 1;
        return raw.Select(x => new FastMovingItemDto(x.ProductId, x.Sku, x.Name, x.CategoryName, x.QuantitySold, x.TotalSales, rank++)).ToList();
    }

    public async Task<IReadOnlyList<FastMovingItemDto>> GetFastMovingItemsAsync(DateTime from, DateTime to, int topCount = 10, CancellationToken ct = default)
    {
        var start = from.Date;
        var end   = to.Date.AddDays(1).AddTicks(-1);

        var sales = await saleRepo.GetByDateRangeAsync(start, end, ct);
        var activeSales = sales.Where(s => s.Status == SaleStatus.Completed).ToList();

        var itemGroups = activeSales
            .SelectMany(s => s.Items)
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                QuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(topCount)
            .ToList();

        if (itemGroups.Count == 0) return [];

        var productIds = itemGroups.Select(g => g.ProductId).ToList();
        var products   = (await productRepo.GetAllAsync(ct)).Where(p => productIds.Contains(p.Id)).ToDictionary(p => p.Id);

        int rank = 1;
        var result = new List<FastMovingItemDto>();
        foreach (var g in itemGroups)
        {
            if (products.TryGetValue(g.ProductId, out var p))
            {
                result.Add(new FastMovingItemDto(
                    p.Id,
                    p.SKU,
                    p.Name,
                    p.Category?.Name ?? "General",
                    g.QuantitySold,
                    g.TotalRevenue,
                    rank++));
            }
        }
        return result;
    }

    // ── 4. Sales Summary ──────────────────────────────────────────────────────

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var start = from.Date;
        var end   = to.Date.AddDays(1).AddTicks(-1);

        var sales = await saleRepo.GetByDateRangeAsync(start, end, ct);
        var activeSales = sales.Where(s => s.Status == SaleStatus.Completed).ToList();

        decimal totalRevenue  = activeSales.Sum(s => s.TotalAmount);
        int totalTransactions = activeSales.Count;
        int totalUnits        = activeSales.Sum(s => s.Items.Sum(i => i.Quantity));
        decimal aov           = totalTransactions > 0 ? totalRevenue / totalTransactions : 0m;

        // Category performance
        var categoryGroups = activeSales
            .SelectMany(s => s.Items)
            .GroupBy(i => i.Product?.CategoryId ?? 0)
            .Select(g => new
            {
                CategoryId = g.Key,
                UnitsSold  = g.Sum(i => i.Quantity),
                Revenue    = g.Sum(i => i.LineTotal)
            })
            .ToList();

        var categories = (await categoryRepo.GetAllAsync(ct)).ToDictionary(c => c.Id);
        var categoryPerf = new List<CategoryPerformanceDto>();

        foreach (var g in categoryGroups)
        {
            string catName = g.CategoryId > 0 && categories.TryGetValue(g.CategoryId, out var cat) ? cat.Name : "General";
            double pct = totalRevenue > 0 ? (double)(g.Revenue / totalRevenue * 100m) : 0;
            categoryPerf.Add(new CategoryPerformanceDto(g.CategoryId, catName, g.UnitsSold, g.Revenue, Math.Round(pct, 1)));
        }

        categoryPerf = categoryPerf.OrderByDescending(c => c.Revenue).ToList();

        // Top products
        var topProducts = await GetFastMovingItemsAsync(from, to, 10, ct);

        // Sales trend
        var trend = await GetSalesTrendAsync(from, to, ct);

        return new SalesSummaryDto(
            totalRevenue,
            totalUnits,
            totalTransactions,
            aov,
            categoryPerf,
            topProducts,
            trend);
    }

    // ── 5. GRN Reports ────────────────────────────────────────────────────────

    public async Task<GrnReportDto> GetGrnReportAsync(DateTime from, DateTime to, int? supplierId = null, CancellationToken ct = default)
    {
        var start = from.Date;
        var end   = to.Date.AddDays(1).AddTicks(-1);

        var purchases = await purchaseRepo.GetAllAsync(ct);
        var filtered = purchases
            .Where(p => p.PurchaseDate >= start && p.PurchaseDate <= end && p.Status != PurchaseStatus.Cancelled)
            .ToList();

        if (supplierId.HasValue && supplierId.Value > 0)
        {
            filtered = filtered.Where(p => p.SupplierId == supplierId.Value).ToList();
        }

        int totalCount        = filtered.Count;
        decimal totalReceived = filtered.Sum(p => p.TotalAmount);
        decimal totalPaid     = filtered.Sum(p => p.PaidAmount);
        decimal totalBalance  = filtered.Sum(p => p.RemainingBalance);

        var grnRecords = filtered.Select(p =>
        {
            string status = p.PaidAmount >= p.TotalAmount && p.TotalAmount > 0
                ? "Paid"
                : p.PaidAmount > 0 ? "Partially Paid" : "Outstanding";

            var lineItems = p.Items.Select(i => new GrnLineItemDto(
                i.ProductId,
                i.Product?.SKU ?? string.Empty,
                i.Product?.Name ?? string.Empty,
                i.Quantity,
                i.FreeQuantity,
                i.ClaimedQuantityReceived,
                i.UnitCost,
                i.TotalCost)).ToList();

            return new GrnRecordDto(
                p.Id,
                p.SupplierId,
                p.Supplier?.Name ?? "Unknown Supplier",
                p.PurchaseDate,
                p.ReceivedDate,
                p.TotalAmount,
                p.PaidAmount,
                p.RemainingBalance,
                status,
                lineItems);
        }).OrderByDescending(g => g.PurchaseDate).ToList();

        return new GrnReportDto(totalCount, totalReceived, totalPaid, totalBalance, grnRecords);
    }

    // ── 6. Claim Items ────────────────────────────────────────────────────────

    public async Task<ClaimItemReportDto> GetClaimItemsReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var start = from.Date;
        var end   = to.Date.AddDays(1).AddTicks(-1);

        var movements = await movementRepo.GetAllAsync(start, end, ct);
        var claimMovements = movements.Where(m =>
            m.Type is MovementType.Adjustment or MovementType.ReturnIn or MovementType.ReturnOut ||
            (m.ReferenceType != null && m.ReferenceType.Contains("Claim", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var claimRecords = new List<ClaimRecordDto>();
        int supplierClaimsCount = 0;
        decimal supplierClaimsVal = 0m;
        int customerReturnsCount = 0;
        decimal customerReturnsVal = 0m;
        int supplierReturnsCount = 0;
        decimal supplierReturnsVal = 0m;

        foreach (var m in claimMovements)
        {
            string source = m.ReferenceType switch
            {
                "WarrantyClaim" => "Customer Warranty Claim",
                "SaleCancellation" => "Customer Return (Cancellation)",
                _ => m.Type.ToString()
            };

            decimal unitVal = m.Product?.WholesalePrice ?? 0m;
            decimal totalVal = m.Quantity * unitVal;

            if (source.Contains("Warranty") || source.Contains("Customer"))
            {
                customerReturnsCount++;
                customerReturnsVal += totalVal;
            }
            else if (m.Type == MovementType.ReturnOut)
            {
                supplierReturnsCount++;
                supplierReturnsVal += totalVal;
            }
            else
            {
                supplierClaimsCount++;
                supplierClaimsVal += totalVal;
            }

            claimRecords.Add(new ClaimRecordDto(
                m.CreatedAt,
                m.Product?.SKU ?? string.Empty,
                m.Product?.Name ?? "Product #" + m.ProductId,
                source,
                m.Quantity,
                unitVal,
                totalVal,
                m.ReferenceId ?? "-",
                m.Notes ?? string.Empty));
        }

        int totalQty = claimRecords.Sum(c => c.Quantity);
        decimal totalValSum = claimRecords.Sum(c => c.TotalValue);

        return new ClaimItemReportDto(
            totalQty,
            totalValSum,
            supplierClaimsCount,
            supplierClaimsVal,
            customerReturnsCount,
            customerReturnsVal,
            supplierReturnsCount,
            supplierReturnsVal,
            claimRecords.OrderByDescending(c => c.Date).ToList());
    }

    // ── 7. Supplier Balances ──────────────────────────────────────────────────

    public async Task<IReadOnlyList<SupplierBalanceReportDto>> GetSupplierBalanceReportAsync(CancellationToken ct = default)
    {
        var summary = await GetSupplierBalanceReportSummaryAsync(null, null, ct);
        return summary.Suppliers;
    }

    public async Task<SupplierBalanceSummaryDto> GetSupplierBalanceReportSummaryAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var suppliers = await supplierRepo.GetActiveAsync(ct);
        var purchases = await purchaseRepo.GetAllAsync(ct);

        var list = new List<SupplierBalanceReportDto>();
        decimal totalPayables = 0m;
        int outstandingCount  = 0;
        decimal totalPurchasesSum = 0m;

        foreach (var s in suppliers)
        {
            var suppPurchases = purchases.Where(p => p.SupplierId == s.Id && p.Status != PurchaseStatus.Cancelled).ToList();
            if (from.HasValue && to.HasValue)
            {
                var start = from.Value.Date;
                var end   = to.Value.Date.AddDays(1).AddTicks(-1);
                suppPurchases = suppPurchases.Where(p => p.PurchaseDate >= start && p.PurchaseDate <= end).ToList();
            }

            decimal totalPurchases = suppPurchases.Sum(p => p.TotalAmount);
            decimal totalPaid      = suppPurchases.Sum(p => p.PaidAmount);
            var lastPurchase       = suppPurchases.OrderByDescending(p => p.PurchaseDate).FirstOrDefault();

            totalPayables += s.Balance;
            totalPurchasesSum += totalPurchases;
            if (s.Balance > 0) outstandingCount++;

            string status = s.Balance <= 0 ? "Paid" : (totalPaid > 0 ? "Partial" : "Outstanding");

            list.Add(new SupplierBalanceReportDto(
                s.Id,
                s.Name,
                s.Phone ?? string.Empty,
                s.Balance,
                s.ContactPerson ?? string.Empty,
                totalPurchases,
                totalPaid,
                lastPurchase?.PurchaseDate,
                status));
        }

        return new SupplierBalanceSummaryDto(
            totalPayables,
            outstandingCount,
            totalPurchasesSum,
            list.OrderByDescending(s => s.CurrentBalance).ToList());
    }

    // ── 8. Customer Insights ──────────────────────────────────────────────────

    public async Task<CustomerReportSummaryDto> GetCustomerReportSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var start = from.Date;
        var end   = to.Date.AddDays(1).AddTicks(-1);

        var customers = await customerRepo.GetAllAsync(ct);
        var sales     = await saleRepo.GetByDateRangeAsync(start, end, ct);
        var activeSales = sales.Where(s => s.Status == SaleStatus.Completed).ToList();

        var salesByCustomer = activeSales.Where(s => s.CustomerId.HasValue).GroupBy(s => s.CustomerId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        var list = new List<CustomerReportDto>();
        decimal totalRev = 0m;
        int activeCount = 0;

        foreach (var c in customers)
        {
            int orders = 0;
            decimal spent = 0m;

            if (salesByCustomer.TryGetValue(c.Id, out var custSales))
            {
                orders = custSales.Count;
                spent  = custSales.Sum(s => s.TotalAmount);
            }

            if (orders > 0) activeCount++;
            totalRev += spent;

            decimal aov = orders > 0 ? spent / orders : 0m;

            list.Add(new CustomerReportDto(
                c.Id,
                c.Name,
                c.Phone ?? string.Empty,
                c.Type.ToString(),
                orders,
                spent,
                aov,
                0m));
        }

        return new CustomerReportSummaryDto(
            customers.Count,
            activeCount,
            totalRev,
            0m,
            list.OrderByDescending(c => c.TotalSpent).ToList());
    }

    // ── Monthly Report Summary (Notifications) ────────────────────────────────

    public async Task<Notifications.MonthlyReportSummaryDto> GetMonthlyReportSummaryAsync(int year, int month, CancellationToken ct = default)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end   = start.AddMonths(1).AddTicks(-1);

        var sales = await saleRepo.GetByDateRangeAsync(start, end, ct);
        var activeSales = sales.Where(s => s.Status == SaleStatus.Completed).ToList();

        var count = activeSales.Count;
        var revenue = activeSales.Sum(s => s.SubTotal);
        var discounts = activeSales.Sum(s => s.DiscountAmount);
        var netSales = activeSales.Sum(s => s.TotalAmount);

        var expenses = await expenseRepo.GetByDateRangeAsync(start, end, ct);
        var totalExp = expenses.Sum(e => e.Amount);
        var netProfit = netSales - totalExp;

        var valuation = await GetStockValuationReportAsync(ct);
        var topRaw = await saleRepo.GetTopSellingProductsAsync(10, ct);
        var topSelling = topRaw
            .Select(t => new Notifications.MonthlyTopProductDto(t.Name, t.Sku, t.QuantitySold, t.TotalSales))
            .ToList();

        string monthLabel = start.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        return new Notifications.MonthlyReportSummaryDto(
            year,
            month,
            monthLabel,
            count,
            revenue,
            discounts,
            netSales,
            totalExp,
            netProfit,
            valuation.TotalActiveProducts,
            valuation.TotalQuantityInStock,
            valuation.TotalCostValue,
            valuation.TotalRetailValuation,
            topSelling);
    }
}

