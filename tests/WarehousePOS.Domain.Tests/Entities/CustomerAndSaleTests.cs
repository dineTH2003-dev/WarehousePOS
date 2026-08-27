using FluentAssertions;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Exceptions;

namespace WarehousePOS.Domain.Tests.Entities;

public sealed class CustomerTests
{
    [Fact]
    public void Create_ValidName_ShouldCreateRetailCustomer()
    {
        var c = Customer.Create("Sunil Perera", phone: "0771112223");
        c.Name.Should().Be("Sunil Perera");
        c.Type.Should().Be(SaleType.Retail);
        c.Phone.Should().Be("0771112223");
        c.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WholesaleType_ShouldSetType()
    {
        var c = Customer.Create("Lanka Stores", type: SaleType.Wholesale);
        c.Type.Should().Be(SaleType.Wholesale);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_InvalidName_ShouldThrow(string? name)
    {
        var action = () => Customer.Create(name!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var c = Customer.Create("Test");
        c.Deactivate();
        c.IsActive.Should().BeFalse();
    }
}

public sealed class SaleTests
{
    private static Product CreateTestProduct(int id, string name, decimal retail, decimal wholesale, int stock = 100)
    {
        var category = Category.Create("General");
        var product = Product.Create(name, $"SKU-{id}", retail, wholesale, category.Id);
        return product;
    }

    [Fact]
    public void Create_ShouldBeInCompletedStatus()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        sale.Status.Should().Be(SaleStatus.Completed);
        sale.SaleType.Should().Be(SaleType.Retail);
        sale.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_ValidProduct_ShouldCalculateLineTotal()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        var product = CreateTestProduct(1, "Rice 1kg", retail: 220, wholesale: 200);

        sale.AddItem(product, quantity: 5, unitPrice: 220);

        sale.Items.Should().HaveCount(1);
        sale.SubTotal.Should().Be(1100);
        sale.TotalAmount.Should().Be(1100);
    }

    [Fact]
    public void AddItem_WithItemDiscount_ShouldSubtractFromLineTotal()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        var product = CreateTestProduct(1, "Sugar 1kg", retail: 250, wholesale: 230);

        sale.AddItem(product, quantity: 2, unitPrice: 250, itemDiscount: 50);

        sale.SubTotal.Should().Be(450); // (250*2) - 50 = 450
        sale.TotalAmount.Should().Be(450);
    }

    [Fact]
    public void ApplyDiscount_ValidAmount_ShouldReduceTotal()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        var product = CreateTestProduct(1, "Tea 500g", retail: 600, wholesale: 550);

        sale.AddItem(product, quantity: 2, unitPrice: 600); // SubTotal 1200
        sale.ApplyDiscount(100);

        sale.DiscountAmount.Should().Be(100);
        sale.TotalAmount.Should().Be(1100);
    }

    [Fact]
    public void ApplyDiscount_ExcessiveDiscount_ShouldThrow()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        var product = CreateTestProduct(1, "Tea 500g", retail: 600, wholesale: 550);
        sale.AddItem(product, quantity: 1, unitPrice: 600);

        var action = () => sale.ApplyDiscount(700);
        action.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void RecordPayment_ValidPayment_ShouldCalculateChange()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        var product = CreateTestProduct(1, "Item", retail: 100, wholesale: 90);
        sale.AddItem(product, quantity: 3, unitPrice: 100); // 300

        sale.RecordPayment(500);

        sale.AmountPaid.Should().Be(500);
        sale.Change.Should().Be(200);
    }

    [Fact]
    public void RecordPayment_InsufficientPayment_ShouldThrow()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        var product = CreateTestProduct(1, "Item", retail: 100, wholesale: 90);
        sale.AddItem(product, quantity: 3, unitPrice: 100); // 300

        var action = () => sale.RecordPayment(250);
        action.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Cancel_ShouldSetStatusToCancelled()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        sale.Cancel();
        sale.Status.Should().Be(SaleStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ShouldThrow()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        sale.Cancel();

        var action = () => sale.Cancel();
        action.Should().Throw<BusinessRuleViolationException>();
    }
}
