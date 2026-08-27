using Microsoft.EntityFrameworkCore;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Infrastructure.Repositories;

public sealed class StoreSettingRepository(AppDbContext db) : IStoreSettingRepository
{
    public async Task<string?> GetValueAsync(string key, CancellationToken ct = default)
    {
        var norm = key.Trim().ToUpperInvariant();
        var setting = await db.StoreSettings.FirstOrDefaultAsync(s => s.Key == norm, ct);
        return setting?.Value;
    }

    public async Task<IReadOnlyList<StoreSetting>> GetAllAsync(CancellationToken ct = default) =>
        await db.StoreSettings.OrderBy(s => s.Key).ToListAsync(ct);

    public async Task SetValueAsync(string key, string value, string? description = null, CancellationToken ct = default)
    {
        var norm = key.Trim().ToUpperInvariant();
        var setting = await db.StoreSettings.FirstOrDefaultAsync(s => s.Key == norm, ct);

        if (setting is null)
        {
            setting = StoreSetting.Create(norm, value, description);
            await db.StoreSettings.AddAsync(setting, ct);
        }
        else
        {
            setting.UpdateValue(value);
            db.StoreSettings.Update(setting);
        }

        await db.SaveChangesAsync(ct);
    }
}

public sealed class ExpenseRepository(AppDbContext db) : IExpenseRepository
{
    public async Task<Expense?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.Expenses.Include(e => e.Category).FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Expense>> GetAllAsync(CancellationToken ct = default) =>
        await db.Expenses.Include(e => e.Category).OrderByDescending(e => e.ExpenseDate).ToListAsync(ct);

    public async Task<IReadOnlyList<Expense>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await db.Expenses.Include(e => e.Category)
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync(ct);

    public async Task AddAsync(Expense expense, CancellationToken ct = default)
    {
        await db.Expenses.AddAsync(expense, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ExpenseCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default) =>
        await db.ExpenseCategories.Where(c => includeInactive || c.IsActive).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<ExpenseCategory?> GetCategoryByIdAsync(int id, CancellationToken ct = default) =>
        await db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddCategoryAsync(ExpenseCategory category, CancellationToken ct = default)
    {
        await db.ExpenseCategories.AddAsync(category, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateCategoryAsync(ExpenseCategory category, CancellationToken ct = default)
    {
        db.ExpenseCategories.Update(category);
        await db.SaveChangesAsync(ct);
    }
}
