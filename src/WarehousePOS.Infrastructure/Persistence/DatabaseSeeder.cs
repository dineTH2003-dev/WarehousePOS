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
        var existing = db.Users.FirstOrDefault(u => u.Username == "happyproducts");
        if (existing is null)
        {
            var adminHash = passwordHasher.Hash("Indika@123");

            var admin = User.Create(
                username: "HappyProducts",
                passwordHash: adminHash,
                fullName: "Happy Products Admin",
                role: UserRole.Admin);

            await db.Users.AddAsync(admin);
            await db.SaveChangesAsync();
        }
        var legacyAdmin = db.Users.FirstOrDefault(u => u.Username == "admin");
        if (legacyAdmin is not null)
        {
            legacyAdmin.Deactivate();
            await db.SaveChangesAsync();
        }
    }
}
