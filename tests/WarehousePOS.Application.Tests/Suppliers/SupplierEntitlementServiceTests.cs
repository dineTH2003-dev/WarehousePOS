using FluentAssertions;
using Moq;
using WarehousePOS.Application.Suppliers;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;
using Xunit;

namespace WarehousePOS.Application.Tests.Suppliers;

public class SupplierEntitlementServiceTests
{
    private readonly Mock<ISupplierEntitlementRepository> _entitlementRepoMock = new();
    private readonly Mock<ISupplierRepository> _supplierRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly SupplierEntitlementService _service;

    public SupplierEntitlementServiceTests()
    {
        _service = new SupplierEntitlementService(
            _entitlementRepoMock.Object,
            _supplierRepoMock.Object,
            _productRepoMock.Object);
    }

    [Fact]
    public async Task CreateEntitlementAsync_ValidDto_ShouldAddAndReturnDto()
    {
        // Arrange
        var supplier = Supplier.Create("Test Supplier", "John", "0771234567");
        var product = Product.Create("Test Product", "SKU123", 100, 80, 1);

        _supplierRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);
        _productRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var dto = new CreateSupplierEntitlementDto(
            SupplierId: 1,
            ProductId: 10,
            Nature: "Obtaining Discount",
            EventDate: DateTime.Today,
            Quantity: null,
            Value: 500m,
            NextEntitlementDate: DateTime.Today.AddDays(30),
            SpecialNotes: "Special summer discount"
        );

        // Act
        var result = await _service.CreateEntitlementAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.SupplierId.Should().Be(1);
        result.ProductId.Should().Be(10);
        result.Nature.Should().Be("Obtaining Discount");
        result.Value.Should().Be(500m);
        result.DisplayQuantityOrValue.Should().Be("Rs. 500.00");
        _entitlementRepoMock.Verify(r => r.AddAsync(It.IsAny<SupplierProductEntitlement>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateEntitlementAsync_ImminentNextDate_ShouldMarkIsImminentTrue()
    {
        // Arrange
        var supplier = Supplier.Create("Test Supplier", "John", "0771234567");
        var product = Product.Create("Test Product", "SKU123", 100, 80, 1);

        _supplierRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _productRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var dto = new CreateSupplierEntitlementDto(
            SupplierId: 1,
            ProductId: 10,
            Nature: "Receiving Free Item",
            EventDate: DateTime.Today,
            NextEntitlementDate: DateTime.Today.AddDays(3)
        );

        // Act
        var result = await _service.CreateEntitlementAsync(dto);

        // Assert
        result.IsImminent.Should().BeTrue();
        result.NextEntitlementDisplay.Should().Contain("Imminent");
    }

    [Fact]
    public async Task CreateEntitlementAsync_NonExistentSupplier_ShouldThrowEntityNotFoundException()
    {
        _supplierRepoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        var dto = new CreateSupplierEntitlementDto(99, 10, "Nature", DateTime.Today);

        var act = () => _service.CreateEntitlementAsync(dto);
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}
