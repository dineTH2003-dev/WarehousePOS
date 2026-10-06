using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Domain.Entities;

/// <summary>
/// Represents a payment installment made towards a sale invoice.
/// Supports advance payments, partial settlements, and split payments.
/// </summary>
public sealed class SalePayment
{
    private SalePayment() { } // EF Core constructor

    public int Id { get; private set; }
    public int SaleId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public int CashierUserId { get; private set; }
    public string? Notes { get; private set; }

    public Sale Sale { get; private set; } = null!;

    internal static SalePayment Create(
        int saleId,
        decimal amount,
        PaymentMethod paymentMethod,
        int cashierUserId,
        string? notes = null)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");

        return new SalePayment
        {
            SaleId = saleId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            PaymentDate = DateTime.UtcNow,
            CashierUserId = cashierUserId,
            Notes = notes?.Trim()
        };
    }
}
