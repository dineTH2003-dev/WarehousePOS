using Microsoft.EntityFrameworkCore;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Infrastructure.Repositories;

public sealed class SupplierProductEntitlementRepository : ISupplierEntitlementRepository
{
    private readonly AppDbContext _db;

    public SupplierProductEntitlementRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<SupplierProductEntitlement?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.SupplierProductEntitlements
            .Include(e => e.Supplier)
            .Include(e => e.Product)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<IReadOnlyList<SupplierProductEntitlement>> GetBySupplierIdAsync(int supplierId, CancellationToken ct = default)
    {
        return await _db.SupplierProductEntitlements
            .Include(e => e.Supplier)
            .Include(e => e.Product)
            .Where(e => e.SupplierId == supplierId)
            .OrderBy(e => e.NextEntitlementDate.HasValue ? 0 : 1)
            .ThenBy(e => e.NextEntitlementDate)
            .ThenByDescending(e => e.EventDate)
            .ThenByDescending(e => e.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SupplierProductEntitlement>> GetByProductIdAsync(int productId, CancellationToken ct = default)
    {
        return await _db.SupplierProductEntitlements
            .Include(e => e.Supplier)
            .Include(e => e.Product)
            .Where(e => e.ProductId == productId)
            .OrderBy(e => e.NextEntitlementDate.HasValue ? 0 : 1)
            .ThenBy(e => e.NextEntitlementDate)
            .ThenByDescending(e => e.EventDate)
            .ThenByDescending(e => e.Id)
            .ToListAsync(ct);
    }

    public async Task AddAsync(SupplierProductEntitlement entitlement, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entitlement);
        await _db.SupplierProductEntitlements.AddAsync(entitlement, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(SupplierProductEntitlement entitlement, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entitlement);
        _db.SupplierProductEntitlements.Remove(entitlement);
        await _db.SaveChangesAsync(ct);
    }
}
