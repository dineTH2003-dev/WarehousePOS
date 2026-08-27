using WarehousePOS.Application.Common;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Infrastructure.Persistence;

/// <summary>
/// Seeds the database with required initial data on first launch.
/// Called from App.xaml.cs after migrations have been applied.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher)
    {
        await SeedDefaultAdminAsync(db, passwordHasher);
    }

    private static async Task SeedDefaultAdminAsync(AppDbContext db, IPasswordHasher passwordHasher)
    {
        // Only seed if no users exist
        if (db.Users.Any()) return;

        var adminHash = passwordHasher.Hash("Admin@1234");

        var admin = User.Create(
            username: "admin",
            passwordHash: adminHash,
            fullName: "System Administrator",
            role: UserRole.Admin);

        await db.Users.AddAsync(admin);
        await db.SaveChangesAsync();
    }
}
