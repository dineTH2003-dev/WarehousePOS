using WarehousePOS.Domain.Common;

namespace WarehousePOS.Domain.Entities;

/// <summary>Expense category (e.g. Utility Bills, Transport, Rent, Wages, Maintenance).</summary>
public sealed class ExpenseCategory : AggregateRoot
{
    private ExpenseCategory() { }

    public string Name        { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive      { get; private set; } = true;

    public static ExpenseCategory Create(string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ExpenseCategory
        {
            Name        = name.Trim(),
            Description = description?.Trim()
        };
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name        = name.Trim();
        Description = description?.Trim();
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate()   { IsActive = true;  SetUpdatedAt(); }
}

/// <summary>An operational expense entry.</summary>
public sealed class Expense : AggregateRoot
{
    private Expense() { }

    public int CategoryId          { get; private set; }
    public ExpenseCategory Category { get; private set; } = null!;
    public decimal Amount          { get; private set; }
    public string Description     { get; private set; } = string.Empty;
    public string? ReferenceNo     { get; private set; }
    public DateTime ExpenseDate    { get; private set; }
    public int RecordedByUserId    { get; private set; }

    public static Expense Create(
        int categoryId,
        decimal amount,
        string description,
        int recordedByUserId,
        DateTime? expenseDate = null,
        string? referenceNo   = null)
    {
        if (categoryId <= 0) throw new ArgumentOutOfRangeException(nameof(categoryId));
        if (amount <= 0)     throw new ArgumentOutOfRangeException(nameof(amount));
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new Expense
        {
            CategoryId       = categoryId,
            Amount           = amount,
            Description      = description.Trim(),
            RecordedByUserId = recordedByUserId,
            ExpenseDate      = expenseDate ?? DateTime.UtcNow,
            ReferenceNo      = referenceNo?.Trim()
        };
    }
}
