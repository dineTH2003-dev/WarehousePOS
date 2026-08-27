using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Exceptions;

namespace WarehousePOS.Domain.Entities;

/// <summary>
/// Sale aggregate — represents a completed or cancelled sale transaction.
/// Sale creation is atomic: stock deduction and inventory movement are
/// handled together in SaleService within a single EF Core transaction.
/// </summary>
public sealed class Sale : AggregateRoot
{
    private readonly List<SaleItem> _items = [];
    private Sale() { }

    public int?   CustomerId       { get; private set; }   // null = walk-in
    public Customer? Customer      { get; private set; }
    public SaleType  SaleType      { get; private set; }
    public SaleStatus Status       { get; private set; } = SaleStatus.Completed;
    public decimal SubTotal        { get; private set; }   // before discount
    public decimal DiscountAmount  { get; private set; }
    public decimal TotalAmount     { get; private set; }   // SubTotal - Discount
    public decimal AmountPaid      { get; private set; }
    public decimal Change          => AmountPaid - TotalAmount;
    public string? Notes           { get; private set; }
    public DateTime SaleDate       { get; private set; }
    public int CreatedByUserId     { get; private set; }

    public IReadOnlyList<SaleItem> Items => _items;

    public static Sale Create(
        SaleType saleType,
        int createdByUserId,
        int? customerId = null,
        string? notes   = null)
    {
        return new Sale
        {
            SaleType        = saleType,
            CustomerId      = customerId,
            CreatedByUserId = createdByUserId,
            Notes           = notes?.Trim(),
            SaleDate        = DateTime.UtcNow
        };
    }

    public void AddItem(Product product, int quantity, decimal unitPrice, decimal itemDiscount = 0)
    {
        if (!product.IsActive)
            throw new BusinessRuleViolationException("InactiveProduct",
                $"Cannot sell deactivated product '{product.Name}'.");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        if (itemDiscount < 0 || itemDiscount > unitPrice * quantity)
            throw new BusinessRuleViolationException("InvalidDiscount", "Discount cannot exceed line total.");

        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is not null) _items.Remove(existing);

        _items.Add(SaleItem.Create(product.Id, quantity, unitPrice, itemDiscount));
        RecalculateTotals();
    }

    public void RemoveItem(int productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is not null) { _items.Remove(item); RecalculateTotals(); }
    }

    public void ApplyDiscount(decimal discountAmount)
    {
        if (discountAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(discountAmount));
        if (discountAmount > SubTotal)
            throw new BusinessRuleViolationException("ExcessiveDiscount", "Discount cannot exceed sub-total.");

        DiscountAmount = discountAmount;
        TotalAmount    = SubTotal - DiscountAmount;
    }

    public void RecordPayment(decimal amountPaid)
    {
        if (amountPaid < TotalAmount)
            throw new BusinessRuleViolationException("InsufficientPayment",
                $"Amount paid (Rs. {amountPaid:N2}) is less than total (Rs. {TotalAmount:N2}).");
        AmountPaid = amountPaid;
    }

    public void Cancel()
    {
        if (Status == SaleStatus.Cancelled)
            throw new BusinessRuleViolationException("AlreadyCancelled", "Sale is already cancelled.");
        Status = SaleStatus.Cancelled;
        SetUpdatedAt();
    }

    private void RecalculateTotals()
    {
        SubTotal      = _items.Sum(i => i.LineTotal);
        TotalAmount   = SubTotal - DiscountAmount;
    }
}

public sealed class SaleItem
{
    private SaleItem() { }

    public int Id           { get; private set; }
    public int SaleId       { get; private set; }
    public int ProductId    { get; private set; }
    public Product Product  { get; private set; } = null!;
    public int Quantity     { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Discount  { get; private set; }
    public decimal LineTotal => (UnitPrice * Quantity) - Discount;

    internal static SaleItem Create(int productId, int quantity, decimal unitPrice, decimal discount) =>
        new() { ProductId = productId, Quantity = quantity, UnitPrice = unitPrice, Discount = discount };
}
