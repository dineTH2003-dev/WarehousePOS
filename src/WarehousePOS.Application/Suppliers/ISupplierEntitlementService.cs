namespace WarehousePOS.Application.Suppliers;

public interface ISupplierEntitlementService
{
    Task<IReadOnlyList<SupplierProductEntitlementDto>> GetEntitlementsBySupplierAsync(int supplierId, CancellationToken ct = default);
    Task<IReadOnlyList<SupplierProductEntitlementDto>> GetEntitlementsByProductAsync(int productId, CancellationToken ct = default);
    Task<SupplierProductEntitlementDto> CreateEntitlementAsync(CreateSupplierEntitlementDto dto, CancellationToken ct = default);
    Task DeleteEntitlementAsync(int id, CancellationToken ct = default);
}
