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
    public int WarrantyYears { get; private set; }
    public int WarrantyMonths { get; private set; }
    public int WarrantyDays { get; private set; }
    public int ClaimedQuantity { get; private set; }
    public int PendingHigherPriceStockQuantity { get; private set; }
    public decimal? PendingNewRetailPrice { get; private set; }
    public decimal? PendingNewWholesalePrice { get; private set; }
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
        int stockQuantity = 0,
        int warrantyYears = 0,
        int warrantyMonths = 0,
        int warrantyDays = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        if (retailPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(retailPrice), "Retail price cannot be negative.");

        if (wholesalePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(wholesalePrice), "Wholesale price cannot be negative.");

        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative.");

        if (warrantyYears < 0)
            throw new ArgumentOutOfRangeException(nameof(warrantyYears), "Warranty years cannot be negative.");

        if (warrantyMonths < 0)
            throw new ArgumentOutOfRangeException(nameof(warrantyMonths), "Warranty months cannot be negative.");

        if (warrantyDays < 0)
            throw new ArgumentOutOfRangeException(nameof(warrantyDays), "Warranty days cannot be negative.");

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
            StockQuantity = stockQuantity,
            WarrantyYears = warrantyYears,
            WarrantyMonths = warrantyMonths,
            WarrantyDays = warrantyDays
        };
    }

    public void UpdatePricing(decimal newRetailPrice, decimal newWholesalePrice)
    {
        if (newRetailPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newRetailPrice));
        if (newWholesalePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newWholesalePrice));

        // If updated price is higher or stock is 0, update immediately.
        if (StockQuantity == 0 || (newRetailPrice >= RetailPrice && newWholesalePrice >= WholesalePrice))
        {
            RetailPrice = newRetailPrice;
            WholesalePrice = newWholesalePrice;
            PendingHigherPriceStockQuantity = 0;
            PendingNewRetailPrice = null;
            PendingNewWholesalePrice = null;
        }
        else if (newRetailPrice < RetailPrice || newWholesalePrice < WholesalePrice)
        {
            // Price reduction: retain higher existing price for current stock
            PendingHigherPriceStockQuantity += StockQuantity;
            PendingNewRetailPrice = newRetailPrice;
            PendingNewWholesalePrice = newWholesalePrice;
        }

        SetUpdatedAt();
    }

    public void ReceiveInboundStock(int quantityReceived, decimal newRetailPrice, decimal newWholesalePrice)
    {
        if (quantityReceived < 0)
            throw new ArgumentOutOfRangeException(nameof(quantityReceived));

        var effectiveRetail = newRetailPrice > 0 ? newRetailPrice : RetailPrice;
        var effectiveWholesale = newWholesalePrice > 0 ? newWholesalePrice : WholesalePrice;

        // Case A: Price Increase or new stock when existing stock is 0
        if (StockQuantity == 0 || (effectiveRetail >= RetailPrice && effectiveWholesale >= WholesalePrice))
        {
            RetailPrice = effectiveRetail;
            WholesalePrice = effectiveWholesale;
            PendingHigherPriceStockQuantity = 0;
            PendingNewRetailPrice = null;
            PendingNewWholesalePrice = null;
        }
        // Case B: Price Reduction while old stock is present
        else if (effectiveRetail < RetailPrice || effectiveWholesale < WholesalePrice)
        {
            // Retain higher current RetailPrice / WholesalePrice for old stock
            PendingHigherPriceStockQuantity += StockQuantity;
            PendingNewRetailPrice = effectiveRetail;
            PendingNewWholesalePrice = effectiveWholesale;
        }

        StockQuantity += quantityReceived;
        SetUpdatedAt();
    }

    public void UpdateDetails(
        string name,
        string sku,
        string? barcode,
        string? description,
        int categoryId,
        int reorderLevel,
        int warrantyYears = 0,
        int warrantyMonths = 0,
        int warrantyDays = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        if (reorderLevel < 0)
            throw new ArgumentOutOfRangeException(nameof(reorderLevel));
        if (warrantyYears < 0)
            throw new ArgumentOutOfRangeException(nameof(warrantyYears));
        if (warrantyMonths < 0)
            throw new ArgumentOutOfRangeException(nameof(warrantyMonths));
        if (warrantyDays < 0)
            throw new ArgumentOutOfRangeException(nameof(warrantyDays));

        Name = name.Trim();
        SKU = sku.Trim().ToUpperInvariant();
        Barcode = barcode?.Trim();
        Description = description?.Trim();
        CategoryId = categoryId;
        ReorderLevel = reorderLevel;
        WarrantyYears = warrantyYears;
        WarrantyMonths = warrantyMonths;
        WarrantyDays = warrantyDays;
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

        if (PendingHigherPriceStockQuantity > 0)
        {
            PendingHigherPriceStockQuantity -= quantity;
            if (PendingHigherPriceStockQuantity <= 0)
            {
                // Old higher-priced stock is now completely exhausted!
                // Transition catalog price to new lower price
                PendingHigherPriceStockQuantity = 0;
                if (PendingNewRetailPrice.HasValue && PendingNewRetailPrice.Value > 0)
                {
                    RetailPrice = PendingNewRetailPrice.Value;
                    PendingNewRetailPrice = null;
                }
                if (PendingNewWholesalePrice.HasValue && PendingNewWholesalePrice.Value > 0)
                {
                    WholesalePrice = PendingNewWholesalePrice.Value;
                    PendingNewWholesalePrice = null;
                }
            }
        }

        SetUpdatedAt();
    }

    public void SetStockQuantity(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Stock quantity cannot be negative.");

        StockQuantity = quantity;
        SetUpdatedAt();
    }

    public void RecordWarrantyClaim(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Claim quantity must be positive.");

        ClaimedQuantity += quantity;
        SetUpdatedAt();
    }

    public void FulfillClaim(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Fulfill quantity must be positive.");

        if (quantity > ClaimedQuantity)
            throw new InvalidOperationException($"Cannot fulfill {quantity} claimed items because only {ClaimedQuantity} items are pending claim.");

        ClaimedQuantity -= quantity;
        StockQuantity += quantity;
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate()   { IsActive = true;  SetUpdatedAt(); }

    public bool IsLowStock => StockQuantity <= ReorderLevel;
}
