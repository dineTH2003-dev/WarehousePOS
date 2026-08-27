using WarehousePOS.Domain.Common;

namespace WarehousePOS.Domain.Entities;

/// <summary>A supplier from whom products are purchased.</summary>
public sealed class Supplier : AggregateRoot
{
    private Supplier() { }

    public string Name          { get; private set; } = string.Empty;
    public string? ContactPerson { get; private set; }
    public string? Phone         { get; private set; }
    public string? Email         { get; private set; }
    public string? Address       { get; private set; }
    public decimal Balance       { get; private set; }   // positive = we owe supplier
    public bool IsActive         { get; private set; } = true;

    public static Supplier Create(
        string name,
        string? contactPerson = null,
        string? phone = null,
        string? email = null,
        string? address = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Supplier
        {
            Name          = name.Trim(),
            ContactPerson = contactPerson?.Trim(),
            Phone         = phone?.Trim(),
            Email         = email?.Trim(),
            Address       = address?.Trim()
        };
    }

    public void Update(string name, string? contactPerson, string? phone, string? email, string? address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name          = name.Trim();
        ContactPerson = contactPerson?.Trim();
        Phone         = phone?.Trim();
        Email         = email?.Trim();
        Address       = address?.Trim();
        SetUpdatedAt();
    }

    /// <summary>Add to the outstanding balance (e.g. after a purchase).</summary>
    public void AddToBalance(decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Balance += amount;
        SetUpdatedAt();
    }

    /// <summary>Reduce balance (e.g. after payment to supplier).</summary>
    public void ReduceBalance(decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Balance -= amount;
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate()   { IsActive = true;  SetUpdatedAt(); }
}
