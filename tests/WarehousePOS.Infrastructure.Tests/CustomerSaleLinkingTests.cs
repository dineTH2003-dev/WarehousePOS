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

public sealed class CustomerSaleLinkingTests
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
    public async Task GetByCustomerAsync_WithMatchingPhoneOrDuplicateAccounts_ReturnsAllSales()
    {
        using var db = CreateDbContext();

        var category = Category.Create("Test Cat");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var product = Product.Create("Item 1", "SKU1", 1000m, 900m, category.Id, stockQuantity: 50);
        db.Products.Add(product);

        var primaryCustomer = Customer.Create("B24", SaleType.Wholesale, phone: "0122548788");
        var duplicateCustomer = Customer.Create("B24-Duplicate", SaleType.Retail, phone: "0122548788");
        var otherCustomer = Customer.Create("Other", SaleType.Retail, phone: "0770000000");
        db.Customers.AddRange(primaryCustomer, duplicateCustomer, otherCustomer);
        await db.SaveChangesAsync();

        // 1. Sale linked directly to primary customer
        var sale1 = Sale.Create(SaleType.Wholesale, 1, primaryCustomer.Id, customerPhone: primaryCustomer.Phone);
        sale1.AddItem(product, 1, 1000m);
        sale1.RecordPayment(1000m, isRegisteredCustomer: true, isAdvancePayment: false);

        // 2. Walk-in sale saved without customerId, but with matching phone
        var sale2 = Sale.Create(SaleType.Wholesale, 1, customerId: null, customerName: "B24", customerPhone: "0122548788");
        sale2.AddItem(product, 2, 1000m);
        sale2.RecordPayment(2000m, isRegisteredCustomer: false, isAdvancePayment: false);

        // 3. Sale linked to duplicate customer account
        var sale3 = Sale.Create(SaleType.Retail, 1, duplicateCustomer.Id, customerPhone: duplicateCustomer.Phone);
        sale3.AddItem(product, 1, 1000m);
        sale3.RecordPayment(1000m, isRegisteredCustomer: true, isAdvancePayment: false);

        // 4. Sale for completely different customer
        var saleOther = Sale.Create(SaleType.Retail, 1, otherCustomer.Id, customerPhone: otherCustomer.Phone);
        saleOther.AddItem(product, 1, 1000m);
        saleOther.RecordPayment(1000m, isRegisteredCustomer: true, isAdvancePayment: false);

        db.Sales.AddRange(sale1, sale2, sale3, saleOther);
        await db.SaveChangesAsync();

        var saleRepo = new SaleRepository(db);

        // Act
        var results = await saleRepo.GetByCustomerAsync(primaryCustomer.Id);

        // Assert
        results.Should().HaveCount(3);
        results.Select(s => s.Id).Should().Contain([sale1.Id, sale2.Id, sale3.Id]);
        results.Select(s => s.Id).Should().NotContain(saleOther.Id);
    }

    [Fact]
    public async Task ProcessSaleAsync_WithExistingCustomerPhone_LinksToExistingCustomerAndPreventsDuplicates()
    {
        using var db = CreateDbContext();

        var category = Category.Create("Test Cat");
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var product = Product.Create("Item 1", "SKU1", 1000m, 900m, category.Id, stockQuantity: 50);
        db.Products.Add(product);

        var existingCustomer = Customer.Create("B24", SaleType.Wholesale, phone: "0122548788");
        db.Customers.Add(existingCustomer);
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

        var req = new CreateSaleRequest(
            SaleType.Wholesale,
            CreatedByUserId: 1,
            CustomerId: null, // Cashier didn't provide CustomerId directly
            DiscountAmount: 0m,
            AmountPaid: 1000m,
            Notes: "Walk-in with phone",
            Items: [new CreateSaleItemRequest(product.Id, 1, 1000m)],
            PaymentMethod: PaymentMethod.Cash,
            CustomerName: "B24",
            CustomerPhone: "0122548788",
            SaveAsNewCustomer: true); // Checkbox was checked

        // Act
        var result = await service.ProcessSaleAsync(req);

        // Assert
        result.CustomerId.Should().Be(existingCustomer.Id);
        (await db.Customers.CountAsync()).Should().Be(1, "should not create a duplicate customer record");
    }
}
