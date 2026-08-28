using Microsoft.EntityFrameworkCore;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        DirectoryManager.EnsureDirectoriesExist();

        // ── Schema bootstrap (handles all 3 scenarios) ───────────────
        // Returns true  → DB file was just created, full schema is ready.
        // Returns false → DB file already existed (tables may or may not be present).
        bool justCreated = await db.Database.EnsureCreatedAsync();

        if (!justCreated)
        {
            // The file existed already. Check how many tables are inside.
            // A stale empty .db left by a previous failed startup will have 0 tables.
            int tableCount = db.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM sqlite_master WHERE type='table'")
                .FirstOrDefault();

            if (tableCount == 0)
            {
                // File is empty — delete it and rebuild the full schema cleanly.
                await db.Database.EnsureDeletedAsync();
                await db.Database.EnsureCreatedAsync();
            }
        }

        // Seed Admin user if no users exist
        if (!await db.Users.AnyAsync())
        {
            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            var adminUser = User.Create("admin", adminPasswordHash, "System Administrator", UserRole.Admin);
            await db.Users.AddAsync(adminUser);
        }

        // Seed Store Settings if none exist
        if (!await db.StoreSettings.AnyAsync())
        {
            var settings = new[]
            {
                StoreSetting.Create("STORE_NAME", "WarehousePOS Main Store", "Name of the business"),
                StoreSetting.Create("STORE_ADDRESS", "123 Main Street, Colombo, Sri Lanka", "Store physical address"),
                StoreSetting.Create("STORE_PHONE", "+94 11 234 5678", "Contact phone number"),
                StoreSetting.Create("STORE_TAX_NO", "VAT-12345678-0000", "Tax Registration Number"),
                StoreSetting.Create("RECEIPT_HEADER", "Welcome to WarehousePOS", "Text shown at top of thermal/matrix receipt"),
                StoreSetting.Create("RECEIPT_FOOTER", "Thank you for your business! Please come again.", "Text shown at bottom of receipt")
            };
            await db.StoreSettings.AddRangeAsync(settings);
        }

        // Seed Default Category if none exist
        if (!await db.Categories.AnyAsync())
        {
            var defaultCategory = Category.Create("General", "Default Product Category");
            await db.Categories.AddAsync(defaultCategory);
        }

        await db.SaveChangesAsync();
    }
}
