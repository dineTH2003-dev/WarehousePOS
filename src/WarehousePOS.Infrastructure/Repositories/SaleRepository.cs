using Microsoft.EntityFrameworkCore;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Infrastructure.Repositories;

public sealed class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct = default)
    {
        var trimmed = phone.Trim();
        return await db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Phone != null && c.Phone == trimmed, ct);
    }

    public async Task<Customer?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var trimmed = name.Trim().ToLower();
        return await db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name.ToLower() == trimmed, ct);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default) =>
        await db.Customers.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Customer>> GetActiveAsync(CancellationToken ct = default) =>
        await db.Customers.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Customer>> SearchAsync(string term, CancellationToken ct = default)
    {
        var query = term.Trim().ToLower();
        return await db.Customers.AsNoTracking()
            .Where(c => c.Name.ToLower().Contains(query) ||
                        (c.Phone != null && c.Phone.Contains(query)))
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Customer>> GetByTypeAsync(SaleType type, CancellationToken ct = default) =>
        await db.Customers.AsNoTracking().Where(c => c.Type == type && c.IsActive).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        await db.Customers.AddAsync(customer, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        var entry = db.ChangeTracker.Entries<Customer>().FirstOrDefault(e => e.Entity.Id == customer.Id);
        if (entry is null)
        {
            db.Entry(customer).State = EntityState.Modified;
        }
        else if (!ReferenceEquals(entry.Entity, customer))
        {
            entry.CurrentValues.SetValues(customer);
        }
        await db.SaveChangesAsync(ct);
    }
}

public sealed class SaleRepository(AppDbContext db) : ISaleRepository
{
    private IQueryable<Sale> WithIncludesNoTracking() =>
        db.Sales
          .AsNoTracking()
          .Include(s => s.Customer)
          .Include(s => s.Payments)
          .Include(s => s.Items)
          .ThenInclude(i => i.Product);

    private IQueryable<Sale> WithIncludesTracking() =>
        db.Sales
          .Include(s => s.Customer)
          .Include(s => s.Payments)
          .Include(s => s.Items)
          .ThenInclude(i => i.Product);

    public async Task<Sale?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await WithIncludesNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Sale>> GetAllAsync(CancellationToken ct = default) =>
        await WithIncludesNoTracking().OrderByDescending(s => s.SaleDate).ToListAsync(ct);

    public async Task<IReadOnlyList<Sale>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await WithIncludesNoTracking()
            .Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Sale>> GetByCustomerAsync(int customerId, CancellationToken ct = default)
    {
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId, ct);
        var phone = customer?.Phone?.Trim();
        var name = customer?.Name?.Trim();

        var query = WithIncludesNoTracking();

        if (!string.IsNullOrEmpty(phone))
        {
            var matchingCustomerIds = await db.Customers.AsNoTracking()
                .Where(c => c.Phone == phone)
                .Select(c => c.Id)
                .ToListAsync(ct);

            return await query
                .Where(s => (s.CustomerId.HasValue && matchingCustomerIds.Contains(s.CustomerId.Value)) ||
                            s.CustomerId == customerId ||
                            (s.CustomerPhone != null && s.CustomerPhone == phone))
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync(ct);
        }

        if (!string.IsNullOrEmpty(name))
        {
            var lowerName = name.ToLower();
            return await query
                .Where(s => s.CustomerId == customerId ||
                            (s.CustomerId == null && s.CustomerName != null && s.CustomerName.ToLower() == lowerName))
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync(ct);
        }

