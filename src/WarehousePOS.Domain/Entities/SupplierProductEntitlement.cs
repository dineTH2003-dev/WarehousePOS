using WarehousePOS.Domain.Common;

namespace WarehousePOS.Domain.Entities;

/// <summary>
/// Records details of supplier entitlements, discounts, or free items 
/// associated with inventory item purchases for a supplier.
/// </summary>
public sealed class SupplierProductEntitlement : AggregateRoot
{
    private SupplierProductEntitlement() { } // EF Core constructor

    public int SupplierId { get; private set; }
    public Supplier Supplier { get; private set; } = null!;

    public int ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    /// <summary>
    /// Status or Nature of entitlement (e.g. "Receiving Free Item", "Obtaining Discount", "Making Purchase", "Special Benefit").
    /// </summary>
    public string Nature { get; private set; } = string.Empty;

    /// <summary>
    /// Quantity (units) relevant to free items or purchase quantities. Nullable if pure monetary discount.
    /// </summary>
    public int? Quantity { get; private set; }

    /// <summary>
    /// Value (monetary amount e.g. Rs. discount value). Nullable if pure free item count.
    /// </summary>
    public decimal? Value { get; private set; }

    /// <summary>
    /// Date relevant to the status or event.
    /// </summary>
    public DateTime EventDate { get; private set; }

    /// <summary>
    /// Date of next discount or free item entitlement (if applicable).
    /// </summary>
    public DateTime? NextEntitlementDate { get; private set; }

    /// <summary>
    /// Special notes or remarks.
    /// </summary>
    public string? SpecialNotes { get; private set; }

    public static SupplierProductEntitlement Create(
        int supplierId,
        int productId,
        string nature,
        DateTime eventDate,
        int? quantity = null,
        decimal? value = null,
        DateTime? nextEntitlementDate = null,
        string? specialNotes = null)
    {
        if (supplierId <= 0)
            throw new ArgumentOutOfRangeException(nameof(supplierId), "Supplier ID must be valid.");

        if (productId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productId), "Product ID must be valid.");

        ArgumentException.ThrowIfNullOrWhiteSpace(nature, nameof(nature));

        if (quantity.HasValue && quantity.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");

        if (value.HasValue && value.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Monetary value cannot be negative.");

        return new SupplierProductEntitlement
        {
            SupplierId = supplierId,
            ProductId = productId,
            Nature = nature.Trim(),
            EventDate = eventDate,
            Quantity = quantity,
            Value = value,
            NextEntitlementDate = nextEntitlementDate,
            SpecialNotes = specialNotes?.Trim()
        };
    }

    public void UpdateDetails(
        string nature,
        DateTime eventDate,
        int? quantity = null,
        decimal? value = null,
        DateTime? nextEntitlementDate = null,
        string? specialNotes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nature, nameof(nature));

        if (quantity.HasValue && quantity.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");

        if (value.HasValue && value.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Monetary value cannot be negative.");

        Nature = nature.Trim();
        EventDate = eventDate;
        Quantity = quantity;
        Value = value;
        NextEntitlementDate = nextEntitlementDate;
        SpecialNotes = specialNotes?.Trim();
        SetUpdatedAt();
    }
}
