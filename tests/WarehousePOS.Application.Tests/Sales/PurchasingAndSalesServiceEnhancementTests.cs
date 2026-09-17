using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WarehousePOS.Application.Expenses;
using WarehousePOS.Application.Sales;
using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Interfaces;
using Xunit;

namespace WarehousePOS.Application.Tests.Sales;

public sealed class PurchasingAndSalesServiceEnhancementTests
{
    private readonly Mock<ISaleRepository> _saleRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public PurchasingAndSalesServiceEnhancementTests()
    {
        _unitOfWorkMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());
    }

    private static Product CreateTestProduct(int id, string name, decimal retail, decimal wholesale, int stock = 20)
    {
        var p = Product.Create(name, $"SKU-{id}", retail, wholesale, 1, stockQuantity: stock);
        typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(p, id);
        return p;
    }

    [Fact]
    public async Task ProcessSaleAsync_WithDeliveryFeeAndCustomCustomer_ShouldSaveDetails()
    {
        var product = CreateTestProduct(1, "Fan", 5000m, 4500m, stock: 10);
        _productRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var service = new SaleService(
            _saleRepoMock.Object,
            _productRepoMock.Object,
            _customerRepoMock.Object,
            _movementRepoMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<SaleService>.Instance);

        var req = new CreateSaleRequest(
            SaleType.Retail,
            CreatedByUserId: 1,
            CustomerId: null,
            DiscountAmount: 0m,
            AmountPaid: 5500m,
            Notes: "Delivery test",
            Items: new[] { new CreateSaleItemRequest(1, 1, 5000m, 0m) },
            PaymentMethod: PaymentMethod.Cash,
            DeliveryFee: 500m,
            CustomerName: "Kamal Perera",
            CustomerPhone: "0777123456",
            DeliveryAddress: "45 Galle Road, Mount Lavinia",
            IsAdvancePayment: false,
            SaveAsNewCustomer: false);

        var saleDto = await service.ProcessSaleAsync(req);

        saleDto.TotalAmount.Should().Be(5500m);
        saleDto.DeliveryFee.Should().Be(500m);
        saleDto.CustomerName.Should().Be("Kamal Perera");
        saleDto.CustomerPhone.Should().Be("0777123456");
        saleDto.DeliveryAddress.Should().Be("45 Galle Road, Mount Lavinia");
        _saleRepoMock.Verify(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessSaleAsync_AdvancePayment_ShouldDeductStockAndSetStatusAdvancePaid()
    {
        var product = CreateTestProduct(1, "Dining Table", 30000m, 25000m, stock: 5);
        _productRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var service = new SaleService(
            _saleRepoMock.Object,
            _productRepoMock.Object,
            _customerRepoMock.Object,
            _movementRepoMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<SaleService>.Instance);

        var req = new CreateSaleRequest(
            SaleType.Retail,
            CreatedByUserId: 1,
            CustomerId: null,
            DiscountAmount: 0m,
            AmountPaid: 10000m, // Advance 10,000 of 30,000
            Notes: "Advance payment layaway order",
            Items: new[] { new CreateSaleItemRequest(1, 1, 30000m, 0m) },
            PaymentMethod: PaymentMethod.Cash,
            DeliveryFee: 0m,
            CustomerName: "Nimal Silva",
            CustomerPhone: "0712345678",
            IsAdvancePayment: true);

        var saleDto = await service.ProcessSaleAsync(req);

        saleDto.Status.Should().Be(SaleStatus.AdvancePaid);
        saleDto.AmountPaid.Should().Be(10000m);
        saleDto.UnpaidAmount.Should().Be(20000m);
        product.StockQuantity.Should().Be(4); // Inventory immediately deducted to reserve item
        _movementRepoMock.Verify(r => r.AddAsync(It.Is<InventoryMovement>(m => m.Type == MovementType.StockOut), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessSaleAsync_SaveAsNewCustomer_ShouldCreateCustomerInDirectory()
    {
        var product = CreateTestProduct(1, "Cooker", 8000m, 7500m, stock: 10);
        _productRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _customerRepoMock.Setup(r => r.SearchAsync("0781234567", It.IsAny<CancellationToken>())).ReturnsAsync(new List<Customer>());

        var service = new SaleService(
            _saleRepoMock.Object,
            _productRepoMock.Object,
            _customerRepoMock.Object,
            _movementRepoMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<SaleService>.Instance);

        var req = new CreateSaleRequest(
            SaleType.Retail,
            CreatedByUserId: 1,
            CustomerId: null,
            DiscountAmount: 0m,
            AmountPaid: 8000m,
            Notes: "Walkin save",
            Items: new[] { new CreateSaleItemRequest(1, 1, 8000m, 0m) },
            PaymentMethod: PaymentMethod.Cash,
            CustomerName: "Sarath Fonseka",
            CustomerPhone: "0781234567",
            SaveAsNewCustomer: true);

        await service.ProcessSaleAsync(req);

        _customerRepoMock.Verify(r => r.AddAsync(It.Is<Customer>(c => c.Name == "Sarath Fonseka" && c.Phone == "0781234567"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordPaymentAsync_ShouldAddInstallmentAndReduceBalance()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(sale, 100);
        var product = CreateTestProduct(1, "Item", 1000m, 900m);
        sale.AddItem(product, 1, 1000m);
        sale.RecordPayment(400m, isRegisteredCustomer: false, isAdvancePayment: true);

        _saleRepoMock.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(sale);

        var service = new SaleService(
            _saleRepoMock.Object,
            _productRepoMock.Object,
            _customerRepoMock.Object,
            _movementRepoMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<SaleService>.Instance);

        var req = new RecordSalePaymentRequest(100, 600m, PaymentMethod.BankTransfer, CashierUserId: 1, Notes: "Settle final balance");
        var updated = await service.RecordPaymentAsync(req);

        updated.AmountPaid.Should().Be(1000m);
        updated.UnpaidAmount.Should().Be(0m);
        updated.Status.Should().Be(SaleStatus.Completed);
        _saleRepoMock.Verify(r => r.UpdateAsync(sale, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessReturnAsync_ShouldRestockInventoryAndRecordMovement()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(sale, 200);
        var product = CreateTestProduct(5, "Blender", 4000m, 3500m, stock: 10);
        sale.AddItem(product, 2, 4000m); // 8000
        sale.RecordPayment(8000m);

        _saleRepoMock.Setup(r => r.GetByIdAsync(200, It.IsAny<CancellationToken>())).ReturnsAsync(sale);
        _productRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var service = new SaleService(
            _saleRepoMock.Object,
            _productRepoMock.Object,
            _customerRepoMock.Object,
            _movementRepoMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<SaleService>.Instance);

        var req = new ProcessSaleReturnRequest(
            200,
            CashierUserId: 1,
            Items: new[] { new ReturnItemRequest(5, 1, "Wrong model") },
            RefundCash: true,
            Notes: "Return 1 unit");

        var updated = await service.ProcessReturnAsync(req);

        product.StockQuantity.Should().Be(11); // Restocked +1
        updated.Status.Should().Be(SaleStatus.PartiallyReturned);
        _movementRepoMock.Verify(r => r.AddAsync(It.Is<InventoryMovement>(m => m.Type == MovementType.ReturnIn && m.Quantity == 1), It.IsAny<CancellationToken>()), Times.Once);
        _saleRepoMock.Verify(r => r.UpdateAsync(sale, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdjustSaleAsync_AdminCanAmendDeliveryAndDetails()
    {
        var sale = Sale.Create(SaleType.Retail, createdByUserId: 1);
        typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(sale, 300);
        var product = CreateTestProduct(1, "Item", 1000m, 900m);
        sale.AddItem(product, 1, 1000m);
        sale.RecordPayment(1000m);

        _saleRepoMock.Setup(r => r.GetByIdAsync(300, It.IsAny<CancellationToken>())).ReturnsAsync(sale);

        var service = new SaleService(
            _saleRepoMock.Object,
            _productRepoMock.Object,
            _customerRepoMock.Object,
            _movementRepoMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<SaleService>.Instance);

        var req = new AdjustSaleRequest(
            SaleId: 300,
            AdminUserId: 1,
            Reason: "Delivery added post-sale",
            DeliveryFee: 150m,
            Notes: "Deliver Saturday",
            CustomerName: "Kasun",
            CustomerPhone: "0770001111",
            DeliveryAddress: "Negombo");

        var updated = await service.AdjustSaleAsync(req);

        updated.DeliveryFee.Should().Be(150m);
        updated.TotalAmount.Should().Be(1150m);
        updated.CustomerName.Should().Be("Kasun");
        updated.CustomerPhone.Should().Be("0770001111");
        updated.DeliveryAddress.Should().Be("Negombo");
        _saleRepoMock.Verify(r => r.UpdateAsync(sale, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExpenseService_CreateLinkedWithSaleId_ShouldPersistSaleId()
    {
        var expenseRepoMock = new Mock<IExpenseRepository>();
        var userRepoMock = new Mock<IUserRepository>();
        var category = ExpenseCategory.Create("Transport & Fuel");
        typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(category, 2);

        expenseRepoMock.Setup(r => r.GetCategoryByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var service = new ExpenseService(expenseRepoMock.Object, userRepoMock.Object, NullLogger<ExpenseService>.Instance);

        var req = new CreateExpenseRequest(
            CategoryId: 2,
            Amount: 1200m,
            Description: "Special courier delivery for Invoice #50",
            RecordedByUserId: 1,
            SaleId: 50);

        var created = await service.CreateAsync(req);

        created.SaleId.Should().Be(50);
        created.Amount.Should().Be(1200m);
        expenseRepoMock.Verify(r => r.AddAsync(It.Is<Expense>(e => e.SaleId == 50 && e.Amount == 1200m), It.IsAny<CancellationToken>()), Times.Once);
    }
}
