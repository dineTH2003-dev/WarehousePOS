using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Application.Purchasing;

// ── Purchase DTOs ─────────────────────────────────────────────────────────────

public sealed record PurchaseItemDto(
    int ProductId,
    string ProductName,
    string SKU,
    int Quantity,
    int FreeQuantity,
    decimal UnitCost,
    decimal TotalCost,
    decimal RetailPrice = 0,
    decimal WholesalePrice = 0,
    int ClaimedQuantityReceived = 0);

public sealed record PurchaseDto(
    int Id,
    int SupplierId,
    string SupplierName,
    PurchaseStatus Status,
    string StatusLabel,
    decimal TotalAmount,
    string? Notes,
    DateTime PurchaseDate,
    DateTime? ReceivedDate,
    IReadOnlyList<PurchaseItemDto> Items,
    string PaymentMethod = "Cash",
    decimal PaidAmount = 0,
    decimal RemainingBalance = 0,
    string? PaymentDetails = null);

public sealed record CreatePurchaseRequest(
    int SupplierId,
    int CreatedByUserId,
    string? Notes,
    IReadOnlyList<CreatePurchaseItemRequest> Items,
    string PaymentMethod = "Cash",
    decimal PaidAmount = 0,
    string? PaymentDetails = null);

public sealed record CreatePurchaseItemRequest(
    int ProductId,
    int Quantity,
    decimal UnitCost,
    int FreeQuantity = 0,
    decimal RetailPrice = 0,
    decimal WholesalePrice = 0,
    int ClaimedQuantityReceived = 0);

// ── Inventory DTOs ────────────────────────────────────────────────────────────

public sealed record InventoryMovementDto(
    int ProductId,
    string ProductName,
    MovementType Type,
    string TypeLabel,
    int Quantity,
    int QuantityBefore,
    int QuantityAfter,
    string? ReferenceId,
    string? Notes,
    DateTime CreatedAt);

public sealed record StockAdjustmentRequest(
    int ProductId,
    MovementType Type,
    int Quantity,
    string Notes,
    int AdjustedByUserId);

public sealed record StockLevelDto(
    int ProductId,
    string Name,
    string SKU,
    string CategoryName,
    int StockQuantity,
    int ReorderLevel,
    bool IsLowStock,
    decimal StockValue);
