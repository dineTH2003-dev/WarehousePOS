using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Domain.Interfaces;

public interface IStoreSettingRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<StoreSetting>> GetAllAsync(CancellationToken ct = default);
    Task SetValueAsync(string key, string value, string? description = null, CancellationToken ct = default);
}
