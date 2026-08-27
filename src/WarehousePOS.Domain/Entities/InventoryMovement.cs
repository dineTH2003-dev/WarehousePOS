using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Domain.Entities;

/// <summary>
/// Records every change to product stock — the single source of truth for inventory.
/// Every stock change (sale, purchase receive, adjustment, return) MUST create one.
/// </summary>
public sealed class InventoryMovement : Entity
{
    private InventoryMovement() { }

    public int ProductId          { get; private set; }
    public Product Product        { get; private set; } = null!;
    public MovementType Type      { get; private set; }
    public int Quantity           { get; private set; }    // always positive; direction determined by Type
    public int QuantityBefore     { get; private set; }
    public int QuantityAfter      { get; private set; }
    public string? ReferenceId    { get; private set; }    // e.g. PurchaseId or SaleId
    public string? ReferenceType  { get; private set; }    // "Purchase", "Sale", "Adjustment"
    public string? Notes          { get; private set; }
    public int CreatedByUserId    { get; private set; }

    public static InventoryMovement Create(
        int productId,
        MovementType type,
        int quantity,
        int quantityBefore,
        int createdByUserId,
        string? referenceId   = null,
        string? referenceType = null,
        string? notes         = null)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        return new InventoryMovement
        {
            ProductId      = productId,
            Type           = type,
            Quantity       = quantity,
            QuantityBefore = quantityBefore,
            QuantityAfter  = type is MovementType.StockIn or MovementType.PurchaseReceive or MovementType.ReturnIn
                             ? quantityBefore + quantity
                             : quantityBefore - quantity,
            CreatedByUserId = createdByUserId,
            ReferenceId    = referenceId,
            ReferenceType  = referenceType,
            Notes          = notes?.Trim()
        };
    }
}
