using Microsoft.EntityFrameworkCore;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Infrastructure.Repositories;

public sealed class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default) =>
        await db.Customers.OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Customer>> GetActiveAsync(CancellationToken ct = default) =>
        await db.Customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Customer>> SearchAsync(string term, CancellationToken ct = default)
    {
        var query = term.Trim().ToLower();
        return await db.Customers
            .Where(c => c.Name.ToLower().Contains(query) ||
                        (c.Phone != null && c.Phone.Contains(query)))
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Customer>> GetByTypeAsync(SaleType type, CancellationToken ct = default) =>
        await db.Customers.Where(c => c.Type == type && c.IsActive).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        await db.Customers.AddAsync(customer, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        db.Customers.Update(customer);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class SaleRepository(AppDbContext db) : ISaleRepository
{
    private IQueryable<Sale> WithIncludes() =>
        db.Sales
          .Include(s => s.Customer)
          .Include(s => s.Items)
          .ThenInclude(i => i.Product);

    public async Task<Sale?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await WithIncludes().FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Sale>> GetAllAsync(CancellationToken ct = default) =>
        await WithIncludes().OrderByDescending(s => s.SaleDate).ToListAsync(ct);

    public async Task<IReadOnlyList<Sale>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await WithIncludes()
            .Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Sale>> GetByCustomerAsync(int customerId, CancellationToken ct = default) =>
        await WithIncludes()
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(ct);

    public async Task AddAsync(Sale sale, CancellationToken ct = default)
    {
        await db.Sales.AddAsync(sale, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Sale sale, CancellationToken ct = default)
    {
        db.Sales.Update(sale);
        await db.SaveChangesAsync(ct);
    }
}
