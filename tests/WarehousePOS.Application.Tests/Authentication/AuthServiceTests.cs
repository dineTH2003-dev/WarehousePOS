using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WarehousePOS.Application.Authentication;
using WarehousePOS.Application.Common;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Tests.Authentication;

public sealed class AuthServiceTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _repoMock.Object,
            _hasherMock.Object,
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResult()
    {
        var user = User.Create("admin", "hashed", "Admin User", UserRole.Admin);
        _repoMock.Setup(r => r.GetByUsernameAsync("admin", default)).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("password123", "hashed")).Returns(true);

        var result = await _sut.LoginAsync(new LoginRequest("admin", "password123"));

        result.Should().NotBeNull();
        result!.Username.Should().Be("admin");
        result.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        var user = User.Create("admin", "hashed", "Admin User", UserRole.Admin);
        _repoMock.Setup(r => r.GetByUsernameAsync("admin", default)).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("wrongpass", "hashed")).Returns(false);

        var result = await _sut.LoginAsync(new LoginRequest("admin", "wrongpass"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_UnknownUser_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), default))
                 .ReturnsAsync((User?)null);

        var result = await _sut.LoginAsync(new LoginRequest("unknown", "pass"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsNull()
    {
        var user = User.Create("admin", "hashed", "Admin User", UserRole.Admin);
        user.Deactivate();
        _repoMock.Setup(r => r.GetByUsernameAsync("admin", default)).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("admin", "password123"));

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("", "pass")]
    [InlineData("user", "")]
    [InlineData(" ", "pass")]
    public async Task LoginAsync_EmptyCredentials_ReturnsNull(string username, string password)
    {
        var result = await _sut.LoginAsync(new LoginRequest(username, password));
        result.Should().BeNull();
    }
}
