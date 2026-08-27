using FluentAssertions;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Exceptions;

namespace WarehousePOS.Domain.Tests.Entities;

public sealed class SupplierTests
{
    [Fact]
    public void Create_ValidName_ShouldCreateSupplier()
    {
        var s = Supplier.Create("ABC Supplies", phone: "0771234567");
        s.Name.Should().Be("ABC Supplies");
        s.Phone.Should().Be("0771234567");
        s.Balance.Should().Be(0);
        s.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_EmptyName_ShouldThrow(string? name) =>
        ((Action)(() => Supplier.Create(name!))).Should().Throw<ArgumentException>();

    [Fact]
    public void AddToBalance_ShouldIncrease()
    {
        var s = Supplier.Create("Test");
        s.AddToBalance(500);
        s.Balance.Should().Be(500);
    }

    [Fact]
    public void ReduceBalance_ShouldDecrease()
    {
        var s = Supplier.Create("Test");
        s.AddToBalance(1000);
        s.ReduceBalance(300);
        s.Balance.Should().Be(700);
    }

    [Fact]
    public void AddToBalance_NegativeAmount_ShouldThrow() =>
        ((Action)(() => Supplier.Create("Test").AddToBalance(-1))).Should().Throw<ArgumentOutOfRangeException>();
}

public sealed class PurchaseTests
{
    [Fact]
    public void Create_ValidData_ShouldBeInDraftStatus()
    {
        var purchase = Purchase.Create(1, 1, "Test purchase");
        purchase.Status.Should().Be(Domain.Enums.PurchaseStatus.Draft);
        purchase.Items.Should().BeEmpty();
        purchase.TotalAmount.Should().Be(0);
    }

    [Fact]
    public void AddItem_ToDraft_ShouldAddItem()
    {
        var purchase = Purchase.Create(1, 1);
        purchase.AddItem(1, 10, 50);
        purchase.Items.Should().HaveCount(1);
        purchase.TotalAmount.Should().Be(500);
    }

    [Fact]
    public void AddItem_SameProduct_ShouldReplace()
    {
        var purchase = Purchase.Create(1, 1);
        purchase.AddItem(1, 5, 50);
        purchase.AddItem(1, 10, 60); // replaces
        purchase.Items.Should().HaveCount(1);
        purchase.Items[0].Quantity.Should().Be(10);
    }

    [Fact]
    public void Confirm_EmptyPurchase_ShouldThrow()
    {
        var purchase = Purchase.Create(1, 1);
        purchase.Invoking(p => p.Confirm())
                .Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Confirm_WithItems_ShouldBeConfirmed()
    {
        var purchase = Purchase.Create(1, 1);
        purchase.AddItem(1, 5, 100);
        purchase.Confirm();
        purchase.Status.Should().Be(Domain.Enums.PurchaseStatus.Confirmed);
    }

    [Fact]
    public void Receive_ConfirmedPurchase_ShouldBeReceived()
    {
        var purchase = Purchase.Create(1, 1);
        purchase.AddItem(1, 5, 100);
        purchase.Confirm();
        purchase.Receive();
        purchase.Status.Should().Be(Domain.Enums.PurchaseStatus.Received);
        purchase.ReceivedDate.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_ReceivedPurchase_ShouldThrow()
    {
        var purchase = Purchase.Create(1, 1);
        purchase.AddItem(1, 5, 100);
        purchase.Confirm();
        purchase.Receive();
        purchase.Invoking(p => p.Cancel())
                .Should().Throw<BusinessRuleViolationException>();
    }
}
