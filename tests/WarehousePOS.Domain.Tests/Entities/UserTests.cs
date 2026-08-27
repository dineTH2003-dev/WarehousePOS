using FluentAssertions;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Domain.Tests.Entities;

public sealed class UserTests
{
    [Fact]
    public void Create_ValidData_ShouldCreateUser()
    {
        var user = User.Create("johndoe", "$2a$12$hashedpassword", "John Doe", UserRole.Worker);

        user.Username.Should().Be("johndoe");
        user.FullName.Should().Be("John Doe");
        user.Role.Should().Be(UserRole.Worker);
        user.IsActive.Should().BeTrue();
        user.LastLoginAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_EmptyUsername_ShouldThrow(string? username)
    {
        Action act = () => User.Create(username!, "hash", "Full Name", UserRole.Admin);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_UsernameIsLowercased()
    {
        var user = User.Create("JohnDoe", "hash", "John Doe", UserRole.Worker);
        user.Username.Should().Be("johndoe");
    }

    [Fact]
    public void RecordLogin_ShouldSetLastLoginAt()
    {
        var user = User.Create("johndoe", "hash", "John Doe", UserRole.Worker);
        var before = DateTime.UtcNow;

        user.RecordLogin();

        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var user = User.Create("johndoe", "hash", "John Doe", UserRole.Worker);
        user.Deactivate();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_DeactivatedUser_ShouldSetIsActiveTrue()
    {
        var user = User.Create("johndoe", "hash", "John Doe", UserRole.Worker);
        user.Deactivate();
        user.Activate();
        user.IsActive.Should().BeTrue();
    }
}
