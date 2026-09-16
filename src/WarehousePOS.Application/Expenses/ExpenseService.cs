using Microsoft.Extensions.Logging;
using WarehousePOS.Application.Reports;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Expenses;

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ExpenseDto>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default);
    Task<ExpenseDto> UpdateAsync(int id, UpdateExpenseRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    Task<ExpenseAnalyticsSummaryDto> GetAnalyticsAsync(ExpenseFilterRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<ExpenseCategoryDto>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<ExpenseCategoryDto> CreateCategoryAsync(CreateExpenseCategoryRequest request, CancellationToken ct = default);
}

public sealed class ExpenseService(
    IExpenseRepository repo,
    IUserRepository? userRepo,
    ILogger<ExpenseService> logger) : IExpenseService
{
    // Convenience constructor for tests that don't pass IUserRepository
    public ExpenseService(IExpenseRepository repo, ILogger<ExpenseService> logger)
        : this(repo, null, logger) { }

    public async Task<IReadOnlyList<ExpenseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var userMap = await GetUserMapAsync(ct);
        var list = await repo.GetAllAsync(ct);
        return list.Select(e => Map(e, userMap)).ToList();
    }

    public async Task<IReadOnlyList<ExpenseDto>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var userMap = await GetUserMapAsync(ct);
        var list = await repo.GetByDateRangeAsync(from, to, ct);
        return list.Select(e => Map(e, userMap)).ToList();
    }

    public async Task<ExpenseDto> CreateAsync(CreateExpenseRequest req, CancellationToken ct = default)
    {
        _ = await repo.GetCategoryByIdAsync(req.CategoryId, ct)
            ?? throw new EntityNotFoundException(nameof(ExpenseCategory), req.CategoryId);

        var expense = Expense.Create(req.CategoryId, req.Amount, req.Description, req.RecordedByUserId, req.ExpenseDate, req.ReferenceNo);
        await repo.AddAsync(expense, ct);

        logger.LogInformation("Expense recorded: Rs. {Amount:N2} ({Description})", expense.Amount, expense.Description);

        var userMap = await GetUserMapAsync(ct);
        return Map(expense, userMap);
    }

    public async Task<ExpenseDto> UpdateAsync(int id, UpdateExpenseRequest req, CancellationToken ct = default)
    {
        var expense = await repo.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Expense), id);

        _ = await repo.GetCategoryByIdAsync(req.CategoryId, ct)
            ?? throw new EntityNotFoundException(nameof(ExpenseCategory), req.CategoryId);

        expense.Update(req.CategoryId, req.Amount, req.Description, req.ReferenceNo, req.ExpenseDate);
        await repo.UpdateAsync(expense, ct);

        logger.LogInformation("Expense #{Id} updated: Rs. {Amount:N2} ({Description})", id, expense.Amount, expense.Description);

        var userMap = await GetUserMapAsync(ct);
        return Map(expense, userMap);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var expense = await repo.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Expense), id);

        await repo.DeleteAsync(expense, ct);
        logger.LogInformation("Expense #{Id} deleted.", id);
    }

    public async Task<ExpenseAnalyticsSummaryDto> GetAnalyticsAsync(ExpenseFilterRequest request, CancellationToken ct = default)
    {
        var userMap = await GetUserMapAsync(ct);
        var allExpenses = await repo.GetAllAsync(ct);

        var from = request.FromDate.Date;
        var toExclusive = request.ToDate.Date.AddDays(1);

        // Filter by date range
        var periodExpenses = allExpenses.Where(e => e.ExpenseDate >= from && e.ExpenseDate < toExclusive).ToList();

        // Apply Category and Search term filters for table records & period summary
        IEnumerable<Expense> filteredQuery = periodExpenses;

        if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
        {
            filteredQuery = filteredQuery.Where(e => e.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            filteredQuery = filteredQuery.Where(e =>
                e.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (e.Category != null && e.Category.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (e.ReferenceNo != null && e.ReferenceNo.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        var filteredExpenses = filteredQuery.OrderByDescending(e => e.ExpenseDate).ToList();

        // KPIs
        var totalExpenses = filteredExpenses.Sum(e => e.Amount);
        var count = filteredExpenses.Count;
        var avgExpense = count > 0 ? Math.Round(totalExpenses / count, 2) : 0m;

        // Category Breakdown & Map
        var activeCategories = await repo.GetCategoriesAsync(false, ct);
        var catMap = activeCategories.ToDictionary(c => c.Id, c => c.Name);

        // Highest Category in period
        string highestCatName = "N/A";
        decimal highestCatAmount = 0m;
        double highestCatPct = 0.0;

        if (totalExpenses > 0)
        {
            var categoryGroup = filteredExpenses
                .GroupBy(e => e.Category?.Name ?? catMap.GetValueOrDefault(e.CategoryId, "Uncategorized"))
                .Select(g => new { Name = g.Key, Total = g.Sum(e => e.Amount) })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            if (categoryGroup != null)
            {
                highestCatName = categoryGroup.Name;
                highestCatAmount = categoryGroup.Total;
                highestCatPct = Math.Round((double)(highestCatAmount / totalExpenses * 100m), 1);
            }
        }

        // Previous Period Comparison
        var periodSpan = toExclusive - from;
        var prevFrom = from - periodSpan;
        var prevToExclusive = from;

        var prevPeriodExpenses = allExpenses
            .Where(e => e.ExpenseDate >= prevFrom && e.ExpenseDate < prevToExclusive)
            .Sum(e => e.Amount);

        double? pctChange = null;
        if (prevPeriodExpenses > 0)
        {
            pctChange = Math.Round((double)((totalExpenses - prevPeriodExpenses) / prevPeriodExpenses * 100m), 1);
        }

        // Category Breakdown
        var categoryBreakdown = new List<ExpenseCategoryAnalyticsDto>();

        foreach (var cat in activeCategories)
        {
            var catExpenses = periodExpenses.Where(e => e.CategoryId == cat.Id).ToList();
            var catTotal = catExpenses.Sum(e => e.Amount);
            var catPct = periodExpenses.Sum(e => e.Amount) > 0
                ? Math.Round((double)(catTotal / periodExpenses.Sum(e => e.Amount) * 100m), 1)
                : 0.0;

            categoryBreakdown.Add(new ExpenseCategoryAnalyticsDto(
                cat.Id,
                cat.Name,
                catTotal,
                catPct,
                catExpenses.Count));
        }

        // Monthly Expense Trend (Monthly totals for selected year or last 12 months)
        var trendList = new List<ExpenseMonthlyTrendDto>();

        var targetYear = from.Year == toExclusive.AddDays(-1).Year ? from.Year : DateTime.UtcNow.Year;
        for (int m = 1; m <= 12; m++)
        {
            var monthExpenses = allExpenses.Where(e => e.ExpenseDate.Year == targetYear && e.ExpenseDate.Month == m).ToList();
            var mTotal = monthExpenses.Sum(e => e.Amount);
            var mCount = monthExpenses.Count;
            var monthName = new DateTime(targetYear, m, 1).ToString("MMM");

            trendList.Add(new ExpenseMonthlyTrendDto(targetYear, m, monthName, mTotal, mCount));
        }

        var mappedFilteredList = filteredExpenses.Select(e => Map(e, userMap)).ToList();

        return new ExpenseAnalyticsSummaryDto(
            from,
            toExclusive.AddDays(-1),
            totalExpenses,
            count,
            avgExpense,
            highestCatName,
            highestCatAmount,
            highestCatPct,
            prevPeriodExpenses,
            pctChange,
            categoryBreakdown,
            trendList,
            mappedFilteredList);
    }

    public async Task<IReadOnlyList<ExpenseCategoryDto>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default) =>
        (await repo.GetCategoriesAsync(includeInactive, ct)).Select(MapCategory).ToList();

    public async Task<ExpenseCategoryDto> CreateCategoryAsync(CreateExpenseCategoryRequest req, CancellationToken ct = default)
    {
        var category = ExpenseCategory.Create(req.Name, req.Description);
        await repo.AddCategoryAsync(category, ct);
        return MapCategory(category);
    }

    private async Task<Dictionary<int, string>> GetUserMapAsync(CancellationToken ct)
    {
        if (userRepo == null) return new Dictionary<int, string>();
        try
        {
            var users = await userRepo.GetAllAsync(ct);
            return users.ToDictionary(u => u.Id, u => string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName);
        }
        catch
        {
            return new Dictionary<int, string>();
        }
    }

    private static ExpenseDto Map(Expense e, Dictionary<int, string>? userMap = null)
    {
        string createdBy = "Admin";
        if (userMap != null && userMap.TryGetValue(e.RecordedByUserId, out var name))
        {
            createdBy = name;
        }

        return new(
            e.Id,
            e.CategoryId,
            e.Category?.Name ?? string.Empty,
            e.Amount,
            e.Description,
            e.ReferenceNo,
            e.ExpenseDate,
            e.RecordedByUserId,
            createdBy);
    }

    private static ExpenseCategoryDto MapCategory(ExpenseCategory c) => new(
        c.Id, c.Name, c.Description, c.IsActive);
}
