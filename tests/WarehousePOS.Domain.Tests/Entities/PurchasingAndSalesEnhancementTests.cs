using FluentAssertions;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Exceptions;
using Xunit;

namespace WarehousePOS.Domain.Tests.Entities;

public sealed class PurchasingAndSalesEnhancementTests
{
    private static Product CreateProduct(int id, string name, decimal retail, decimal wholesale, int stock = 100)
    {
        var p = Product.Create(name, $"SKU-{id}", retail, wholesale, categoryId: 1, stockQuantity: stock);
        typeof(WarehousePOS.Domain.Common.Entity).GetProperty(nameof(WarehousePOS.Domain.Common.Entity.Id))?.SetValue(p, id);
        return p;
    }

    [Fact]
    public void Purchase_ApplyDiscount_ShouldRecalculateTotals()
    {
        var purchase = Purchase.Create(supplierId: 1, createdByUserId: 1);
        var product = CreateProduct(1, "Item A", 100, 80);

        purchase.AddItem(1, quantity: 10, unitCost: 50); // SubTotal: 500
        purchase.SubTotal.Should().Be(500);
        purchase.TotalAmount.Should().Be(500);

        purchase.ApplyDiscount(50);
        purchase.DiscountAmount.Should().Be(50);
        purchase.TotalAmount.Should().Be(450);
    }

    [Fact]
    public void Purchase_ApplyDiscount_ExcessiveDiscount_ShouldThrow()
    {
        var purchase = Purchase.Create(supplierId: 1, createdByUserId: 1);
        purchase.AddItem(1, quantity: 2, unitCost: 50); // SubTotal: 100

        var action = () => purchase.ApplyDiscount(150);
        action.Should().Throw<BusinessRuleViolationException>().WithMessage("*Discount cannot exceed purchase sub-total*");
    }

    [Fact]
    public void Sale_SetDeliveryDetails_ShouldUpdateFieldsAndRecalculateTotal()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        var product = CreateProduct(1, "Item A", 100, 80);
        sale.AddItem(product, quantity: 2, unitPrice: 100); // SubTotal: 200

        sale.SetDeliveryDetails(50, "John Doe", "0771234567", "123 Colombo Road");

        sale.DeliveryFee.Should().Be(50);
        sale.CustomerName.Should().Be("John Doe");
        sale.CustomerPhone.Should().Be("0771234567");
        sale.DeliveryAddress.Should().Be("123 Colombo Road");
        sale.TotalAmount.Should().Be(250); // 200 + 50
    }

    [Fact]
    public void Sale_AdvancePayment_ShouldSetStatusAdvancePaidAndTrackBalance()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        var product = CreateProduct(1, "Item A", 100, 80);
        sale.AddItem(product, quantity: 10, unitPrice: 100); // Total: 1000

        sale.RecordPayment(300, isRegisteredCustomer: false, isAdvancePayment: true, userId: 1);

        sale.Status.Should().Be(SaleStatus.AdvancePaid);
        sale.AmountPaid.Should().Be(300);
        sale.UnpaidAmount.Should().Be(700);
        sale.Payments.Should().HaveCount(1);
        sale.Payments[0].Amount.Should().Be(300);
    }

    [Fact]
    public void Sale_RecordAdditionalPayment_ShouldSettleAdvanceBill()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        var product = CreateProduct(1, "Item A", 100, 80);
        sale.AddItem(product, quantity: 5, unitPrice: 100); // Total: 500

        sale.RecordPayment(200, isRegisteredCustomer: false, isAdvancePayment: true, userId: 1);
        sale.Status.Should().Be(SaleStatus.AdvancePaid);
        sale.UnpaidAmount.Should().Be(300);

        // Record installment 1: 150
        sale.RecordAdditionalPayment(150, PaymentMethod.Cash, userId: 1, "Installment 1");
        sale.AmountPaid.Should().Be(350);
        sale.UnpaidAmount.Should().Be(150);
        sale.Status.Should().Be(SaleStatus.AdvancePaid);

        // Record installment 2: 150 -> Settle
        sale.RecordAdditionalPayment(150, PaymentMethod.BankTransfer, userId: 1, "Final Settlement");
        sale.AmountPaid.Should().Be(500);
        sale.UnpaidAmount.Should().Be(0);
        sale.Status.Should().Be(SaleStatus.Completed);
        sale.Payments.Should().HaveCount(3);
    }

    [Fact]
    public void Sale_RecordAdditionalPayment_Overpayment_ShouldThrow()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        var product = CreateProduct(1, "Item A", 100, 80);
        sale.AddItem(product, quantity: 2, unitPrice: 100); // Total: 200
        sale.RecordPayment(100, isRegisteredCustomer: false, isAdvancePayment: true);

        var action = () => sale.RecordAdditionalPayment(150, PaymentMethod.Cash, 1);
        action.Should().Throw<BusinessRuleViolationException>().WithMessage("*cannot exceed remaining balance*");
    }

    [Fact]
    public void Sale_ProcessReturn_PartialAndFull_ShouldUpdateStatusAndQuantities()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        var productA = CreateProduct(1, "Item A", 100, 80);
        var productB = CreateProduct(2, "Item B", 200, 180);
        sale.AddItem(productA, quantity: 4, unitPrice: 100); // 400
        sale.AddItem(productB, quantity: 2, unitPrice: 200); // 400 -> Total: 800
        sale.RecordPayment(800);

        // Partial return: 2 of Item A
        sale.ProcessReturn(productA.Id, 2);
        sale.Status.Should().Be(SaleStatus.PartiallyReturned);
        sale.Items.First(i => i.ProductId == productA.Id).ReturnedQuantity.Should().Be(2);

        // Return remaining: 2 of Item A, 2 of Item B -> Fully returned
        sale.ProcessReturn(productA.Id, 2);
        sale.ProcessReturn(productB.Id, 2);
        sale.Status.Should().Be(SaleStatus.Returned);
    }

    [Fact]
    public void Sale_AdjustBillDetails_ShouldUpdateDetailsAndAudit()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        var product = CreateProduct(1, "Item A", 100, 80);
        sale.AddItem(product, quantity: 2, unitPrice: 100); // 200
        sale.RecordPayment(200);

        sale.AdjustBillDetails(newDeliveryFee: 75, newNotes: "Deliver after 5 PM", customerName: "Jane", customerPhone: "0719876543", address: "Kandy");

        sale.DeliveryFee.Should().Be(75);
        sale.TotalAmount.Should().Be(275);
        sale.CustomerName.Should().Be("Jane");
        sale.CustomerPhone.Should().Be("0719876543");
        sale.DeliveryAddress.Should().Be("Kandy");
        sale.Notes.Should().Be("Deliver after 5 PM");
    }
}
