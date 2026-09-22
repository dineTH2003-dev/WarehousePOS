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

    [Fact]
    public async Task CreateStoresSalaryParameters()
    {
        var admin = User.Create("admin", "hash", "Admin", UserRole.Admin);
        var repository = new Mock<IUserRepository>();
        repository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        repository.Setup(x => x.GetByUsernameAsync("employee1", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash("secret")).Returns("bcrypt-hash");
        var service = new UserManagementService(repository.Object, hasher.Object);

        var request = new CreateUserRequest("employee1", "Employee One", "secret", UserRole.Worker, 45000m, "Basic + Transport", "25th of month");
        var result = await service.CreateAsync(1, request);

        result.BaseSalary.Should().Be(45000m);
        result.SalaryComponents.Should().Be("Basic + Transport");
        result.PaymentDueDate.Should().Be("25th of month");
        repository.Verify(x => x.AddAsync(It.Is<User>(u => u.BaseSalary == 45000m && u.SalaryComponents == "Basic + Transport" && u.PaymentDueDate == "25th of month"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSupportsCashierRoleAndFormattedDueDate()
    {
        var admin = User.Create("admin", "hash", "Admin", UserRole.Admin);
        var repository = new Mock<IUserRepository>();
        repository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        repository.Setup(x => x.GetByUsernameAsync("cashier1", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash("pass")).Returns("hashed-pass");
        var service = new UserManagementService(repository.Object, hasher.Object);

        var request = new CreateUserRequest("cashier1", "Cashier Person", "pass", UserRole.Cashier, 35000m, "Basic", "2026-09-20");
        var result = await service.CreateAsync(1, request);

        result.Role.Should().Be(UserRole.Cashier);
        result.PaymentDueDate.Should().Be("2026-09-20");
    }

    private static UserManagementService CreateService(Mock<IUserRepository> repository) =>
        new(repository.Object, Mock.Of<IPasswordHasher>());
}
