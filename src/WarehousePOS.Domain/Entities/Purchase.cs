using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Exceptions;

namespace WarehousePOS.Domain.Entities;

/// <summary>
/// Purchase order aggregate. Tracks the full lifecycle:
/// Draft → Confirmed → Received | Cancelled
/// </summary>
public sealed class Purchase : AggregateRoot
{
    private readonly List<PurchaseItem> _items = [];
    private Purchase() { }

    public int SupplierId              { get; private set; }
    public Supplier Supplier           { get; private set; } = null!;
    public PurchaseStatus Status        { get; private set; } = PurchaseStatus.Draft;
    public string? Notes               { get; private set; }
    public DateTime PurchaseDate       { get; private set; }
    public DateTime? ReceivedDate      { get; private set; }
    public int CreatedByUserId         { get; private set; }

    public IReadOnlyList<PurchaseItem> Items => _items;

    public decimal TotalAmount => _items.Sum(i => i.TotalCost);

    public static Purchase Create(int supplierId, int createdByUserId, string? notes = null)
    {
        if (supplierId <= 0) throw new ArgumentOutOfRangeException(nameof(supplierId));
        return new Purchase
        {
            SupplierId      = supplierId,
            CreatedByUserId = createdByUserId,
            Notes           = notes?.Trim(),
            PurchaseDate    = DateTime.UtcNow
        };
    }

    public void AddItem(int productId, int quantity, decimal unitCost)
    {
        if (Status != PurchaseStatus.Draft)
            throw new BusinessRuleViolationException("PurchaseNotDraft", "Items can only be added to Draft purchases.");
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitCost < 0)  throw new ArgumentOutOfRangeException(nameof(unitCost));

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
            _items.Remove(existing);

        _items.Add(PurchaseItem.Create(productId, quantity, unitCost));
        SetUpdatedAt();
    }

    public void RemoveItem(int productId)
    {
        if (Status != PurchaseStatus.Draft)
            throw new BusinessRuleViolationException("PurchaseNotDraft", "Items can only be removed from Draft purchases.");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is not null) { _items.Remove(item); SetUpdatedAt(); }
    }

    public void Confirm()
    {
        if (Status != PurchaseStatus.Draft)
            throw new BusinessRuleViolationException("InvalidStatus", "Only Draft purchases can be confirmed.");
        if (!_items.Any())
            throw new BusinessRuleViolationException("EmptyPurchase", "Cannot confirm an empty purchase order.");
        Status = PurchaseStatus.Confirmed;
        SetUpdatedAt();
    }

    public void Receive()
    {
        if (Status != PurchaseStatus.Confirmed)
            throw new BusinessRuleViolationException("InvalidStatus", "Only Confirmed purchases can be received.");
        Status       = PurchaseStatus.Received;
        ReceivedDate = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Cancel()
    {
        if (Status == PurchaseStatus.Received)
            throw new BusinessRuleViolationException("InvalidStatus", "Cannot cancel a received purchase.");
        Status = PurchaseStatus.Cancelled;
        SetUpdatedAt();
    }
}

public sealed class PurchaseItem
{
    private PurchaseItem() { }

    public int Id          { get; private set; }
    public int PurchaseId  { get; private set; }
    public int ProductId   { get; private set; }
    public int Quantity    { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalCost => Quantity * UnitCost;

    public Product Product { get; private set; } = null!;

    internal static PurchaseItem Create(int productId, int quantity, decimal unitCost) =>
        new() { ProductId = productId, Quantity = quantity, UnitCost = unitCost };
}
