using Microsoft.EntityFrameworkCore;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Infrastructure.Repositories;

public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    private IQueryable<Product> WithCategory() =>
        db.Products.Include(p => p.Category);

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await WithCategory().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        await WithCategory().FirstOrDefaultAsync(p => p.SKU == sku.ToUpperInvariant(), ct);

    public async Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken ct = default) =>
        await WithCategory().FirstOrDefaultAsync(p => p.Barcode == barcode, ct);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default) =>
        await WithCategory().OrderBy(p => p.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId, CancellationToken ct = default) =>
        await WithCategory().Where(p => p.CategoryId == categoryId).OrderBy(p => p.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> SearchAsync(string searchTerm, CancellationToken ct = default)
    {
        var term = searchTerm.Trim().ToLower();
        return await WithCategory()
            .Where(p => p.Name.ToLower().Contains(term)
                     || p.SKU.ToLower().Contains(term)
                     || (p.Barcode != null && p.Barcode.Contains(term)))
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken ct = default) =>
        await WithCategory()
            .Where(p => p.IsActive && p.StockQuantity <= p.ReorderLevel)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync(ct);

    public async Task<bool> ExistsBySkuAsync(string sku, int? excludeId = null, CancellationToken ct = default) =>
        await db.Products.AnyAsync(
            p => p.SKU == sku.ToUpperInvariant() && (excludeId == null || p.Id != excludeId), ct);

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await db.Products.AddAsync(product, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        db.Products.Update(product);
        await db.SaveChangesAsync(ct);
    }
}
