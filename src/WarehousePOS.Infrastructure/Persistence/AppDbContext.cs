using Microsoft.EntityFrameworkCore;
using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Infrastructure.Persistence;

/// <summary>
/// The main EF Core database context for WarehousePOS.
/// Connection string is configured in App.xaml.cs — the database lives at:
///   C:\ProgramData\WarehousePOS\Data\WarehousePOS.db
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // --- Entities ---
    public DbSet<Product>           Products           { get; set; }
    public DbSet<Category>          Categories         { get; set; }
    public DbSet<User>              Users              { get; set; }
    public DbSet<Supplier>          Suppliers          { get; set; }
    public DbSet<Purchase>          Purchases          { get; set; }
    public DbSet<PurchaseItem>      PurchaseItems      { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }
    public DbSet<Customer>          Customers          { get; set; }
    public DbSet<Sale>              Sales              { get; set; }
    public DbSet<SaleItem>          SaleItems          { get; set; }
    public DbSet<AuditLog>          AuditLogs          { get; set; }
    public DbSet<StoreSetting>      StoreSettings      { get; set; }
    public DbSet<ExpenseCategory>   ExpenseCategories  { get; set; }
    public DbSet<Expense>           Expenses           { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all entity type configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
