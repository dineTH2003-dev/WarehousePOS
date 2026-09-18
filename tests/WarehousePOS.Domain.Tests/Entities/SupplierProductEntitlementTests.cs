using FluentAssertions;
using WarehousePOS.Domain.Entities;
using Xunit;

namespace WarehousePOS.Domain.Tests.Entities;

public class SupplierProductEntitlementTests
{
    [Fact]
    public void Create_ValidParameters_ShouldInstantiateSuccessfully()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var nextDate = now.AddDays(30);

        // Act
        var entitlement = SupplierProductEntitlement.Create(
            supplierId: 1,
            productId: 10,
            nature: "Receiving Free Item",
            eventDate: now,
            quantity: 5,
            value: null,
            nextEntitlementDate: nextDate,
            specialNotes: "Bonus offer"
        );

        // Assert
        entitlement.SupplierId.Should().Be(1);
        entitlement.ProductId.Should().Be(10);
        entitlement.Nature.Should().Be("Receiving Free Item");
        entitlement.Quantity.Should().Be(5);
        entitlement.Value.Should().BeNull();
        entitlement.EventDate.Should().Be(now);
        entitlement.NextEntitlementDate.Should().Be(nextDate);
        entitlement.SpecialNotes.Should().Be("Bonus offer");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidSupplierId_ShouldThrowArgumentOutOfRangeException(int supplierId)
    {
        var act = () => SupplierProductEntitlement.Create(supplierId, 1, "Nature", DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidProductId_ShouldThrowArgumentOutOfRangeException(int productId)
    {
        var act = () => SupplierProductEntitlement.Create(1, productId, "Nature", DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_NegativeQuantity_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => SupplierProductEntitlement.Create(1, 1, "Nature", DateTime.UtcNow, quantity: -5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_NegativeValue_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => SupplierProductEntitlement.Create(1, 1, "Nature", DateTime.UtcNow, value: -100m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
