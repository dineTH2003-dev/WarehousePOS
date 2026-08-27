using Microsoft.EntityFrameworkCore;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Infrastructure.Repositories;

public sealed class SupplierRepository(AppDbContext db) : ISupplierRepository
{
    public async Task<Supplier?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken ct = default) =>
        await db.Suppliers.OrderBy(s => s.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Supplier>> GetActiveAsync(CancellationToken ct = default) =>
        await db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(ct);

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default) =>
        await db.Suppliers.AnyAsync(
            s => s.Name.ToLower() == name.ToLower() && (excludeId == null || s.Id != excludeId), ct);

    public async Task AddAsync(Supplier supplier, CancellationToken ct = default)
    {
        await db.Suppliers.AddAsync(supplier, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Supplier supplier, CancellationToken ct = default)
    {
        db.Suppliers.Update(supplier);
        await db.SaveChangesAsync(ct);
    }
}
