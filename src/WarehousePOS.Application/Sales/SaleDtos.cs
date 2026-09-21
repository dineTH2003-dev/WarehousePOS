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
    bool IsActive,
    decimal OutstandingBalance = 0m)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Phone) ? Name : $"{Name} ({Phone})";
};

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

public sealed record SalePaymentDto(
    int Id,
    int SaleId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string PaymentMethodLabel,
    DateTime PaymentDate,
    int CashierUserId,
    string? Notes);

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
    int WarrantyDays = 0,
    int ClaimedQuantity = 0,
    int ReturnedQuantity = 0,
    int ReturnableQuantity = 0);

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
    IReadOnlyList<SaleItemDto> Items,
    PaymentMethod PaymentMethod = PaymentMethod.Cash,
    decimal DeliveryFee = 0,
    string? CustomerPhone = null,
    string? DeliveryAddress = null,
    decimal UnpaidAmount = 0,
    IReadOnlyList<SalePaymentDto>? Payments = null);

public sealed record CreateSaleRequest(
    SaleType SaleType,
    int CreatedByUserId,
    int? CustomerId,
    decimal DiscountAmount,
    decimal AmountPaid,
    string? Notes,
    IReadOnlyList<CreateSaleItemRequest> Items,
    PaymentMethod PaymentMethod = PaymentMethod.Cash,
    decimal DeliveryFee = 0,
    string? CustomerName = null,
    string? CustomerPhone = null,
    string? DeliveryAddress = null,
    bool IsAdvancePayment = false,
    bool SaveAsNewCustomer = false);

public sealed record CreateSaleItemRequest(
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal Discount = 0);

public sealed record RecordSalePaymentRequest(
    int SaleId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    int CashierUserId,
    string? Notes = null);

public sealed record ReturnItemRequest(
    int ProductId,
    int Quantity,
    string? Reason = null);

public sealed record ProcessSaleReturnRequest(
    int SaleId,
    int CashierUserId,
    IReadOnlyList<ReturnItemRequest> Items,
    bool RefundCash = true,
    string? Notes = null);

public sealed record AdjustSaleRequest(
    int SaleId,
    int AdminUserId,
    string Reason,
    decimal? DeliveryFee = null,
    string? Notes = null,
    string? CustomerName = null,
    string? CustomerPhone = null,
    string? DeliveryAddress = null);

public sealed record SaleSearchCriteria(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SearchTerm = null,
    SaleStatus? Status = null,
    PaymentMethod? PaymentMethod = null,
    SaleType? SaleType = null);

public sealed record ProcessCustomerClaimRequest(
    int ProductId,
    int? CustomerId = null,
    int? SaleId = null,
    int Quantity = 1,
    decimal ClaimAmount = 0m,
    string Reason = "",
    int UserId = 1);

