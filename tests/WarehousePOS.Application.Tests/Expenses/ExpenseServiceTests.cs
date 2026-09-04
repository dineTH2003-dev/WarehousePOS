using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WarehousePOS.Application.Expenses;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Tests.Expenses;

public sealed class ExpenseServiceTests
{
    private readonly Mock<IExpenseRepository> _repoMock = new();
    private readonly ExpenseService _sut;

    public ExpenseServiceTests()
    {
        _sut = new ExpenseService(_repoMock.Object, NullLogger<ExpenseService>.Instance);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsMappedCategories()
    {
        var cat1 = ExpenseCategory.Create("Utility Bills", "Electricity & Water");
        var cat2 = ExpenseCategory.Create("Transport", "Fuel");
        _repoMock.Setup(r => r.GetCategoriesAsync(false, default))
                 .ReturnsAsync(new List<ExpenseCategory> { cat1, cat2 });

        var result = await _sut.GetCategoriesAsync();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Utility Bills");
        result[1].Name.Should().Be("Transport");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedExpenses()
    {
        var expense = Expense.Create(1, 2500m, "Electricity bill", 1, DateTime.UtcNow, "REF-001");
        _repoMock.Setup(r => r.GetAllAsync(default))
                 .ReturnsAsync(new List<Expense> { expense });

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].Amount.Should().Be(2500m);
        result[0].Description.Should().Be("Electricity bill");
        result[0].ReferenceNo.Should().Be("REF-001");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_AddsExpenseAndReturnsDto()
    {
        var category = ExpenseCategory.Create("Utility Bills");
        _repoMock.Setup(r => r.GetCategoryByIdAsync(1, default))
                 .ReturnsAsync(category);

        var req = new CreateExpenseRequest(1, 1500.50m, "Water bill", 1, DateTime.UtcNow, "REF-100");
        var result = await _sut.CreateAsync(req);

        result.Should().NotBeNull();
        result.Amount.Should().Be(1500.50m);
        result.Description.Should().Be("Water bill");
        _repoMock.Verify(r => r.AddAsync(It.Is<Expense>(e => e.Amount == 1500.50m && e.CategoryId == 1), default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NonExistentCategory_ThrowsEntityNotFoundException()
    {
        _repoMock.Setup(r => r.GetCategoryByIdAsync(999, default))
                 .ReturnsAsync((ExpenseCategory?)null);

        var req = new CreateExpenseRequest(999, 1500m, "Water bill", 1);
        var act = () => _sut.CreateAsync(req);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateCategoryAsync_ValidRequest_AddsCategory()
    {
        var req = new CreateExpenseCategoryRequest("Maintenance", "Building repairs");
        var result = await _sut.CreateCategoryAsync(req);

        result.Should().NotBeNull();
        result.Name.Should().Be("Maintenance");
        result.Description.Should().Be("Building repairs");
        _repoMock.Verify(r => r.AddCategoryAsync(It.Is<ExpenseCategory>(c => c.Name == "Maintenance"), default), Times.Once);
    }
}
