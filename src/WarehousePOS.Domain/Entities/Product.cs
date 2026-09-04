using WarehousePOS.Domain.Common;

namespace WarehousePOS.Domain.Entities;

/// <summary>
/// Represents a product in the warehouse catalog.
/// A product has both retail and wholesale pricing.
/// </summary>
public sealed class Product : AggregateRoot
{
    private Product() { } // EF Core constructor

    public string Name { get; private set; } = string.Empty;
    public string SKU { get; private set; } = string.Empty;
    public string? Barcode { get; private set; }
    public string? Description { get; private set; }
    public decimal RetailPrice { get; private set; }
    public decimal WholesalePrice { get; private set; }
    public int StockQuantity { get; private set; }
    public int ReorderLevel { get; private set; }
    public bool IsActive { get; private set; } = true;

    public int CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;

    public static Product Create(
        string name,
        string sku,
        decimal retailPrice,
        decimal wholesalePrice,
        int categoryId,
        string? barcode = null,
        string? description = null,
        int reorderLevel = 5,
        int stockQuantity = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        if (retailPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(retailPrice), "Retail price cannot be negative.");

        if (wholesalePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(wholesalePrice), "Wholesale price cannot be negative.");

        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative.");

        return new Product
        {
            Name = name.Trim(),
            SKU = sku.Trim().ToUpperInvariant(),
            Barcode = barcode?.Trim(),
            Description = description?.Trim(),
            RetailPrice = retailPrice,
            WholesalePrice = wholesalePrice,
            CategoryId = categoryId,
            ReorderLevel = reorderLevel,
            StockQuantity = stockQuantity
        };
    }

    public void UpdatePricing(decimal newRetailPrice, decimal newWholesalePrice)
    {
        if (newRetailPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newRetailPrice));
        if (newWholesalePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newWholesalePrice));

        RetailPrice = newRetailPrice;
        WholesalePrice = newWholesalePrice;
        SetUpdatedAt();
    }

    public void UpdateDetails(string name, string? barcode, string? description, int categoryId, int reorderLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (reorderLevel < 0)
            throw new ArgumentOutOfRangeException(nameof(reorderLevel));

        Name = name.Trim();
        Barcode = barcode?.Trim();
        Description = description?.Trim();
        CategoryId = categoryId;
        ReorderLevel = reorderLevel;
        SetUpdatedAt();
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        StockQuantity += quantity;
        SetUpdatedAt();
    }

    public void DeductStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        if (quantity > StockQuantity)
            throw new Exceptions.InsufficientStockException(Name, quantity, StockQuantity);

        StockQuantity -= quantity;
        SetUpdatedAt();
    }

    public void SetStockQuantity(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Stock quantity cannot be negative.");

        StockQuantity = quantity;
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate()   { IsActive = true;  SetUpdatedAt(); }

    public bool IsLowStock => StockQuantity <= ReorderLevel;
}
