using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WarehousePOS.Application.Sales;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Infrastructure.Persistence;
using WarehousePOS.Infrastructure.Repositories;
using Xunit;

namespace WarehousePOS.Infrastructure.Tests;

public sealed class SalePaymentIntegrationTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RecordPayment_WithCustomer_ShouldUpdateSuccessfully()
    {
        using var db = CreateDbContext();

        var category = Category.Create("Test Cat");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var product = Product.Create("Item 1", "SKU1", 1000m, 900m, category.Id, stockQuantity: 10);
        db.Products.Add(product);

        var customer = Customer.Create("John Doe", SaleType.Retail, phone: "0771234567");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var sale = Sale.Create(SaleType.Retail, 1, customer.Id);
        sale.AddItem(product, 1, 1000m);
        sale.RecordPayment(400m, isRegisteredCustomer: true, isAdvancePayment: true);
        db.Sales.Add(sale);
        await db.SaveChangesAsync();

        var saleRepo = new SaleRepository(db);
        var customerRepo = new CustomerRepository(db);
        var productRepo = new ProductRepository(db);
        var movementRepo = new InventoryMovementRepository(db);
        var uow = new UnitOfWork(db);

        var service = new SaleService(
            saleRepo,
            productRepo,
            customerRepo,
            movementRepo,
            uow,
            NullLogger<SaleService>.Instance);

        var req = new RecordSalePaymentRequest(sale.Id, 600m, PaymentMethod.Cash, 1, "Settlement");
        var result = await service.RecordPaymentAsync(req);

        result.AmountPaid.Should().Be(1000m);
        result.UnpaidAmount.Should().Be(0m);
        result.Status.Should().Be(SaleStatus.Completed);
    }

    [Fact]
    public async Task RecordPayment_WalkInCustomer_ShouldUpdateSuccessfully()
    {
        using var db = CreateDbContext();

        var category = Category.Create("Test Cat");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var product = Product.Create("Item 1", "SKU1", 1000m, 900m, category.Id, stockQuantity: 10);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var sale = Sale.Create(SaleType.Retail, 1, null);
        sale.AddItem(product, 1, 1000m);
        sale.RecordPayment(400m, isRegisteredCustomer: false, isAdvancePayment: true);
        db.Sales.Add(sale);
        await db.SaveChangesAsync();

        var saleRepo = new SaleRepository(db);
        var customerRepo = new CustomerRepository(db);
        var productRepo = new ProductRepository(db);
        var movementRepo = new InventoryMovementRepository(db);
        var uow = new UnitOfWork(db);

        var service = new SaleService(
            saleRepo,
            productRepo,
            customerRepo,
            movementRepo,
            uow,
            NullLogger<SaleService>.Instance);

        var req = new RecordSalePaymentRequest(sale.Id, 600m, PaymentMethod.Cash, 1, "Settlement");
        var result = await service.RecordPaymentAsync(req);

        result.AmountPaid.Should().Be(1000m);
        result.UnpaidAmount.Should().Be(0m);
        result.Status.Should().Be(SaleStatus.Completed);
    }
}
