using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Domain.Interfaces;

public interface ISupplierEntitlementRepository
{
    Task<SupplierProductEntitlement?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<SupplierProductEntitlement>> GetBySupplierIdAsync(int supplierId, CancellationToken ct = default);
    Task<IReadOnlyList<SupplierProductEntitlement>> GetByProductIdAsync(int productId, CancellationToken ct = default);
    Task AddAsync(SupplierProductEntitlement entitlement, CancellationToken ct = default);
    Task DeleteAsync(SupplierProductEntitlement entitlement, CancellationToken ct = default);
}
