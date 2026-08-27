using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Domain.Interfaces;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Supplier>> GetActiveAsync(CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default);
    Task AddAsync(Supplier supplier, CancellationToken ct = default);
    Task UpdateAsync(Supplier supplier, CancellationToken ct = default);
}
