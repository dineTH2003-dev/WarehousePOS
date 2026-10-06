namespace WarehousePOS.Application.Suppliers;

public record SupplierProductEntitlementDto(
    int Id,
    int SupplierId,
    string SupplierName,
    int ProductId,
    string ProductName,
    string ProductCode,
    string Nature,
    int? Quantity,
    decimal? Value,
    string DisplayQuantityOrValue,
    DateTime EventDate,
    DateTime? NextEntitlementDate,
    string NextEntitlementDisplay,
    bool IsImminent,
    string? SpecialNotes,
    DateTime CreatedAt
);

public record CreateSupplierEntitlementDto(
    int SupplierId,
    int ProductId,
    string Nature,
    DateTime EventDate,
    int? Quantity = null,
    decimal? Value = null,
    DateTime? NextEntitlementDate = null,
    string? SpecialNotes = null
);
