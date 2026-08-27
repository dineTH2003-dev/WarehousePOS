using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Application.Sales;

// ── Customer DTOs ─────────────────────────────────────────────────────────────

public sealed record CustomerDto(
    int Id,
    string Name,
    SaleType Type,
    string TypeLabel,
    string? Phone,
    string? Email,
    string? Address,
    bool IsActive);

public sealed record CreateCustomerRequest(
    string Name,
    SaleType Type = SaleType.Retail,
    string? Phone = null,
    string? Email = null,
    string? Address = null);

public sealed record UpdateCustomerRequest(
    int Id,
    string Name,
    SaleType Type,
    string? Phone = null,
    string? Email = null,
    string? Address = null);

// ── Sale DTOs ─────────────────────────────────────────────────────────────────

public sealed record SaleItemDto(
    int ProductId,
    string ProductName,
    string SKU,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal LineTotal);

public sealed record SaleDto(
    int Id,
    int? CustomerId,
    string CustomerName,
    SaleType SaleType,
    string SaleTypeLabel,
    SaleStatus Status,
    string StatusLabel,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal Change,
    string? Notes,
    DateTime SaleDate,
    IReadOnlyList<SaleItemDto> Items);

public sealed record CreateSaleRequest(
    SaleType SaleType,
    int CreatedByUserId,
    int? CustomerId,
    decimal DiscountAmount,
    decimal AmountPaid,
    string? Notes,
    IReadOnlyList<CreateSaleItemRequest> Items);

public sealed record CreateSaleItemRequest(
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal Discount = 0);
