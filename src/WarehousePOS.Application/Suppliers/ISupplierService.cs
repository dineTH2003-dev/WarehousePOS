namespace WarehousePOS.Application.Suppliers;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SupplierDto>> GetActiveAsync(CancellationToken ct = default);
    Task<SupplierDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default);
    Task<SupplierDto> UpdateAsync(UpdateSupplierRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
    Task ActivateAsync(int id, CancellationToken ct = default);
}
