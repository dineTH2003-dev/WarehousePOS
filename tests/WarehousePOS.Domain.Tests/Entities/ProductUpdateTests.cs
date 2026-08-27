using FluentAssertions;
using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Domain.Tests.Entities;

public sealed class ProductUpdateTests
{
    [Fact]
    public void UpdateDetails_ValidData_ShouldUpdateFields()
    {
        var product = Product.Create("Old Name", "SKU001", 100, 80, 1);
        product.UpdateDetails("New Name", "1234567890", "New desc", 2, 10);

        product.Name.Should().Be("New Name");
        product.Barcode.Should().Be("1234567890");
        product.Description.Should().Be("New desc");
        product.CategoryId.Should().Be(2);
        product.ReorderLevel.Should().Be(10);
    }

    [Fact]
    public void UpdateDetails_EmptyName_ShouldThrow()
    {
        var product = Product.Create("Test", "SKU001", 100, 80, 1);
        Action act = () => product.UpdateDetails("", null, null, 1, 5);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdatePricing_NegativePrice_ShouldThrow()
    {
        var product = Product.Create("Test", "SKU001", 100, 80, 1);
        Action act = () => product.UpdatePricing(-1, 80);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
