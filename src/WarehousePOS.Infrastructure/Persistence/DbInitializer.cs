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
            else
            {
                // Migrate any missing columns on existing SQLite tables
                await EnsureColumnsExistAsync(db);
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

        // Seed Default Expense Categories if none exist
        if (!await db.ExpenseCategories.AnyAsync())
        {
            var defaultExpenseCategories = new[]
            {
                ExpenseCategory.Create("Utility Bills", "Electricity, water, internet, telephone, etc."),
                ExpenseCategory.Create("Transport & Fuel", "Deliveries, logistics, vehicle fuel, transport"),
                ExpenseCategory.Create("Rent & Lease", "Warehouse, storage, and facility rent / lease"),
                ExpenseCategory.Create("Wages & Salaries", "Staff payroll and temporary/daily labor"),
                ExpenseCategory.Create("Maintenance & Repairs", "Equipment, building, and facility maintenance"),
                ExpenseCategory.Create("Office Supplies", "Stationery, packaging, printing supplies"),
                ExpenseCategory.Create("Miscellaneous", "Other operational expenses")
            };
            await db.ExpenseCategories.AddRangeAsync(defaultExpenseCategories);
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureColumnsExistAsync(AppDbContext db)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS SupplierProductEntitlements (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SupplierId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    Nature TEXT NOT NULL,
                    Quantity INTEGER NULL,
                    Value TEXT NULL,
                    EventDate TEXT NOT NULL,
                    NextEntitlementDate TEXT NULL,
                    SpecialNotes TEXT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NULL,
                    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id) ON DELETE RESTRICT,
                    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE RESTRICT
                );");

            var supplierColumns = await db.Database
                .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Suppliers')")
                .ToListAsync();

            if (!supplierColumns.Contains("ProvidedProducts", StringComparer.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Suppliers ADD COLUMN ProvidedProducts TEXT NULL;");
            }

            var purchaseItemColumns = await db.Database
                .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('PurchaseItems')")
                .ToListAsync();

            if (purchaseItemColumns.Any())
            {
                if (!purchaseItemColumns.Contains("FreeQuantity", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE PurchaseItems ADD COLUMN FreeQuantity INTEGER NOT NULL DEFAULT 0;");
                }
                if (!purchaseItemColumns.Contains("RetailPrice", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE PurchaseItems ADD COLUMN RetailPrice TEXT NOT NULL DEFAULT '0';");
                }
                if (!purchaseItemColumns.Contains("WholesalePrice", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE PurchaseItems ADD COLUMN WholesalePrice TEXT NOT NULL DEFAULT '0';");
                }
                if (!purchaseItemColumns.Contains("ClaimedQuantityReceived", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE PurchaseItems ADD COLUMN ClaimedQuantityReceived INTEGER NOT NULL DEFAULT 0;");
                }
            }

            var saleItemColumns = await db.Database
                .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('SaleItems')")
                .ToListAsync();

            if (saleItemColumns.Any() && !saleItemColumns.Contains("ClaimedQuantity", StringComparer.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE SaleItems ADD COLUMN ClaimedQuantity INTEGER NOT NULL DEFAULT 0;");
            }

            var purchaseColumns = await db.Database
                .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Purchases')")
                .ToListAsync();

            if (purchaseColumns.Any())
            {
                if (!purchaseColumns.Contains("PaymentMethod", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Purchases ADD COLUMN PaymentMethod TEXT NOT NULL DEFAULT 'Cash';");
                }
                if (!purchaseColumns.Contains("PaidAmount", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Purchases ADD COLUMN PaidAmount TEXT NOT NULL DEFAULT '0';");
                }
                if (!purchaseColumns.Contains("PaymentDetails", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Purchases ADD COLUMN PaymentDetails TEXT NULL;");
                }
            }

            var saleColumns = await db.Database
                .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Sales')")
                .ToListAsync();

            if (saleColumns.Any() && !saleColumns.Contains("PaymentMethod", StringComparer.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Sales ADD COLUMN PaymentMethod TEXT NOT NULL DEFAULT 'Cash';");
            }

            var customerColumns = await db.Database
                .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Customers')")
                .ToListAsync();

            if (customerColumns.Any())
            {
                if (!customerColumns.Contains("DiscountRate", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Customers ADD COLUMN DiscountRate TEXT NOT NULL DEFAULT '0';");
                }
                if (!customerColumns.Contains("OutstandingBalance", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Customers ADD COLUMN OutstandingBalance TEXT NOT NULL DEFAULT '0';");
                }
            }

            var productColumns = await db.Database
                .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Products')")
                .ToListAsync();

            if (productColumns.Any())
            {
                if (!productColumns.Contains("WarrantyYears", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN WarrantyYears INTEGER NOT NULL DEFAULT 0;");
                }
                if (!productColumns.Contains("WarrantyMonths", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN WarrantyMonths INTEGER NOT NULL DEFAULT 0;");
                }
                if (!productColumns.Contains("WarrantyDays", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN WarrantyDays INTEGER NOT NULL DEFAULT 0;");
                }
                if (!productColumns.Contains("ClaimedQuantity", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN ClaimedQuantity INTEGER NOT NULL DEFAULT 0;");
                }
                if (!productColumns.Contains("PendingHigherPriceStockQuantity", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN PendingHigherPriceStockQuantity INTEGER NOT NULL DEFAULT 0;");
                }
                if (!productColumns.Contains("PendingNewRetailPrice", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN PendingNewRetailPrice TEXT NULL;");
                }
                if (!productColumns.Contains("PendingNewWholesalePrice", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Products ADD COLUMN PendingNewWholesalePrice TEXT NULL;");
                }
            }

            // --- Purchasing enhancements ---
            if (purchaseColumns.Any() && !purchaseColumns.Contains("DiscountAmount", StringComparer.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Purchases ADD COLUMN DiscountAmount TEXT NOT NULL DEFAULT '0';");
            }

            // --- Sales enhancements: Delivery fee, custom customer, and address ---
            if (saleColumns.Any())
            {
                if (!saleColumns.Contains("DeliveryFee", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Sales ADD COLUMN DeliveryFee TEXT NOT NULL DEFAULT '0';");
                }
                if (!saleColumns.Contains("CustomerName", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Sales ADD COLUMN CustomerName TEXT NULL;");
                }
                if (!saleColumns.Contains("CustomerPhone", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Sales ADD COLUMN CustomerPhone TEXT NULL;");
                }
                if (!saleColumns.Contains("DeliveryAddress", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Sales ADD COLUMN DeliveryAddress TEXT NULL;");
                }
            }

            // --- SaleItems: Returned Quantity ---
            if (saleItemColumns.Any() && !saleItemColumns.Contains("ReturnedQuantity", StringComparer.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE SaleItems ADD COLUMN ReturnedQuantity INTEGER NOT NULL DEFAULT 0;");
            }

            // --- Expenses: Linked SaleId ---
            var expenseColumns = await db.Database
                .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Expenses')")
                .ToListAsync();

            if (expenseColumns.Any() && !expenseColumns.Contains("SaleId", StringComparer.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Expenses ADD COLUMN SaleId INTEGER NULL;");
            }

            // --- SalePayments table for advance and installment payments ---
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS SalePayments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SaleId INTEGER NOT NULL,
                    Amount TEXT NOT NULL,
                    PaymentMethod INTEGER NOT NULL,
                    PaymentDate TEXT NOT NULL,
                    CashierUserId INTEGER NOT NULL,
                    Notes TEXT NULL,
                    FOREIGN KEY (SaleId) REFERENCES Sales (Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_SalePayments_SaleId ON SalePayments (SaleId);
                CREATE INDEX IF NOT EXISTS IX_SalePayments_PaymentDate ON SalePayments (PaymentDate);
            ");

            // --- Users table enhancements (BaseSalary & CommissionRate) ---
            var userColumns = await db.Database
                .SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Users')")
                .ToListAsync();

            if (userColumns.Any())
            {
                if (!userColumns.Contains("BaseSalary", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Users ADD COLUMN BaseSalary TEXT NOT NULL DEFAULT '0';");
                }
                if (!userColumns.Contains("CommissionRate", StringComparer.OrdinalIgnoreCase))
                {
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Users ADD COLUMN CommissionRate TEXT NOT NULL DEFAULT '0';");
                }
            }

            // --- Executive Reporting Domain Tables ---
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS DeliveryTrips (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TripCode TEXT NOT NULL,
                    DriverUserId INTEGER NOT NULL,
                    DispatchedAtUtc TEXT NOT NULL,
                    DeliveredAtUtc TEXT NULL,
                    Status INTEGER NOT NULL,
                    OdometerStartKm REAL NOT NULL,
                    OdometerEndKm REAL NOT NULL,
                    MileagePayout TEXT NOT NULL DEFAULT '0',
                    BonusPayout TEXT NOT NULL DEFAULT '0',
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NULL,
                    FOREIGN KEY (DriverUserId) REFERENCES Users (Id) ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS FuelLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DriverUserId INTEGER NOT NULL,
                    FuelDateUtc TEXT NOT NULL,
                    Liters TEXT NOT NULL DEFAULT '0',
                    TotalCost TEXT NOT NULL DEFAULT '0',
                    ReceiptNumber TEXT NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NULL,
                    FOREIGN KEY (DriverUserId) REFERENCES Users (Id) ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS ServiceTickets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TicketCode TEXT NOT NULL,
                    TechnicianUserId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    DefectDescription TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    CreatedDateUtc TEXT NOT NULL,
                    ResolvedDateUtc TEXT NULL,
                    TurnaroundHours REAL NOT NULL DEFAULT 0,
                    RepairLaborFee TEXT NOT NULL DEFAULT '0',
                    SparePartsCost TEXT NOT NULL DEFAULT '0',
                    TechnicianBonus TEXT NOT NULL DEFAULT '0',
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NULL,
                    FOREIGN KEY (TechnicianUserId) REFERENCES Users (Id) ON DELETE RESTRICT,
                    FOREIGN KEY (ProductId) REFERENCES Products (Id) ON DELETE RESTRICT
                );
            ");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error auto-migrating SQLite columns: {ex.Message}");
        }
    }
}


