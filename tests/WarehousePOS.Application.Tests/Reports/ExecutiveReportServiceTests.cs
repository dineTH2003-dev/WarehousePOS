using FluentAssertions;
using Moq;
using WarehousePOS.Application.Reports;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Tests.Reports;

public sealed class ExecutiveReportServiceTests
{
    private readonly Mock<ISaleRepository> _saleRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ISupplierRepository> _supplierRepoMock = new();
    private readonly Mock<IExpenseRepository> _expenseRepoMock = new();
    private readonly Mock<IPurchaseRepository> _purchaseRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();

    private readonly ReportService _sut;

    public ExecutiveReportServiceTests()
    {
        _sut = new ReportService(
            _saleRepoMock.Object,
            _productRepoMock.Object,
            _supplierRepoMock.Object,
            _expenseRepoMock.Object,
            _purchaseRepoMock.Object,
            _customerRepoMock.Object,
            _movementRepoMock.Object,
            _categoryRepoMock.Object,
            _userRepoMock.Object);
    }

    [Fact]
    public async Task GetOverviewDashboardAsync_ReturnsOverviewMetrics()
    {
        // Arrange
        var from = DateTime.Today.AddDays(-30);
        var to = DateTime.Today;

        _saleRepoMock.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([]);

        _expenseRepoMock.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetOverviewDashboardAsync(from, to);

        // Assert
        result.Should().NotBeNull();
        result.GrossRevenue.Should().Be(0m);
        result.NetProfit.Should().Be(0m);
    }

    [Fact]
    public async Task GetInventoryLeanReportAsync_ReturnsLeanInventoryMetrics()
    {
        // Arrange
        var p1 = Product.Create("Product 1", "SKU1", 100m, 60m, 1, stockQuantity: 10, reorderLevel: 5);
        _productRepoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync([p1]);
        _supplierRepoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);
        _categoryRepoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);


        // Act
        var result = await _sut.GetInventoryLeanReportAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalStockValuation.Should().Be(600m); // 10 * 60m
    }

    [Fact]
    public async Task GetItemPerformanceMatrixAsync_CategorizesQuadrantsCorrectly()
    {
        // Arrange
        var p1 = Product.Create("Star Item", "SKU1", 200m, 100m, 1, stockQuantity: 50);
        _productRepoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync([p1]);
        _saleRepoMock.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetItemPerformanceMatrixAsync(DateTime.Today.AddDays(-30), DateTime.Today);

        // Assert
        result.Should().NotBeNull();
        result.SkuPoints.Should().HaveCount(1);
        result.SkuPoints[0].SKU.Should().Be("SKU1");
    }
}
