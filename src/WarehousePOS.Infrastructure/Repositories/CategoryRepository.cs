using Microsoft.EntityFrameworkCore;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Infrastructure.Repositories;

public sealed class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) =>
        await db.Categories.Include(c => c.Products).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken ct = default) =>
        await db.Categories.Include(c => c.Products)
                           .Where(c => c.IsActive)
                           .OrderBy(c => c.Name)
                           .ToListAsync(ct);

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default) =>
        await db.Categories.AnyAsync(
            c => c.Name.ToLower() == name.ToLower() && (excludeId == null || c.Id != excludeId), ct);

    public async Task AddAsync(Category category, CancellationToken ct = default)
    {
        await db.Categories.AddAsync(category, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        db.Categories.Update(category);
        await db.SaveChangesAsync(ct);
    }
}
