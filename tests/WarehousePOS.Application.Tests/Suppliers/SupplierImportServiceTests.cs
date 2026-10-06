using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WarehousePOS.Application.Suppliers;
using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Interfaces;
using Xunit;

namespace WarehousePOS.Application.Tests.Suppliers;

public sealed class SupplierImportServiceTests
{
    private readonly Mock<ISupplierRepository> _supplierRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly List<Supplier> _savedSuppliers = [];
    private readonly List<Product> _savedProducts = [];
    private readonly List<Category> _savedCategories = [];

    private readonly SupplierImportService _sut;

    public SupplierImportServiceTests()
    {
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _supplierRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_savedSuppliers);

        _supplierRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .Callback<Supplier, CancellationToken>((sup, _) =>
            {
                typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(sup, _savedSuppliers.Count + 1);
                _savedSuppliers.Add(sup);
            })
            .Returns(Task.CompletedTask);

        _supplierRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _productRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_savedProducts);

        _productRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((prod, _) =>
            {
                typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(prod, _savedProducts.Count + 1);
                _savedProducts.Add(prod);
            })
            .Returns(Task.CompletedTask);

        var generalCategory = Category.Create("General", "Default category");
        typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(generalCategory, 1);
        _savedCategories.Add(generalCategory);

        _categoryRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_savedCategories);

        _sut = new SupplierImportService(
            _supplierRepoMock.Object,
            _productRepoMock.Object,
            _categoryRepoMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<SupplierImportService>.Instance);
    }

    [Fact]
    public async Task ImportCsvAsync_ValidCsvWithProvidedProducts_SuccessfullyCreatesSuppliersAndAutoCreatesProducts()
    {
        // Arrange
        string csv = @"Supplier Name,Contact Person,Phone,Email,Address,Provided Products,Opening Balance
Ceylon Beverage Distributors,Kamal Perera,0712345678,kamal@ceylonbev.lk,123 Galle Road Colombo,Coca Cola 1.5L; Sprite 1.5L; Fanta 1.5L,1500.00
Lanka Stationery,Nimal Silva,0779876543,sales@lanka.com,45 Kandy Road,Atlas CR Book; Ballpoint Pen,0.00";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new SupplierImportOptions(UpdateExisting: true, AutoCreateMissingProducts: true);

        // Act
        var result = await _sut.ImportCsvAsync(stream, options);

        // Assert
        result.TotalRows.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.ErrorCount.Should().Be(0);
        result.Errors.Should().BeEmpty();

        _savedSuppliers.Should().HaveCount(2);
        var bevSupplier = _savedSuppliers.First(s => s.Name == "Ceylon Beverage Distributors");
        bevSupplier.ContactPerson.Should().Be("Kamal Perera");
        bevSupplier.Phone.Should().Be("0712345678");
        bevSupplier.Balance.Should().Be(1500.00m);
        bevSupplier.ProvidedProducts.Should().Contain("Coca Cola 1.5L");
        bevSupplier.ProvidedProducts.Should().Contain("Sprite 1.5L");

        // Verify auto-created products in catalog
        _savedProducts.Should().HaveCount(5);
        _savedProducts.Select(p => p.Name).Should().Contain(["Coca Cola 1.5L", "Sprite 1.5L", "Fanta 1.5L", "Atlas CR Book", "Ballpoint Pen"]);
    }

    [Fact]
    public async Task ImportCsvAsync_MultipleRowsForSameSupplier_MergesProvidedProducts()
    {
        // Arrange: Same supplier listed on multiple rows with different items
        string csv = @"Supplier Name,Contact Person,Phone,Email,Address,Provided Products
Metro Hardware,Sunil Fernando,0711112233,sunil@metro.lk,Colombo,Hammer Steel 500g
Metro Hardware,Sunil Fernando,0711112233,sunil@metro.lk,Colombo,Measuring Tape 5m
Metro Hardware,Sunil Fernando,0711112233,sunil@metro.lk,Colombo,Screwdriver Set";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new SupplierImportOptions(UpdateExisting: true, AutoCreateMissingProducts: true);

        // Act
        var result = await _sut.ImportCsvAsync(stream, options);

        // Assert
        result.TotalRows.Should().Be(3);
        result.SuccessCount.Should().Be(1); // Merged into 1 supplier
        result.ErrorCount.Should().Be(0);

        _savedSuppliers.Should().HaveCount(1);
        var supplier = _savedSuppliers[0];
        supplier.Name.Should().Be("Metro Hardware");
        supplier.ProvidedProducts.Should().Contain("Hammer Steel 500g");
        supplier.ProvidedProducts.Should().Contain("Measuring Tape 5m");
        supplier.ProvidedProducts.Should().Contain("Screwdriver Set");
    }

    [Fact]
    public async Task ImportCsvAsync_ExistingSupplier_UpdatesContactAndAppendsProductsWhenEnabled()
    {
        // Arrange: Supplier already in database
        var existing = Supplier.Create("ABC Agro", "Old Contact", "0710000000", "old@abc.lk", "Galle", "Fertilizer A");
        _savedSuppliers.Add(existing);

        string csv = @"Supplier Name,Contact Person,Phone,Email,Address,Provided Products
ABC Agro,New Manager,0771234567,manager@abc.lk,Matara,Pesticide B; Fertilizer A";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new SupplierImportOptions(UpdateExisting: true, AutoCreateMissingProducts: false);

        // Act
        var result = await _sut.ImportCsvAsync(stream, options);

        // Assert
        result.SuccessCount.Should().Be(0);
        result.UpdatedCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);

        existing.ContactPerson.Should().Be("New Manager");
        existing.Phone.Should().Be("0771234567");
        existing.Email.Should().Be("manager@abc.lk");
        existing.Address.Should().Be("Matara");
        existing.ProvidedProducts.Should().Contain("Fertilizer A");
        existing.ProvidedProducts.Should().Contain("Pesticide B");
    }

    [Fact]
    public async Task ImportCsvAsync_ExistingSupplier_SkipsWhenUpdateExistingFalse()
    {
        // Arrange
        var existing = Supplier.Create("XYZ Traders", "Old Contact", "0710000000", null, null, null);
        _savedSuppliers.Add(existing);

        string csv = @"Supplier Name,Contact Person,Phone,Provided Products
XYZ Traders,New Contact,0771112222,New Item";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new SupplierImportOptions(UpdateExisting: false, AutoCreateMissingProducts: false);

        // Act
        var result = await _sut.ImportCsvAsync(stream, options);

        // Assert
        result.SkippedCount.Should().Be(1);
        result.UpdatedCount.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        existing.ContactPerson.Should().Be("Old Contact"); // Untouched
    }

    [Fact]
    public async Task ImportCsvAsync_InvalidPhone_RecordsErrorAndSkipsRow()
    {
        // Arrange: Phone does not have 10 digits or start with 0
        string csv = @"Supplier Name,Phone
Valid Supplier,0712345678
Invalid Phone Supplier,12345";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new SupplierImportOptions();

        // Act
        var result = await _sut.ImportCsvAsync(stream, options);

        // Assert
        result.TotalRows.Should().Be(2);
        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.RowNumber == 3 && e.Reason.Contains("Phone number must consist of 10 digits starting with '0'"));
    }

    [Fact]
    public async Task ImportCsvAsync_InvalidEmail_RecordsErrorAndSkipsRow()
    {
        // Arrange: Email is malformed
        string csv = @"Supplier Name,Email
Bad Email Supplier,not-an-email";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new SupplierImportOptions();

        // Act
        var result = await _sut.ImportCsvAsync(stream, options);

        // Assert
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.RowNumber == 2 && e.Reason.Contains("Invalid email address"));
        _savedSuppliers.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportCsvAsync_EmptyCsv_ReturnsError()
    {
        using var stream = new MemoryStream(""u8.ToArray());
        var options = new SupplierImportOptions();

        var result = await _sut.ImportCsvAsync(stream, options);

        result.ErrorCount.Should().Be(1);
        result.SummaryMessage.Should().Contain("empty");
    }

    [Fact]
    public async Task ImportCsvAsync_MissingSupplierNameHeader_ReturnsHeaderError()
    {
        string csv = @"Contact Person,Phone,Email
John Doe,0712345678,john@test.com";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new SupplierImportOptions();

        var result = await _sut.ImportCsvAsync(stream, options);

        result.ErrorCount.Should().Be(1);
        result.SummaryMessage.Should().Contain("missing the required 'Supplier Name' column");
    }

    [Fact]
    public void GenerateSampleTemplateCsv_ReturnsValidUtf8CsvWithHeaders()
    {
        // Act
        var bytes = _sut.GenerateSampleTemplateCsv();
        string content = Encoding.UTF8.GetString(bytes);

        // Assert
        content.Should().StartWith("\uFEFFSupplier Name,Contact Person,Phone,Email,Address,Provided Products,Opening Balance");
        content.Should().Contain("Ceylon Beverage Distributors");
        content.Should().Contain("Coca Cola 1.5L; Sprite 1.5L; Fanta 1.5L");
    }
}
