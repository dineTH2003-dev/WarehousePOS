using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Domain.Entities;

/// <summary>A customer — retail walk-in or registered wholesale buyer.</summary>
public sealed class Customer : AggregateRoot
{
    private Customer() { }

    public string Name      { get; private set; } = string.Empty;
    public string? Phone    { get; private set; }
    public string? Email    { get; private set; }
    public string? Address  { get; private set; }
    public SaleType Type    { get; private set; } = SaleType.Retail;
    public bool IsActive    { get; private set; } = true;

    public static Customer Create(
        string name,
        SaleType type   = SaleType.Retail,
        string? phone   = null,
        string? email   = null,
        string? address = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Customer
        {
            Name    = name.Trim(),
            Type    = type,
            Phone   = phone?.Trim(),
            Email   = email?.Trim(),
            Address = address?.Trim()
        };
    }

    public void Update(string name, SaleType type, string? phone, string? email, string? address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name    = name.Trim();
        Type    = type;
        Phone   = phone?.Trim();
        Email   = email?.Trim();
        Address = address?.Trim();
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate()   { IsActive = true;  SetUpdatedAt(); }
}
