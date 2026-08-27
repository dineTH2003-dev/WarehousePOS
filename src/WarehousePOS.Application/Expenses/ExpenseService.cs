using Microsoft.Extensions.Logging;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Expenses;

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ExpenseDto>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<ExpenseCategoryDto>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<ExpenseCategoryDto> CreateCategoryAsync(CreateExpenseCategoryRequest request, CancellationToken ct = default);
}

public sealed class ExpenseService(
    IExpenseRepository repo,
    ILogger<ExpenseService> logger) : IExpenseService
{
    public async Task<IReadOnlyList<ExpenseDto>> GetAllAsync(CancellationToken ct = default) =>
        (await repo.GetAllAsync(ct)).Select(Map).ToList();

    public async Task<IReadOnlyList<ExpenseDto>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        (await repo.GetByDateRangeAsync(from, to, ct)).Select(Map).ToList();

    public async Task<ExpenseDto> CreateAsync(CreateExpenseRequest req, CancellationToken ct = default)
    {
        _ = await repo.GetCategoryByIdAsync(req.CategoryId, ct)
            ?? throw new EntityNotFoundException(nameof(ExpenseCategory), req.CategoryId);

        var expense = Expense.Create(req.CategoryId, req.Amount, req.Description, req.RecordedByUserId, req.ExpenseDate, req.ReferenceNo);
        await repo.AddAsync(expense, ct);

        logger.LogInformation("Expense recorded: Rs. {Amount:N2} ({Description})", expense.Amount, expense.Description);
        return Map(expense);
    }

    public async Task<IReadOnlyList<ExpenseCategoryDto>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default) =>
        (await repo.GetCategoriesAsync(includeInactive, ct)).Select(MapCategory).ToList();

    public async Task<ExpenseCategoryDto> CreateCategoryAsync(CreateExpenseCategoryRequest req, CancellationToken ct = default)
    {
        var category = ExpenseCategory.Create(req.Name, req.Description);
        await repo.AddCategoryAsync(category, ct);
        return MapCategory(category);
    }

    private static ExpenseDto Map(Expense e) => new(
        e.Id, e.CategoryId, e.Category?.Name ?? string.Empty,
        e.Amount, e.Description, e.ReferenceNo, e.ExpenseDate, e.RecordedByUserId);

    private static ExpenseCategoryDto MapCategory(ExpenseCategory c) => new(
        c.Id, c.Name, c.Description, c.IsActive);
}
