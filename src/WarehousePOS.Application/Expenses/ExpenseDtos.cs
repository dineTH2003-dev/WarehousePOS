using WarehousePOS.Application.Reports;

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
    int RecordedByUserId,
    string CreatedBy = "Admin");

public sealed record CreateExpenseRequest(
    int CategoryId,
    decimal Amount,
    string Description,
    int RecordedByUserId,
    DateTime? ExpenseDate = null,
    string? ReferenceNo   = null);

public sealed record UpdateExpenseRequest(
    int CategoryId,
    decimal Amount,
    string Description,
    DateTime ExpenseDate,
    string? ReferenceNo = null);

public sealed record CreateExpenseCategoryRequest(
    string Name,
    string? Description = null);

public sealed record ExpenseFilterRequest(
    DateRangePreset DatePreset,
    DateTime FromDate,
    DateTime ToDate,
    int? CategoryId = null,
    string? SearchTerm = null);

public sealed record ExpenseCategoryAnalyticsDto(
    int CategoryId,
    string CategoryName,
    decimal TotalAmount,
    double PercentageOfTotal,
    int Count);

public sealed record ExpenseMonthlyTrendDto(
    int Year,
    int Month,
    string MonthLabel,
    decimal TotalExpenses,
    int TransactionCount);

public sealed record ExpenseAnalyticsSummaryDto(
    DateTime FromDate,
    DateTime ToDate,
    decimal TotalExpenses,
    int ExpenseCount,
    decimal AverageExpense,
    string HighestCategoryName,
    decimal HighestCategoryAmount,
    double HighestCategoryPercentage,
    decimal PreviousPeriodTotalExpenses,
    double? PercentageChangeVsPreviousPeriod,
    IReadOnlyList<ExpenseCategoryAnalyticsDto> CategoryBreakdown,
    IReadOnlyList<ExpenseMonthlyTrendDto> MonthlyTrend,
    IReadOnlyList<ExpenseDto> FilteredExpenses);
