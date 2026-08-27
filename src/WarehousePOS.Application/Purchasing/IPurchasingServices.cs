using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Application.Purchasing;

public interface IPurchaseService
{
    Task<IReadOnlyList<PurchaseDto>> GetAllAsync(CancellationToken ct = default);
    Task<PurchaseDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PurchaseDto> CreateAsync(CreatePurchaseRequest request, CancellationToken ct = default);
    Task ConfirmAsync(int purchaseId, CancellationToken ct = default);
    Task<PurchaseDto> ReceiveStockAsync(int purchaseId, CancellationToken ct = default);
    Task CancelAsync(int purchaseId, CancellationToken ct = default);
}

public interface IInventoryService
{
    Task<IReadOnlyList<StockLevelDto>> GetStockLevelsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StockLevelDto>> GetLowStockAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InventoryMovementDto>> GetMovementHistoryAsync(int productId, CancellationToken ct = default);
    Task AdjustStockAsync(StockAdjustmentRequest request, CancellationToken ct = default);
}
