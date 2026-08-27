using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count = 100, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetByUserAsync(int userId, CancellationToken ct = default);
}
