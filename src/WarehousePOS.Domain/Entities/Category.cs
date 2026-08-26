using WarehousePOS.Domain.Common;

namespace WarehousePOS.Domain.Entities;

/// <summary>Product category (e.g., Electronics, Stationery).</summary>
public sealed class Category : AggregateRoot
{
    private Category() { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<Product> _products = [];
    public IReadOnlyList<Product> Products => _products.AsReadOnly();

    public static Category Create(string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Category { Name = name.Trim(), Description = description?.Trim() };
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = description?.Trim();
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate()   { IsActive = true;  SetUpdatedAt(); }
}
