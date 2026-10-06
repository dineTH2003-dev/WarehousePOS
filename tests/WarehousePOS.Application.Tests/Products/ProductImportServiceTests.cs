using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WarehousePOS.Application.Products;
using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Interfaces;
using Xunit;

namespace WarehousePOS.Application.Tests.Products;

public sealed class ProductImportServiceTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly List<Category> _savedCategories = [];
    private readonly List<Product> _savedProducts = [];
    private readonly List<InventoryMovement> _savedMovements = [];

    private readonly ProductImportService _sut;

    public ProductImportServiceTests()
    {
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>(async (action, _) => await action());

        _categoryRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_savedCategories);

        _categoryRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((cat, _) =>
            {
                typeof(Entity).GetProperty(nameof(Entity.Id))?.SetValue(cat, _savedCategories.Count + 1);
                _savedCategories.Add(cat);
            })
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

        _movementRepoMock
            .Setup(r => r.AddAsync(It.IsAny<InventoryMovement>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryMovement, CancellationToken>((mov, _) => _savedMovements.Add(mov))
            .Returns(Task.CompletedTask);

        _sut = new ProductImportService(
            _productRepoMock.Object,
            _categoryRepoMock.Object,
            _movementRepoMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<ProductImportService>.Instance);
    }

    [Fact]
    public async Task ImportCsvAsync_UserFourColumnFormat_SuccessfullyImportsWithDefaults()
    {
        string csv = @"Product Name,SKU,Category,Supplier
Classic Foam Double Layer 72*36*4,CFDL72364,MATTRESS,Arpico
Kingstra Arm Chair,KVAC010,Plastic Chair,Kingstra
Rice Cooker 1.5KG,IRC229,Rice Cooker,DR Industreis PVT LTD";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new ProductImportOptions();

        var result = await _sut.ImportCsvAsync(stream, options);

        result.SuccessCount.Should().Be(3);
        result.ErrorCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        _savedProducts.Should().HaveCount(3);

        // Check defaults
        var first = _savedProducts.First(p => p.SKU == "CFDL72364");
        first.Name.Should().Be("Classic Foam Double Layer 72*36*4");
        first.RetailPrice.Should().Be(0m);
        first.WholesalePrice.Should().Be(0m);
        first.StockQuantity.Should().Be(0);
        first.ReorderLevel.Should().Be(5);

        // Check categories auto-created
        _savedCategories.Should().Contain(c => c.Name == "Mattress");
        _savedCategories.Should().Contain(c => c.Name == "Plastic Chair");
        _savedCategories.Should().Contain(c => c.Name == "Rice Cooker");
    }

    [Fact]
    public async Task ImportCsvAsync_DuplicateSkusWithDifferentNames_AutoDifferentiatesWithoutFailing()
    {
        // Like the Nilkamal chairs sharing factory model code
        string csv = @"Product Name,SKU,Category,Supplier
2003-EXECUTIVE CHAIR-HB-BLUE,A-01-10-01-014-410,Office Chair,Nilkamal
2003-EXECUTIVE CHAIR-HB-Black,A-01-10-01-014-410,Office Chair,Nilkamal
5101-EXECUTIVE CHAIR-HB-Blue,A-01-10-01-014-410,Office Chair,Nilkamal";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new ProductImportOptions();

        var result = await _sut.ImportCsvAsync(stream, options);

        result.SuccessCount.Should().Be(3);
        result.ErrorCount.Should().Be(0);
        _savedProducts.Should().HaveCount(3);

        var skus = _savedProducts.Select(p => p.SKU).ToList();
        skus.Should().OnlyHaveUniqueItems();
        skus.Should().Contain("A-01-10-01-014-410");
        skus.Should().Contain("A-01-10-01-014-410-1");
        skus.Should().Contain("A-01-10-01-014-410-2");
    }

    [Fact]
    public async Task ImportCsvAsync_MissingSkuOrNA_AutoGeneratesUniqueSku()
    {
        string csv = @"Product Name,SKU,Category,Supplier
GANG CHAIR 3 SEATER,N/A,Office Chair,Nilkamal
Wooden Coffee Table,,Furniture,Local";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new ProductImportOptions();

        var result = await _sut.ImportCsvAsync(stream, options);

        result.SuccessCount.Should().Be(2);
        _savedProducts.Should().HaveCount(2);
        _savedProducts.All(p => !string.IsNullOrWhiteSpace(p.SKU)).Should().BeTrue();
        _savedProducts.Select(p => p.SKU).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task ImportCsvAsync_FullTemplateWithPricesAndStock_CreatesStockMovements()
    {
        string csv = @"Product Name,SKU,Category,Retail Price,Wholesale Price,Stock Quantity,Reorder Level,Barcode,Description
Premium Mattress,PM-01,Mattress,45000.00,38000.00,10,2,8901234567,High quality foam";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new ProductImportOptions();

        var result = await _sut.ImportCsvAsync(stream, options, userId: 2);

        result.SuccessCount.Should().Be(1);
        _savedProducts.Should().HaveCount(1);
        var product = _savedProducts[0];
        product.RetailPrice.Should().Be(45000.00m);
        product.WholesalePrice.Should().Be(38000.00m);
        product.StockQuantity.Should().Be(10);
        product.Barcode.Should().Be("8901234567");

        _savedMovements.Should().HaveCount(1);
        var mov = _savedMovements[0];
        mov.Quantity.Should().Be(10);
        mov.QuantityAfter.Should().Be(10);
        mov.CreatedByUserId.Should().Be(2);
        mov.ReferenceType.Should().Be("ProductImport");
    }

    [Fact]
    public void GenerateSampleTemplateCsv_ReturnsValidCsvBytes()
    {
        var bytes = _sut.GenerateSampleTemplateCsv();
        bytes.Should().NotBeNullOrEmpty();
        string csv = Encoding.UTF8.GetString(bytes);
        csv.Should().Contain("Product Name");
        csv.Should().Contain("SKU");
        csv.Should().Contain("Category");
        csv.Should().Contain("Retail Price");
    }

    [Fact]
    public async Task ImportCsvAsync_CompleteUserProductCatalog_ImportsAll152ProductsSuccessfully()
    {
        string csv = @"Product Name,SKU,Category,Supplier
Classic Foam Double Layer 72*36*4,CFDL72364,MATTRESS,Arpico
Classic Foam Double Layer 72*42*4,CFDL72424,MATTRESS,Arpico
Classic Foam Double Layer 72*48*4,CFDL72484,MATTRESS,Arpico
Classic Foam Double Layer 72*60*4,CFDL72604,MATTRESS,Arpico
Classic Foam Double Layer 72*72*4,CFDL72724,MATTRESS,Arpico
Hybrid Mattrees 72*36*5,HM72365,MATTRESS,Arpico
Hybrid Mattrees 72*48*5,HM72485,MATTRESS,Arpico
Hybrid Mattrees 72*60*5,HM72605,MATTRESS,Arpico
Hybrid Mattrees 72*72*5,HM72725,MATTRESS,Arpico
Hybrid Mattrees 75*36*5,HM75365,MATTRESS,Arpico
Hybrid Mattrees 75*48*5,HM75485,MATTRESS,Arpico
Hybrid Mattrees 75*60*5,HM75605,MATTRESS,Arpico
Hybrid Mattrees 75*72*5,HM75725,MATTRESS,Arpico
Hybrid Mattrees 78*36*5,HM78365,MATTRESS,Arpico
Hybrid Mattrees 78*48*5,HM78485,MATTRESS,Arpico
Hybrid Mattrees 78*60*5,HM78605,MATTRESS,Arpico
Hybrid Mattrees 78*72*5,HM78725,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 72*36*8,FSM72368,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 72*48*8,FSM72488,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 72*60*8,FSM72608,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 72*72*8,FSM72728,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 75*36*8,FSM75368,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 75*48*8,FSM75488,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 75*60*8,FSM75608,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 75*72*8,FSM75728,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 78*36*8,FSM78368,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 78*48*8,FSM78488,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 78*60*8,FSM78608,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 78*72*8,FSM78728,MATTRESS,Arpico
Comfi Pillows 18*27,CP1827,Pillows,Arpico
Comfi Pillows 16*24,CP1624,Pillows,Arpico
Kingstra Double Layer 72*36*4,KDL72364,Mattress,Kingstra
Kingstra Double Layer 72*42*4,KDL72424,Mattress,Kingstra
Kingstra Double Layer 72*48*4,KDL72484,Mattress,Kingstra
Kingstra Double Layer 72*60*4,KDL72604,Mattress,Kingstra
Kingstra Double Layer 72*72*4,KDL72724,Mattress,Kingstra
Super Flex Spring 72*36*7,SFS72367,Spring Mattress,Piyestra Furniture
Super Flex Spring 72*48*7,SFS72487,Spring Mattress,Piyestra Furniture
Super Flex Spring 72*60*7,SFS72607,Spring Mattress,Piyestra Furniture
Super Flex Spring 72*72*7,SFS72727,Spring Mattress,Piyestra Furniture
Super Flex Spring 75*36*7,SFS75367,Spring Mattress,Piyestra Furniture
Super Flex Spring 75*48*7,SFS75487,Spring Mattress,Piyestra Furniture
Super Flex Spring 75*60*7,SFS75607,Spring Mattress,Piyestra Furniture
Super Flex Spring 75*72*7,SFS75727,Spring Mattress,Piyestra Furniture
Super Flex Spring 78*36*7,SFS78367,Spring Mattress,Piyestra Furniture
Super Flex Spring 78*48*7,SFS78487,Spring Mattress,Piyestra Furniture
Super Flex Spring 78*60*7,SFS78607,Spring Mattress,Piyestra Furniture
Super Flex Spring 78*72*7,SFS78727,Spring Mattress,Piyestra Furniture
Kingstra Arm Chair,KVAC010,Plastic Chair,Kingstra
Kingstra Armless Chair,KDC305,Plastic Chair,Kingstra
Recliner Chair,Reno,Sofa,Piyestra Furniture
Rice Cooker 750G,IRC159,Rice Cooker,DR Industreis PVT LTD
Rice Cooker 1KG,IRC189,Rice Cooker,DR Industreis PVT LTD
Rice Cooker 1.5KG,IRC229,Rice Cooker,DR Industreis PVT LTD
Rice Cooker 2KG,IRC289,Rice Cooker,DR Industreis PVT LTD
Rice Cooker 3.5KG,IRC429,Rice Cooker,DR Industreis PVT LTD
Stand Fan 45w 5 Blade,ISf017,Stand Fan,DR Industreis PVT LTD
Pressur Washer 130 Bar,IPW002,Pressur Washer,DR Industreis PVT LTD
Innovex Washing Machine 7Kg Full Auto,IFA70S,Innovex Washing Machine,DR Industreis PVT LTD
Fridge 180l Defrost,DDR195,Fridge,DR Industreis PVT LTD
Fridge 180l Defrose Invertor,IRI195,Fridge,DR Industreis PVT LTD
Fridge 240l Defreost,IDI240,Fridge,DR Industreis PVT LTD
Fridge 250l Invertor,INR240,Fridge,DR Industreis PVT LTD
Arpico Flaxifoam spring Mattress 72*36*10,FSM723610,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 72*48*10,FSM724810,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 72*60*10,FSM726010,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 72*72*10,FSM727210,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 75*36*10,FSM753610,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 75*48*10,FSM754810,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 75*60*10,FSM756010,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 75*72*10,FSM757210,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 78*36*10,FSM783610,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 78*48*10,FSM784810,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 78*60*10,FSM786010,MATTRESS,Arpico
Arpico Flaxifoam spring Mattress 78*72*10,FSM787210,MATTRESS,Arpico
2003-EXECUTIVE CHAIR-HB-BLUE,A-01-10-01-014-410,Office Chair,Nilkamal
1101A-COMPUTER CHAIR -WITH ARM-BLACK,A-01-10-01-002-407,Office Chair,Nilkamal
Amelia-PRW,A-01-01-04-001-210,Plastic Chair,Nilkamal
Amelia-RSW,A-01-01-04-001-212,Plastic Chair,Nilkamal
Aquarius Solo-BRD,A-01-02-01-002-105,Plastic Chair,Nilkamal
Aquarius Solo-DBL,A-01-02-01-002-109,Plastic Chair,Nilkamal
Aquarius Solo-TPN,A-01-02-01-002-138,Plastic Chair,Nilkamal
Cherry(BRS)-MRN,A-01-02-03-014-120,Plastic Stool,Nilkamal
Cherry(BRS)-NLG,A-01-02-03-014-125,Plastic Stool,Nilkamal
CLB-RSW,A-01-01-04-002-212,Plastic Chair,Nilkamal
CLB-PRW (A-01-01-04-002-212),A-01-01-04-002-212,Plastic Chair,Nilkamal
Concord-PRW,A-01-01-02-011-210,Plastic Chair,Nilkamal
Concord-IBK,A-01-01-02-011-210,Plastic Chair,Nilkamal
Concord-RSW,A-01-01-02-011-212,Plastic Chair,Nilkamal
Desire-BLK,A-01-01-03-009-102,Plastic Chair,Nilkamal
DONUT-BBC/BRD,A-01-02-03-004-342,Plastic Stool,Nilkamal
DONUT-PCH/IBK,A-01-02-03-004-371,Plastic Stool,Nilkamal
DONUT-WBN/BST,A-01-02-03-004-324,Plastic Stool,Nilkamal
Eeezy-IBK,A-01-01-03-017-117I,Plastic Chair,Nilkamal
Eeezy-RDB,A-01-01-03-017-117,Plastic Chair,Nilkamal
Elite-RSW,A-01-01-04-006-212,Plastic Chair,Nilkamal
PRINCE PRW,A-01-01-04-006-212,Plastic Chair,Nilkamal
Freedom Mini Large-GRY/TPN,A-01-02-05-003-308,Storage Cabinet,Nilkamal
Freedom Mini Medium-GRY/DBL,A-01-02-05-002-307,Storage Cabinet,Nilkamal
Freedom Mini Medium-GRY/TPN,A-01-02-05-002-308,Storage Cabinet,Nilkamal
Marvel-BLK,A-01-02-04-004-102,Table,Nilkamal
Marvel-PRW,A-01-02-04-004-210,Table,Nilkamal
Marvel-RSW,A-01-02-04-004-212,Table,Nilkamal
Mystique-PRW (A-01-01-02-014-210),A-01-01-02-014-210,Plastic Chair,Nilkamal
NEO-ibk,A-01-01-04-011-172,Plastic Chair,Nilkamal
NEO-PRW,A-01-01-04-011-118,Plastic Chair,Nilkamal
Opal-PRW,A-01-01-04-004-210,Plastic Chair,Nilkamal
Passion-PRW,A-01-01-03-003-102,Plastic Chair,Nilkamal
Rattan Chair-D Gray,A-01-01-03-016-172,Plastic Chair,Nilkamal
Rocky-Jungle-Blue/Red,A-01-02-01-005-302,Kids Furniture,Nilkamal
Rocky-Jungle-Green/Yellow,A-01-02-01-005-306,Kids Furniture,Nilkamal
STL21-(Stool)-BRD,B-01-01-04-015-105,Plastic Stool,Nilkamal
STL21-(Stool)-DBL,B-01-01-04-015-109,Plastic Stool,Nilkamal
VALENTINE-MBG,A-01-01-04-012-210,Plastic Chair,Nilkamal
VALENTINE-PRW,A-01-01-04-012-210,Plastic Chair,Nilkamal
VALENTINE-RSW (A-01-01-04-012-210),A-01-01-04-012-210,Plastic Chair,Nilkamal
Apple Desk With Chair-Blue/Red,A-01-02-01-006-302,Kids Furniture,Nilkamal
Apple Desk WITH CHAIR-Green/Yellow,A-01-02-01-006-306,Kids Furniture,Nilkamal
Globe W/O Disc-IBK,A-01-01-02-019-117,Plastic Stool,Nilkamal
Grand-RSW,A-01-01-03-014-212,Plastic Chair,Nilkamal
Rio-RSW,A-01-01-01-008-212,Plastic Chair,Nilkamal
1201A COMPUTER CHAIR HB-BLUE,A-01-10-01-016-410,Office Chair,Nilkamal
VALENTINE-RSW (A-01-01-04-012-212),A-01-01-04-012-212,Plastic Chair,Nilkamal
Weekender-BLK,A-01-01-03-006-102,Plastic Chair,Nilkamal
Wonder Baby Chair BRD/CEB/MYEL,B-01-01-01-027-368,Kids Furniture,Nilkamal
Nova-IBK,A-01-02-04-005-117,Plastic Chair,Nilkamal
Mystique-IBK,A-01-01-02-014-117,Plastic Chair,Nilkamal
Mystique-PRW (A-01-01-02-014-117),A-01-01-02-014-117,Plastic Chair,Nilkamal
Mystique-RSW,A-01-01-02-014-212,Plastic Chair,Nilkamal
Rattan Chair-PRW,A-01-01-03-016-141,Plastic Chair,Nilkamal
Rattan Chair-IBK,A-01-01-03-016-141,Plastic Chair,Nilkamal
STL-KICK-BRD,B-01-01-04-016-105,Plastic Stool,Nilkamal
Eeezy-WBN,A-01-01-03-017-141,Plastic Chair,Nilkamal
2001-EXECUTIVE CHAIR-LB-BLACK,A-01-10-01-012-407,Office Chair,Nilkamal
2001-EXECUTIVE CHAIR-LB-BLUE,A-01-10-01-012-410,Office Chair,Nilkamal
Startrek Chair – MWH/DBL,B-01-01-01-026-379,Plastic Chair,Nilkamal
1101-COMPUTER CHAIR-WITHOUT ARM-BLUE,A-01-10-01-001-410,Office Chair,Nilkamal
1101-COMPUTER CHAIR-WITHOUT ARM-BLACK,A-01-10-01-001-407,Office Chair,Nilkamal
CLB-PRW (A-01-01-04-002-210),A-01-01-04-002-210,Plastic Chair,Nilkamal
Trend-flore,A-01-01-03-014-182,Plastic Chair,Nilkamal
ECO BIN 13L PGR/GRY,A-01-02-08-015-382,Waste Bin,Nilkamal
ECO BIN 13L BRD/GRY,A-01-02-08-015-380,Waste Bin,Nilkamal
ECO BIN 13L DBL/GRY,A-01-02-08-015-381,Waste Bin,Nilkamal
LOUNDRY BASKET 50L,A-01-02-04-008-172,Laundry Basket,Nilkamal
2003-EXECUTIVE CHAIR-HB-Black,A-01-10-01-014-410,Office Chair,Nilkamal
Trend-sunset,A-01-01-03-014-181,Plastic Chair,Nilkamal
Trend-ocean,A-01-01-03-014-181,Plastic Chair,Nilkamal
5101-EXECUTIVE CHAIR-HB-Blue,A-01-10-01-014-410,Office Chair,Nilkamal
4502-EXECUTIVE CHAIR-HB,A-01-10-01-014-410,Office Chair,Nilkamal
2203-EXECUTIVE CHAIR-HB-Black,A-01-10-01-014-410,Office Chair,Nilkamal
1101A-COMPUTER CHAIR -WITH ARM-blue,A-01-10-01-002-407,Office Chair,Nilkamal
GANG CHAIR 3 SEATER,N/A,Office Chair,Nilkamal";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var options = new ProductImportOptions();

        var result = await _sut.ImportCsvAsync(stream, options);

        result.TotalRows.Should().Be(152);
        result.SuccessCount.Should().Be(152);
        result.ErrorCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        _savedProducts.Should().HaveCount(152);

        // Verify that all SKUs in the database are 100% unique
        _savedProducts.Select(p => p.SKU).Should().OnlyHaveUniqueItems();
    }
}
