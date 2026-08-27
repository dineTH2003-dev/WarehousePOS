using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Purchasing;

public sealed class InventoryService(
    IProductRepository productRepo,
    IInventoryMovementRepository movementRepo) : IInventoryService
{
    public async Task<IReadOnlyList<StockLevelDto>> GetStockLevelsAsync(CancellationToken ct = default)
    {
        var products = await productRepo.GetAllAsync(ct);
        return products.Where(p => p.IsActive).Select(MapStock).ToList();
    }

    public async Task<IReadOnlyList<StockLevelDto>> GetLowStockAsync(CancellationToken ct = default)
    {
        var products = await productRepo.GetLowStockAsync(ct);
        return products.Select(MapStock).ToList();
    }

    public async Task<IReadOnlyList<InventoryMovementDto>> GetMovementHistoryAsync(int productId, CancellationToken ct = default)
    {
        var movements = await movementRepo.GetByProductAsync(productId, ct);
        return movements.Select(MapMovement).ToList();
    }

    public async Task AdjustStockAsync(StockAdjustmentRequest req, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(req.Notes, "Notes are required for stock adjustments.");

        var product = await productRepo.GetByIdAsync(req.ProductId, ct)
            ?? throw new EntityNotFoundException(nameof(Product), req.ProductId);

        if (req.Type is not (MovementType.StockIn or MovementType.Adjustment))
            throw new BusinessRuleViolationException("InvalidAdjustment",
                "Manual adjustments must use StockIn or Adjustment movement types.");

        var before = product.StockQuantity;

        if (req.Type == MovementType.StockIn)
            product.AddStock(req.Quantity);
        else
        {
            if (req.Quantity > product.StockQuantity)
                throw new InsufficientStockException(product.Name, req.Quantity, product.StockQuantity);
            product.DeductStock(req.Quantity);
        }

        await productRepo.UpdateAsync(product, ct);

        var movement = InventoryMovement.Create(
            product.Id, req.Type, req.Quantity, before,
            req.AdjustedByUserId, notes: req.Notes, referenceType: "Adjustment");
        await movementRepo.AddAsync(movement, ct);
    }

    private static StockLevelDto MapStock(Product p) => new(
        p.Id, p.Name, p.SKU,
        p.Category?.Name ?? string.Empty,
        p.StockQuantity, p.ReorderLevel, p.IsLowStock,
        p.StockQuantity * p.RetailPrice);

    private static InventoryMovementDto MapMovement(InventoryMovement m) => new(
        m.ProductId, m.Product?.Name ?? string.Empty,
        m.Type, m.Type.ToString(), m.Quantity,
        m.QuantityBefore, m.QuantityAfter,
        m.ReferenceId, m.Notes, m.CreatedAt);
}
