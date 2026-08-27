using FluentAssertions;
using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Domain.Tests.Entities;

public sealed class CategoryTests
{
    [Fact]
    public void Create_ValidName_ShouldCreateCategory()
    {
        var cat = Category.Create("Electronics", "Electronic products");
        cat.Name.Should().Be("Electronics");
        cat.Description.Should().Be("Electronic products");
        cat.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_EmptyName_ShouldThrow(string? name)
    {
        Action act = () => Category.Create(name!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ChangesNameAndDescription()
    {
        var cat = Category.Create("Old Name");
        cat.Update("New Name", "New Desc");
        cat.Name.Should().Be("New Name");
        cat.Description.Should().Be("New Desc");
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var cat = Category.Create("Test");
        cat.Deactivate();
        cat.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_ShouldSetIsActiveTrue()
    {
        var cat = Category.Create("Test");
        cat.Deactivate();
        cat.Activate();
        cat.IsActive.Should().BeTrue();
    }
}
