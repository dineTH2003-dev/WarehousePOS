using Microsoft.Extensions.Logging;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Products;

public sealed class ProductService(
    IProductRepository repo,
    ICategoryRepository categoryRepo,
    IInventoryMovementRepository movementRepo,
    ILogger<ProductService> logger) : IProductService
{
    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default)
    {
        var products = await repo.GetAllAsync(ct);
        return products.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ProductDto>> SearchAsync(string searchTerm, CancellationToken ct = default)
    {
        var products = await repo.SearchAsync(searchTerm, ct);
        return products.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ProductDto>> GetByCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        var products = await repo.GetByCategoryAsync(categoryId, ct);
        return products.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ProductDto>> GetLowStockAsync(CancellationToken ct = default)
    {
        var products = await repo.GetLowStockAsync(ct);
        return products.Select(Map).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var p = await repo.GetByIdAsync(id, ct);
        return p is null ? null : Map(p);
    }

    public async Task<ProductDto?> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        var p = await repo.GetByBarcodeAsync(barcode, ct);
        return p is null ? null : Map(p);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        if (await repo.ExistsBySkuAsync(request.SKU, ct: ct))
            throw new BusinessRuleViolationException("UniqueSKU", $"SKU '{request.SKU}' is already in use.");

        var category = await categoryRepo.GetByIdAsync(request.CategoryId, ct)
            ?? throw new EntityNotFoundException(nameof(Category), request.CategoryId);

        var product = Product.Create(
            request.Name, request.SKU, request.RetailPrice,
            request.WholesalePrice, request.CategoryId,
            request.Barcode, request.Description, request.ReorderLevel, request.StockQuantity);

        await repo.AddAsync(product, ct);
        if (request.StockQuantity > 0)
            await movementRepo.AddAsync(InventoryMovement.Create(
                product.Id, MovementType.StockIn, request.StockQuantity, 0,
                request.UpdatedByUserId, referenceType: "Product", notes: "Initial stock"), ct);
        logger.LogInformation("Product created: {SKU} — {Name}", product.SKU, product.Name);
        return Map(product) with { CategoryName = category.Name };
    }

    public async Task<ProductDto> UpdateAsync(UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new EntityNotFoundException(nameof(Product), request.Id);

        var category = await categoryRepo.GetByIdAsync(request.CategoryId, ct)
            ?? throw new EntityNotFoundException(nameof(Category), request.CategoryId);

        // Reflect name/description/category changes via a dedicated update method
        product.UpdateDetails(request.Name, request.Barcode, request.Description, request.CategoryId, request.ReorderLevel);
        product.UpdatePricing(request.RetailPrice, request.WholesalePrice);

        var stockBefore = product.StockQuantity;
        if (request.StockQuantity > stockBefore)
            product.AddStock(request.StockQuantity - stockBefore);
        else if (request.StockQuantity < stockBefore)
            product.DeductStock(stockBefore - request.StockQuantity);

        await repo.UpdateAsync(product, ct);
        if (request.StockQuantity != stockBefore)
            await movementRepo.AddAsync(InventoryMovement.Create(
                product.Id,
                request.StockQuantity > stockBefore ? MovementType.StockIn : MovementType.Adjustment,
                Math.Abs(request.StockQuantity - stockBefore),
                stockBefore,
                request.UpdatedByUserId,
                referenceType: "Product",
                notes: "Product form stock update"), ct);
        return Map(product) with { CategoryName = category.Name };
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var product = await repo.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Product), id);
        product.Deactivate();
        await repo.UpdateAsync(product, ct);
    }

    public async Task ActivateAsync(int id, CancellationToken ct = default)
    {
        var product = await repo.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Product), id);
        product.Activate();
        await repo.UpdateAsync(product, ct);
    }

    private static ProductDto Map(Product p) => new(
        p.Id, p.Name, p.SKU, p.Barcode, p.Description,
        p.RetailPrice, p.WholesalePrice, p.StockQuantity, p.ReorderLevel,
        p.IsActive, p.IsLowStock, p.CategoryId,
        p.Category?.Name ?? string.Empty);
}
