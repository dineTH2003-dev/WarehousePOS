using FluentAssertions;
using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Domain.Tests.Entities;

public sealed class ExpenseAndSettingsTests
{
    [Fact]
    public void Create_ExpenseCategory_ValidName_ShouldSetProperties()
    {
        var category = ExpenseCategory.Create("Utility Bills", "Electricity and Water");
        category.Name.Should().Be("Utility Bills");
        category.Description.Should().Be("Electricity and Water");
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Expense_ValidValues_ShouldSetProperties()
    {
        var expense = Expense.Create(
            categoryId: 1,
            amount: 15000,
            description: "Electricity Bill August",
            recordedByUserId: 1,
            referenceNo: "CEB-88912");

        expense.CategoryId.Should().Be(1);
        expense.Amount.Should().Be(15000);
        expense.Description.Should().Be("Electricity Bill August");
        expense.ReferenceNo.Should().Be("CEB-88912");
    }

    [Theory]
    [InlineData(0, 100, "Desc")]
    [InlineData(1, 0, "Desc")]
    [InlineData(1, -50, "Desc")]
    [InlineData(1, 100, "")]
    public void Create_Expense_InvalidInputs_ShouldThrow(int catId, decimal amount, string desc)
    {
        var act = () => Expense.Create(catId, amount, desc, recordedByUserId: 1);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Create_StoreSetting_ShouldNormalizeKey()
    {
        var setting = StoreSetting.Create("store_name", "My Warehouse", "Store Name");
        setting.Key.Should().Be("STORE_NAME");
        setting.Value.Should().Be("My Warehouse");
    }
}