        return await query
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Sale>> SearchAsync(DateTime? from, DateTime? to, string? searchTerm, SaleStatus? status, PaymentMethod? paymentMethod, SaleType? saleType, CancellationToken ct = default)
    {
        var query = WithIncludesNoTracking();

        if (from.HasValue)
            query = query.Where(s => s.SaleDate >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.SaleDate <= to.Value);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (paymentMethod.HasValue)
            query = query.Where(s => s.PaymentMethod == paymentMethod.Value);

        if (saleType.HasValue)
            query = query.Where(s => s.SaleType == saleType.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(s =>
                s.Id.ToString().Contains(term) ||
                (s.Customer != null && s.Customer.Name.ToLower().Contains(term)) ||
                (s.Customer != null && s.Customer.Phone != null && s.Customer.Phone.Contains(term)) ||
                (s.CustomerName != null && s.CustomerName.ToLower().Contains(term)) ||
                (s.CustomerPhone != null && s.CustomerPhone.Contains(term)) ||
                (s.Notes != null && s.Notes.ToLower().Contains(term)));
        }

        return await query.OrderByDescending(s => s.SaleDate).ToListAsync(ct);
    }

    public async Task AddAsync(Sale sale, CancellationToken ct = default)
    {
        await db.Sales.AddAsync(sale, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Sale sale, CancellationToken ct = default)
    {
        // Detach ALL currently-tracked Sale entities to avoid identity-tracking
        // conflicts when the same DbContext scope processes multiple sales.
        foreach (var e in db.ChangeTracker.Entries<Sale>().ToList())
            e.State = EntityState.Detached;

        // Also detach all tracked related entities to prevent duplicates.
        foreach (var e in db.ChangeTracker.Entries<SaleItem>().ToList())
            e.State = EntityState.Detached;
        foreach (var e in db.ChangeTracker.Entries<SalePayment>().ToList())
            e.State = EntityState.Detached;

        // Mark the root sale as Modified (updates scalar columns).
        db.Entry(sale).State = EntityState.Modified;

        // Mark existing items as Unchanged (no structural changes to items during payment).
        foreach (var item in sale.Items)
            db.Entry(item).State = EntityState.Unchanged;

        // For each payment: new ones (Id == 0) are Added; existing ones are Unchanged.
        foreach (var payment in sale.Payments)
        {
            db.Entry(payment).State = payment.Id == 0
                ? EntityState.Added
                : EntityState.Unchanged;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<(int ProductId, string Sku, string Name, string CategoryName, int QuantitySold, decimal TotalSales)>> GetTopSellingProductsAsync(int topCount = 10, CancellationToken ct = default)
    {
        // EF Core 3+ cannot translate GroupBy over navigation properties
        // (e.g. i.Product.Name, i.Product.Category.Name) — it throws
        // RelationalGroupByShaperExpression. Fix: group by primitive scalar
        // (ProductId only) so EF Core can emit a plain SQL GROUP BY, then
        // join the aggregation result back to Products + Categories in-memory.

        // Step 1 — SQL-translatable aggregation grouped only on the FK scalar.
        var aggregated = await db.SaleItems
            .Where(i => i.Product.IsActive)
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId    = g.Key,
                QuantitySold = g.Sum(x => x.Quantity),
                // LineTotal is a C# computed property (no DB column) — EF Core cannot
                // translate it in a SQL Sum(). Inline the formula instead:
                TotalSales   = g.Sum(x => (x.UnitPrice * x.Quantity) - x.Discount)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(topCount)
            .ToListAsync(ct);

        if (aggregated.Count == 0)
            return [];

        // Step 2 — fetch names/SKUs/categories for those product IDs.
        var productIds = aggregated.Select(a => a.ProductId).ToList();
        var products   = await db.Products
            .Include(p => p.Category)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(ct);

        var productMap = products.ToDictionary(p => p.Id);

        // Step 3 — join in memory to preserve the ORDER BY QuantitySold ranking.
        return aggregated
            .Where(a => productMap.ContainsKey(a.ProductId))
            .Select(a =>
            {
                var p = productMap[a.ProductId];
                return (a.ProductId, p.SKU, p.Name, p.Category.Name, a.QuantitySold, a.TotalSales);
            })
            .ToList();
    }
}
