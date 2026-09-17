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
    private readonly List<SalePayment> _payments = [];
    private Sale() { }

    public int?   CustomerId       { get; private set; }   // null = walk-in / ad-hoc
    public Customer? Customer      { get; private set; }
    public SaleType  SaleType      { get; private set; }
    public SaleStatus Status       { get; private set; } = SaleStatus.Completed;
    public decimal SubTotal        { get; private set; }   // before discount
    public decimal DiscountAmount  { get; private set; }
    public decimal DeliveryFee     { get; private set; }
    public decimal TotalAmount     { get; private set; }   // (SubTotal - Discount) + DeliveryFee
    public decimal AmountPaid      { get; private set; }
    public decimal Change          => Math.Max(0m, AmountPaid - TotalAmount);
    public decimal UnpaidAmount    => Math.Max(0m, TotalAmount - AmountPaid);
    public PaymentMethod PaymentMethod { get; private set; } = PaymentMethod.Cash;
    public string? Notes           { get; private set; }
    public string? CustomerName    { get; private set; }
    public string? CustomerPhone   { get; private set; }
    public string? DeliveryAddress { get; private set; }
    public DateTime SaleDate       { get; private set; }
    public int CreatedByUserId     { get; private set; }

    public IReadOnlyList<SaleItem> Items => _items;
    public IReadOnlyList<SalePayment> Payments => _payments;

    public static Sale Create(
        SaleType saleType,
        int createdByUserId,
        int? customerId = null,
        string? notes   = null,
        PaymentMethod paymentMethod = PaymentMethod.Cash,
        decimal deliveryFee = 0,
        string? customerName = null,
        string? customerPhone = null,
        string? deliveryAddress = null)
    {
        if (deliveryFee < 0)
            throw new ArgumentOutOfRangeException(nameof(deliveryFee), "Delivery fee cannot be negative.");

        return new Sale
        {
            SaleType        = saleType,
            CustomerId      = customerId,
            CreatedByUserId = createdByUserId,
            Notes           = notes?.Trim(),
            SaleDate        = DateTime.UtcNow,
            PaymentMethod   = paymentMethod,
            DeliveryFee     = deliveryFee,
            CustomerName    = customerName?.Trim(),
            CustomerPhone   = customerPhone?.Trim(),
            DeliveryAddress = deliveryAddress?.Trim()
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
        RecalculateTotals();
    }

    public void SetDeliveryDetails(decimal deliveryFee, string? customerName = null, string? customerPhone = null, string? deliveryAddress = null)
    {
        if (deliveryFee < 0)
            throw new ArgumentOutOfRangeException(nameof(deliveryFee), "Delivery fee cannot be negative.");

        DeliveryFee = deliveryFee;
        if (customerName != null) CustomerName = customerName.Trim();
        if (customerPhone != null) CustomerPhone = customerPhone.Trim();
        if (deliveryAddress != null) DeliveryAddress = deliveryAddress.Trim();
        RecalculateTotals();
    }

    public void RecordPayment(decimal amountPaid, bool isRegisteredCustomer = false, bool isAdvancePayment = false, int userId = 1, string? notes = null)
    {
        if (amountPaid < 0)
            throw new ArgumentOutOfRangeException(nameof(amountPaid), "Amount paid cannot be negative.");

        if (!isRegisteredCustomer && !isAdvancePayment && amountPaid < TotalAmount)
            throw new BusinessRuleViolationException("InsufficientPayment",
                $"Unregistered customers must pay in full. Amount paid (Rs. {amountPaid:N2}) is less than total (Rs. {TotalAmount:N2}).");

        AmountPaid = amountPaid;
        if (amountPaid > 0)
        {
            _payments.Add(SalePayment.Create(Id, Math.Min(amountPaid, TotalAmount), PaymentMethod, userId, notes ?? (isAdvancePayment ? "Advance Payment" : "Initial Payment")));
        }

        if (isAdvancePayment && AmountPaid < TotalAmount)
        {
            Status = SaleStatus.AdvancePaid;
        }
        else
        {
            Status = SaleStatus.Completed;
        }
        SetUpdatedAt();
    }

    public void RecordAdditionalPayment(decimal amount, PaymentMethod paymentMethod, int userId, string? notes = null)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
        if (Status == SaleStatus.Cancelled)
            throw new BusinessRuleViolationException("CancelledSale", "Cannot record payment on a cancelled sale.");

        var remaining = UnpaidAmount;
        if (amount > remaining)
            throw new BusinessRuleViolationException("Overpayment", $"Payment amount (Rs. {amount:N2}) cannot exceed remaining balance (Rs. {remaining:N2}).");

        AmountPaid += amount;
        _payments.Add(SalePayment.Create(Id, amount, paymentMethod, userId, notes ?? "Balance Settlement"));

        if (AmountPaid >= TotalAmount && Status == SaleStatus.AdvancePaid)
        {
            Status = SaleStatus.Completed;
        }
        SetUpdatedAt();
    }

    public void ProcessReturn(int productId, int quantity)
    {
        if (Status == SaleStatus.Cancelled)
            throw new BusinessRuleViolationException("CancelledSale", "Cannot return items from a cancelled sale.");

        var item = _items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new BusinessRuleViolationException("ItemNotFound", $"Product ID {productId} is not on invoice #{Id}.");

        item.RecordReturn(quantity);

        if (_items.All(i => i.ReturnedQuantity == i.Quantity))
        {
            Status = SaleStatus.Returned;
        }
        else
        {
            Status = SaleStatus.PartiallyReturned;
        }
        SetUpdatedAt();
    }

    public void AdjustBillDetails(decimal? newDeliveryFee, string? newNotes, string? customerName, string? customerPhone, string? address)
    {
        if (Status == SaleStatus.Cancelled)
            throw new BusinessRuleViolationException("CancelledSale", "Cannot adjust a cancelled sale.");

        if (newDeliveryFee.HasValue)
        {
            if (newDeliveryFee.Value < 0) throw new ArgumentOutOfRangeException(nameof(newDeliveryFee));
            DeliveryFee = newDeliveryFee.Value;
        }
        if (newNotes != null) Notes = newNotes.Trim();
        if (customerName != null) CustomerName = customerName.Trim();
        if (customerPhone != null) CustomerPhone = customerPhone.Trim();
        if (address != null) DeliveryAddress = address.Trim();

        RecalculateTotals();
        SetUpdatedAt();
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
        SubTotal    = _items.Sum(i => i.LineTotal);
        TotalAmount = Math.Max(0m, SubTotal - DiscountAmount) + DeliveryFee;
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
    public int ClaimedQuantity { get; private set; }
    public int ReturnedQuantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Discount  { get; private set; }
    public decimal LineTotal => (UnitPrice * Quantity) - Discount;

    public int UnclaimedQuantity => Quantity - ClaimedQuantity;
    public int ReturnableQuantity => Quantity - ReturnedQuantity;

    internal static SaleItem Create(int productId, int quantity, decimal unitPrice, decimal discount) =>
        new() { ProductId = productId, Quantity = quantity, UnitPrice = unitPrice, Discount = discount };

    public void RecordClaim(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Claim quantity must be positive.");

        if (ClaimedQuantity + quantity > Quantity)
            throw new BusinessRuleViolationException("ExcessiveClaim", $"Cannot claim {quantity} units because only {UnclaimedQuantity} units remain unclaimed on this invoice line.");

        ClaimedQuantity += quantity;
    }

    public void RecordReturn(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Return quantity must be positive.");

        if (ReturnedQuantity + quantity > Quantity)
            throw new BusinessRuleViolationException("ExcessiveReturn", $"Cannot return {quantity} units because only {ReturnableQuantity} units remain on this line.");

        ReturnedQuantity += quantity;
    }
}
