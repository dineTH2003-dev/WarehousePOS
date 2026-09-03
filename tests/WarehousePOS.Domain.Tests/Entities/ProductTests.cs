using FluentAssertions;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Exceptions;

namespace WarehousePOS.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the Product entity.
/// These tests verify domain invariants and business rules.
/// </summary>
public sealed class ProductTests
{
    [Fact]
    public void Create_ValidData_ShouldCreateProduct()
    {
        // Arrange & Act
        var product = Product.Create(
            name: "A4 Ream Paper",
            sku: "PRP-A4-500",
            retailPrice: 950.00m,
            wholesalePrice: 800.00m,
            categoryId: 1);

        // Assert
        product.Name.Should().Be("A4 Ream Paper");
        product.SKU.Should().Be("PRP-A4-500");
        product.RetailPrice.Should().Be(950.00m);
        product.WholesalePrice.Should().Be(800.00m);
        product.IsActive.Should().BeTrue();
        product.StockQuantity.Should().Be(0);
    }

    [Fact]
    public void Create_WithInitialStock_ShouldSetStockQuantity()
    {
        var product = Product.Create("Test", "SKU001", 100, 80, 1, stockQuantity: 25);

        product.StockQuantity.Should().Be(25);
    }

    [Fact]
    public void Create_WithNegativeStock_ShouldThrow()
    {
        Action act = () => Product.Create("Test", "SKU001", 100, 80, 1, stockQuantity: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetStockQuantity_NegativeQuantity_ShouldThrow()
    {
        var product = Product.Create("Test", "SKU001", 100, 80, 1);

        Action act = () => product.SetStockQuantity(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_EmptyName_ShouldThrow(string? name)
    {
        // Act
        Action act = () => Product.Create(name!, "SKU001", 100, 80, 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NegativeRetailPrice_ShouldThrow()
    {
        Action act = () => Product.Create("Test", "SKU001", -1, 80, 1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddStock_PositiveQuantity_ShouldIncreaseStock()
    {
        var product = Product.Create("Test", "SKU001", 100, 80, 1);

        product.AddStock(50);

        product.StockQuantity.Should().Be(50);
    }

    [Fact]
    public void DeductStock_SufficientStock_ShouldDecreaseStock()
    {
        var product = Product.Create("Test", "SKU001", 100, 80, 1);
        product.AddStock(100);

        product.DeductStock(30);

        product.StockQuantity.Should().Be(70);
    }

    [Fact]
    public void DeductStock_InsufficientStock_ShouldThrowInsufficientStockException()
    {
        var product = Product.Create("Test Product", "SKU001", 100, 80, 1);
        product.AddStock(10);

        Action act = () => product.DeductStock(50);

        act.Should().Throw<InsufficientStockException>()
           .WithMessage("*Test Product*");
    }

    [Fact]
    public void Deactivate_ActiveProduct_ShouldSetIsActiveFalse()
    {
        var product = Product.Create("Test", "SKU001", 100, 80, 1);

        product.Deactivate();

        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsLowStock_StockAtOrBelowReorderLevel_ShouldBeTrue()
    {
        var product = Product.Create("Test", "SKU001", 100, 80, 1, reorderLevel: 5);
        product.AddStock(5); // exactly at reorder level

        product.IsLowStock.Should().BeTrue();
    }

    [Fact]
    public void IsLowStock_StockAboveReorderLevel_ShouldBeFalse()
    {
        var product = Product.Create("Test", "SKU001", 100, 80, 1, reorderLevel: 5);
        product.AddStock(10);

        product.IsLowStock.Should().BeFalse();
    }
}
