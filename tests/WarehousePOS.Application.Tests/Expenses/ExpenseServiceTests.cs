using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WarehousePOS.Application.Expenses;
using WarehousePOS.Application.Reports;
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
    public async Task UpdateAsync_ValidRequest_UpdatesExpense()
    {
        var category = ExpenseCategory.Create("Transport & Fuel");
        var expense = Expense.Create(1, 1000m, "Old fuel", 1, DateTime.UtcNow);

        _repoMock.Setup(r => r.GetByIdAsync(10, default)).ReturnsAsync(expense);
        _repoMock.Setup(r => r.GetCategoryByIdAsync(1, default)).ReturnsAsync(category);

        var updateReq = new UpdateExpenseRequest(1, 3500m, "Updated fuel cost", DateTime.UtcNow, "REF-NEW");
        var result = await _sut.UpdateAsync(10, updateReq);

        result.Should().NotBeNull();
        result.Amount.Should().Be(3500m);
        result.Description.Should().Be("Updated fuel cost");
        _repoMock.Verify(r => r.UpdateAsync(expense, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingExpense_RemovesExpense()
    {
        var expense = Expense.Create(1, 1000m, "To delete", 1, DateTime.UtcNow);
        _repoMock.Setup(r => r.GetByIdAsync(5, default)).ReturnsAsync(expense);

        await _sut.DeleteAsync(5);

        _repoMock.Verify(r => r.DeleteAsync(expense, default), Times.Once);
    }

    [Fact]
    public async Task GetAnalyticsAsync_CalculatesAccurateKPIsAndCategoryBreakdown()
    {
        var cat1 = ExpenseCategory.Create("Transport & Fuel");
        typeof(ExpenseCategory).GetProperty("Id")!.SetValue(cat1, 1);
        var cat2 = ExpenseCategory.Create("Utility Bills");
        typeof(ExpenseCategory).GetProperty("Id")!.SetValue(cat2, 2);

        var today = DateTime.Today;
        var e1 = Expense.Create(1, 5000m, "Diesel fuel for lorry", 1, today, "REF-1");
        typeof(Expense).GetProperty("Category")!.SetValue(e1, cat1);

        var e2 = Expense.Create(2, 2000m, "Water bill", 1, today, "REF-2");
        typeof(Expense).GetProperty("Category")!.SetValue(e2, cat2);

        _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<Expense> { e1, e2 });
        _repoMock.Setup(r => r.GetCategoriesAsync(false, default)).ReturnsAsync(new List<ExpenseCategory> { cat1, cat2 });

        var req = new ExpenseFilterRequest(DateRangePreset.ThisMonth, new DateTime(today.Year, today.Month, 1), today.AddDays(1));
        var summary = await _sut.GetAnalyticsAsync(req);

        summary.TotalExpenses.Should().Be(7000m);
        summary.ExpenseCount.Should().Be(2);
        summary.AverageExpense.Should().Be(3500m);
        summary.HighestCategoryName.Should().Be("Transport & Fuel");
        summary.HighestCategoryAmount.Should().Be(5000m);
        summary.HighestCategoryPercentage.Should().Be(71.4);
    }
}
