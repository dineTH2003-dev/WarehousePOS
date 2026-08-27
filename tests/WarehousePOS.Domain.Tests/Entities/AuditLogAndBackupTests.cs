using FluentAssertions;
using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Domain.Tests.Entities;

public sealed class AuditLogTests
{
    [Fact]
    public void Create_ValidAuditLog_ShouldSetProperties()
    {
        var log = AuditLog.Create(
            userId: 1,
            username: "admin",
            action: "SALE_PROCESSED",
            entityName: "Sale",
            entityId: "101",
            details: "Retail sale completed for Rs. 1500");

        log.UserId.Should().Be(1);
        log.Username.Should().Be("admin");
        log.Action.Should().Be("SALE_PROCESSED");
        log.EntityName.Should().Be("Sale");
        log.EntityId.Should().Be("101");
        log.Details.Should().Contain("1500");
    }

    [Theory]
    [InlineData("", "Sale")]
    [InlineData("SALE", "")]
    [InlineData(null, "Sale")]
    public void Create_InvalidActionOrEntity_ShouldThrow(string? action, string? entity)
    {
        var act = () => AuditLog.Create(1, "admin", action!, entity!);
        act.Should().Throw<ArgumentException>();
    }
}
