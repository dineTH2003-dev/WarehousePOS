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
    decimal DiscountRate,
    bool IsActive);

public sealed record CreateCustomerRequest(
    string Name,
    SaleType Type = SaleType.Retail,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    decimal DiscountRate = 0m);

public sealed record UpdateCustomerRequest(
    int Id,
    string Name,
    SaleType Type,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    decimal DiscountRate = 0m);

// ── Sale DTOs ─────────────────────────────────────────────────────────────────

public sealed record SaleItemDto(
    int ProductId,
    string ProductName,
    string SKU,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal LineTotal,
    int WarrantyYears = 0,
    int WarrantyMonths = 0,
    int WarrantyDays = 0);

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
