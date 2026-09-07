using FluentAssertions;
using Moq;
using WarehousePOS.Application.Authentication;
using WarehousePOS.Application.Common;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Tests.Authentication;

public sealed class UserManagementServiceTests
{
    [Fact]
    public async Task NonAdminCannotManageUsers()
    {
        var worker = User.Create("cashier", "hash", "Cashier", UserRole.Worker);
        var repository = new Mock<IUserRepository>();
        repository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(worker);
        var service = CreateService(repository);

        var action = () => service.GetAllAsync(1);

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateHashesPasswordAndStoresRole()
    {
        var admin = User.Create("admin", "hash", "Admin", UserRole.Admin);
        var repository = new Mock<IUserRepository>();
        repository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        repository.Setup(x => x.GetByUsernameAsync("cashier", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash("secret")).Returns("bcrypt-hash");
        var service = new UserManagementService(repository.Object, hasher.Object);

        var result = await service.CreateAsync(1, new CreateUserRequest("cashier", "Cashier", "secret", UserRole.Worker));

        result.Role.Should().Be(UserRole.Worker);
        hasher.Verify(x => x.Hash("secret"), Times.Once);
        repository.Verify(x => x.AddAsync(It.Is<User>(u => u.PasswordHash == "bcrypt-hash" && u.Role == UserRole.Worker), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LastActiveAdminCannotBeDeactivated()
    {
        var admin = User.Create("admin", "hash", "Admin", UserRole.Admin);
        var repository = new Mock<IUserRepository>();
        repository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        repository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([admin]);
        var service = CreateService(repository);

        var action = () => service.DeactivateAsync(1, 1);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("You cannot deactivate the currently logged-in account.");
    }

    private static UserManagementService CreateService(Mock<IUserRepository> repository) =>
        new(repository.Object, Mock.Of<IPasswordHasher>());
}
