using FluentAssertions;
using Moq;
using WarehousePOS.Application.Reports;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Tests.Reports;

public sealed class ReportServiceTests
{
    private readonly Mock<ISaleRepository> _saleRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ISupplierRepository> _supplierRepoMock = new();
    private readonly Mock<IExpenseRepository> _expenseRepoMock = new();
    private readonly Mock<IPurchaseRepository> _purchaseRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();

    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _sut = new ReportService(
            _saleRepoMock.Object,
            _productRepoMock.Object,
            _supplierRepoMock.Object,
            _expenseRepoMock.Object,
            _purchaseRepoMock.Object,
            _customerRepoMock.Object,
            _movementRepoMock.Object,
            _categoryRepoMock.Object);
    }

    [Fact]
    public async Task GetGeneralAnalyticsAsync_ReturnsCorrectCalculations()
    {
        // Arrange
        var from = DateTime.Today.AddDays(-7);
        var to = DateTime.Today;

        var product = Product.Create("Widget A", "SKU001", 100m, 60m, 1, stockQuantity: 50);
        var sale = Sale.Create(SaleType.Retail, 1);
        sale.AddItem(product, 2, 100m, 0m); // SubTotal = 200
        sale.ApplyDiscount(10m);             // Net = 190
        sale.RecordPayment(190m);

        _saleRepoMock
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([sale]);

        _productRepoMock
            .Setup(r => r.GetAllAsync(default))
            .ReturnsAsync([product]);

        _expenseRepoMock
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetGeneralAnalyticsAsync(from, to);

        // Assert
        result.Should().NotBeNull();
        result.TotalRevenue.Should().Be(200m);
        result.TotalDiscounts.Should().Be(10m);
        result.NetRevenue.Should().Be(190m);
        result.TotalTransactions.Should().Be(1);
        result.AverageOrderValue.Should().Be(190m);
        result.CostOfGoodsSold.Should().Be(120m); // 2 * 60m
        result.NetProfit.Should().Be(70m);       // 190 - 120 - 0
    }

    [Fact]
    public async Task GetStockValuationReportAsync_ReturnsValuationMetrics()
    {
        // Arrange
        var p1 = Product.Create("Item 1", "SKU1", 150m, 100m, 1, stockQuantity: 10, reorderLevel: 5);
        var p2 = Product.Create("Item 2", "SKU2", 200m, 120m, 1, stockQuantity: 2, reorderLevel: 5);
        var p3 = Product.Create("Item 3", "SKU3", 50m, 30m, 1, stockQuantity: 0, reorderLevel: 5);

        _productRepoMock
            .Setup(r => r.GetAllAsync(default))
            .ReturnsAsync([p1, p2, p3]);

        // Act
        var result = await _sut.GetStockValuationReportAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalActiveProducts.Should().Be(3);
        result.TotalQuantityInStock.Should().Be(12);
        result.TotalCostValue.Should().Be(1240m); // 10*100 + 2*120 + 0
        result.TotalRetailValuation.Should().Be(1900m); // 10*150 + 2*200 + 0
        result.PotentialProfitMargin.Should().Be(660m);
        result.LowStockCount.Should().Be(1); // p2 (2 <= 5)
        result.OutOfStockCount.Should().Be(1); // p3 (0 <= 0)
    }

    [Fact]
    public async Task GetLowStockReportAsync_ReturnsOnlyLowAndOutOfStockItems()
    {
        // Arrange
        var p1 = Product.Create("In Stock", "SKU1", 100m, 50m, 1, stockQuantity: 20, reorderLevel: 5);
        var p2 = Product.Create("Low Stock", "SKU2", 100m, 50m, 1, stockQuantity: 3, reorderLevel: 5);

        _productRepoMock
            .Setup(r => r.GetAllAsync(default))
            .ReturnsAsync([p1, p2]);

        // Act
        var lowStock = await _sut.GetLowStockReportAsync();

        // Assert
        lowStock.Should().HaveCount(1);
        lowStock[0].SKU.Should().Be("SKU2");
        lowStock[0].Status.Should().Be("Low Stock");
    }

    [Fact]
    public async Task GetGrnReportAsync_FiltersBySupplierCorrectly()
    {
        // Arrange
        var p1 = Purchase.Create(1, 1);
        p1.AddItem(1, 5, 50m);
        p1.Confirm(); p1.Receive();

        var p2 = Purchase.Create(2, 1);
        p2.AddItem(2, 5, 50m);
        p2.Confirm(); p2.Receive();

        _purchaseRepoMock
            .Setup(r => r.GetAllAsync(default))
            .ReturnsAsync([p1, p2]);

        // Act
        var result = await _sut.GetGrnReportAsync(DateTime.Today.AddDays(-30), DateTime.Today, supplierId: 1);

        // Assert
        result.Should().NotBeNull();
        result.TotalGrnsCount.Should().Be(1);
        result.Items[0].SupplierId.Should().Be(1);
    }

    [Fact]
    public async Task GetGeneralAnalyticsAsync_WhenFromDateAfterToDate_ShouldHandleEmptyDateRange()
    {
        // Arrange
        var from = DateTime.Today.AddDays(7);
        var to = DateTime.Today;

        _saleRepoMock
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([]);

        _expenseRepoMock
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([]);

        _productRepoMock
            .Setup(r => r.GetAllAsync(default))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetGeneralAnalyticsAsync(from, to);

        // Assert
        result.Should().NotBeNull();
        result.TotalRevenue.Should().Be(0m);
        result.TotalTransactions.Should().Be(0);
    }
}
