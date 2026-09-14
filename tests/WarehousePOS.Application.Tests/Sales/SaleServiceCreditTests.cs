using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WarehousePOS.Application.Sales;
using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Tests.Sales;

public sealed class SaleServiceCreditTests
{
    private readonly Mock<ISaleRepository> _saleRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public SaleServiceCreditTests()
    {
        _unitOfWorkMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());
    }

    [Fact]
    public async Task ProcessSaleAsync_RegisteredCustomerCreditPurchase_ShouldIncreaseCustomerOutstandingBalance()
    {
        // Arrange
        var customer = Customer.Create("Sunil Stores", SaleType.Wholesale, phone: "0771234567");
        _customerRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var category = Category.Create("General");
        var product = Product.Create("Spring Mattress", "SPM-1", 50000m, 45000m, category.Id);
        product.AddStock(10);
        _productRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var service = new SaleService(_saleRepoMock.Object, _productRepoMock.Object, _customerRepoMock.Object, _movementRepoMock.Object, _unitOfWorkMock.Object, NullLogger<SaleService>.Instance);

        var req = new CreateSaleRequest(
            SaleType.Wholesale,
            CreatedByUserId: 1,
            CustomerId: 10,
            DiscountAmount: 0m,
            AmountPaid: 20000m, // Total is 45,000, Paid 20,000 => Credit 25,000
            Notes: "Credit Sale Test",
            Items: new[] { new CreateSaleItemRequest(1, 1, 45000m, 0m) },
            PaymentMethod: PaymentMethod.Card);

        // Act
        var result = await service.ProcessSaleAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.AmountPaid.Should().Be(20000m);
        result.PaymentMethod.Should().Be(PaymentMethod.Card);
        customer.OutstandingBalance.Should().Be(25000m);
        _customerRepoMock.Verify(r => r.UpdateAsync(customer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessSaleAsync_WalkInCustomerInsufficientPayment_ShouldThrow()
    {
        var category = Category.Create("General");
        var product = Product.Create("Spring Mattress", "SPM-1", 50000m, 45000m, category.Id);
        product.AddStock(10);
        _productRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var service = new SaleService(_saleRepoMock.Object, _productRepoMock.Object, _customerRepoMock.Object, _movementRepoMock.Object, _unitOfWorkMock.Object, NullLogger<SaleService>.Instance);

        var req = new CreateSaleRequest(
            SaleType.Retail,
            CreatedByUserId: 1,
            CustomerId: null, // Walk-in
            DiscountAmount: 0m,
            AmountPaid: 20000m, // Total 50,000, Paid 20,000
            Notes: "Walkin Test",
            Items: new[] { new CreateSaleItemRequest(1, 1, 50000m, 0m) },
            PaymentMethod: PaymentMethod.Cash);

        var action = async () => await service.ProcessSaleAsync(req);
        (await action.Should().ThrowAsync<BusinessRuleViolationException>())
            .WithMessage("*Unregistered customers must pay in full*");
    }
}
