namespace WarehousePOS.Application.Products;

// ── Category DTOs ─────────────────────────────────────────────────────────────

public sealed record CategoryDto(int Id, string Name, string? Description, bool IsActive, int ProductCount);

public sealed record CreateCategoryRequest(string Name, string? Description);

public sealed record UpdateCategoryRequest(int Id, string Name, string? Description);

// ── Product DTOs ──────────────────────────────────────────────────────────────

public sealed record ProductDto(
    int Id,
    string Name,
    string SKU,
    string? Barcode,
    string? Description,
    decimal RetailPrice,
    decimal WholesalePrice,
    int StockQuantity,
    int ReorderLevel,
    int WarrantyYears,
    int WarrantyMonths,
    int WarrantyDays,
    int ClaimedQuantity,
    bool IsActive,
    bool IsLowStock,
    int CategoryId,
    string CategoryName)
{
    public string ClaimedText => ClaimedQuantity > 0 ? $" ({ClaimedQuantity})" : string.Empty;
};

public sealed record CreateProductRequest(
    string Name,
    string SKU,
    string? Barcode,
    string? Description,
    decimal RetailPrice,
    decimal WholesalePrice,
    int CategoryId,
    int ReorderLevel = 5,
    int StockQuantity = 0,
    int WarrantyYears = 0,
    int WarrantyMonths = 0,
    int WarrantyDays = 0,
    int UpdatedByUserId = 1);

public sealed record UpdateProductRequest(
    int Id,
    string Name,
    string SKU,
    string? Barcode,
    string? Description,
    decimal RetailPrice,
    decimal WholesalePrice,
    int CategoryId,
    int ReorderLevel,
    int StockQuantity,
    int WarrantyYears = 0,
    int WarrantyMonths = 0,
    int WarrantyDays = 0,
    int UpdatedByUserId = 1);
