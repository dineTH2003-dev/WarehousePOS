using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Domain.Interfaces;

public interface IPurchaseRepository
{
    Task<Purchase?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Purchase>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Purchase>> GetByStatusAsync(PurchaseStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<Purchase>> GetBySupplierAsync(int supplierId, CancellationToken ct = default);
    Task AddAsync(Purchase purchase, CancellationToken ct = default);
    Task UpdateAsync(Purchase purchase, CancellationToken ct = default);
}

public interface IInventoryMovementRepository
{
    Task<IReadOnlyList<InventoryMovement>> GetByProductAsync(int productId, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryMovement>> GetAllAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task AddAsync(InventoryMovement movement, CancellationToken ct = default);
}
