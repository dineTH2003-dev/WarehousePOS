using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Domain.Interfaces;

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Expense>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Expense>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(Expense expense, CancellationToken ct = default);

    // Expense Categories
    Task<IReadOnlyList<ExpenseCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<ExpenseCategory?> GetCategoryByIdAsync(int id, CancellationToken ct = default);
    Task AddCategoryAsync(ExpenseCategory category, CancellationToken ct = default);
    Task UpdateCategoryAsync(ExpenseCategory category, CancellationToken ct = default);
}
