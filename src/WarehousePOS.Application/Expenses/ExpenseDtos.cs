namespace WarehousePOS.Application.Expenses;

public sealed record ExpenseCategoryDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive);

public sealed record ExpenseDto(
    int Id,
    int CategoryId,
    string CategoryName,
    decimal Amount,
    string Description,
    string? ReferenceNo,
    DateTime ExpenseDate,
    int RecordedByUserId);

public sealed record CreateExpenseRequest(
    int CategoryId,
    decimal Amount,
    string Description,
    int RecordedByUserId,
    DateTime? ExpenseDate = null,
    string? ReferenceNo   = null);

public sealed record CreateExpenseCategoryRequest(
    string Name,
    string? Description = null);
