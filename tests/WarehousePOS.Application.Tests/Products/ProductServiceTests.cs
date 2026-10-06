using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WarehousePOS.Application.Products;
using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;
using Xunit;

namespace WarehousePOS.Application.Tests.Products;

public sealed class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepoMock = new();
    private readonly Mock<ISupplierRepository> _supplierRepoMock = new();

    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(
            _productRepoMock.Object,
            _categoryRepoMock.Object,
            _movementRepoMock.Object,
            _supplierRepoMock.Object,
            NullLogger<ProductService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_DuplicateBarcode_ThrowsBusinessRuleViolationException()
    {
        var category = Category.Create("Electronics");
        typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(category, 1);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var existingProduct = Product.Create("Existing Product", "SKU-EXIST", 100m, 80m, 1, barcode: "123456789012");
        _productRepoMock.Setup(r => r.GetByBarcodeAsync("123456789012", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        var request = new CreateProductRequest(
            Name: "New Product",
            SKU: "SKU-NEW",
            Barcode: "123456789012",
            Description: "Duplicate barcode test",
            CategoryId: 1,
            RetailPrice: 150m,
            WholesalePrice: 120m,
            StockQuantity: 10,
            ReorderLevel: 2,
            UpdatedByUserId: 1);

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*already assigned*");
    }

    [Fact]
    public async Task UpdateAsync_DuplicateBarcodeOnDifferentProduct_ThrowsBusinessRuleViolationException()
    {
        var category = Category.Create("Electronics");
        typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(category, 1);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var currentProduct = Product.Create("Product To Update", "SKU-CURRENT", 100m, 80m, 1, barcode: "111111111111");
        typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(currentProduct, 10);
        _productRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(currentProduct);

        var otherProduct = Product.Create("Other Product", "SKU-OTHER", 200m, 160m, 1, barcode: "999999999999");
        typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(otherProduct, 20);
        _productRepoMock.Setup(r => r.GetByBarcodeAsync("999999999999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherProduct);

        var updateReq = new UpdateProductRequest(
            Id: 10,
            Name: "Product To Update",
            SKU: "SKU-CURRENT",
            Barcode: "999999999999", // collides with product ID 20
            Description: "Barcode collision test",
            CategoryId: 1,
            RetailPrice: 100m,
            WholesalePrice: 80m,
            StockQuantity: 10,
            ReorderLevel: 2,
            UpdatedByUserId: 1);

        var act = () => _sut.UpdateAsync(updateReq);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*already assigned*");
    }

    [Fact]
    public async Task UpdateAsync_SameBarcodeOnSameProduct_Succeeds()
    {
        var category = Category.Create("Electronics");
        typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(category, 1);
        _categoryRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var currentProduct = Product.Create("Product To Update", "SKU-CURRENT", 100m, 80m, 1, barcode: "111111111111");
        typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(currentProduct, 10);
        _productRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(currentProduct);
        _productRepoMock.Setup(r => r.GetByBarcodeAsync("111111111111", It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentProduct);

        var updateReq = new UpdateProductRequest(
            Id: 10,
            Name: "Product To Update",
            SKU: "SKU-CURRENT",
            Barcode: "111111111111", // same barcode on same product
            Description: "Same barcode test",
            CategoryId: 1,
            RetailPrice: 110m,
            WholesalePrice: 90m,
            StockQuantity: 10,
            ReorderLevel: 2,
            UpdatedByUserId: 1);

        var result = await _sut.UpdateAsync(updateReq);

        result.Should().NotBeNull();
        result.RetailPrice.Should().Be(110m);
        _productRepoMock.Verify(r => r.UpdateAsync(currentProduct, It.IsAny<CancellationToken>()), Times.Once);
    }
}
