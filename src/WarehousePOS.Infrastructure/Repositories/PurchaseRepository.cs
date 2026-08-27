using Microsoft.EntityFrameworkCore;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Infrastructure.Repositories;

public sealed class PurchaseRepository(AppDbContext db) : IPurchaseRepository
{
    private IQueryable<Purchase> WithIncludes() =>
        db.Purchases.Include(p => p.Supplier).Include(p => p.Items).ThenInclude(i => i.Product);

    public async Task<Purchase?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await WithIncludes().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Purchase>> GetAllAsync(CancellationToken ct = default) =>
        await WithIncludes().OrderByDescending(p => p.PurchaseDate).ToListAsync(ct);

    public async Task<IReadOnlyList<Purchase>> GetByStatusAsync(PurchaseStatus status, CancellationToken ct = default) =>
        await WithIncludes().Where(p => p.Status == status).OrderByDescending(p => p.PurchaseDate).ToListAsync(ct);

    public async Task<IReadOnlyList<Purchase>> GetBySupplierAsync(int supplierId, CancellationToken ct = default) =>
        await WithIncludes().Where(p => p.SupplierId == supplierId).OrderByDescending(p => p.PurchaseDate).ToListAsync(ct);

    public async Task AddAsync(Purchase purchase, CancellationToken ct = default)
    {
        await db.Purchases.AddAsync(purchase, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Purchase purchase, CancellationToken ct = default)
    {
        db.Purchases.Update(purchase);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class InventoryMovementRepository(AppDbContext db) : IInventoryMovementRepository
{
    public async Task<IReadOnlyList<InventoryMovement>> GetByProductAsync(int productId, CancellationToken ct = default) =>
        await db.InventoryMovements
                .Include(m => m.Product)
                .Where(m => m.ProductId == productId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync(ct);

    public async Task<IReadOnlyList<InventoryMovement>> GetAllAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var q = db.InventoryMovements.Include(m => m.Product).AsQueryable();
        if (from.HasValue) q = q.Where(m => m.CreatedAt >= from.Value);
        if (to.HasValue)   q = q.Where(m => m.CreatedAt <= to.Value);
        return await q.OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
    }

    public async Task AddAsync(InventoryMovement movement, CancellationToken ct = default)
    {
        await db.InventoryMovements.AddAsync(movement, ct);
        await db.SaveChangesAsync(ct);
    }
}
