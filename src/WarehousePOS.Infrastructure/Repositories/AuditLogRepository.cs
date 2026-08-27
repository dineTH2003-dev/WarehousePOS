using Microsoft.EntityFrameworkCore;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Infrastructure.Repositories;

public sealed class AuditLogRepository(AppDbContext db) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog log, CancellationToken ct = default)
    {
        await db.AuditLogs.AddAsync(log, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count = 100, CancellationToken ct = default) =>
        await db.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync(ct);

    public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(int userId, CancellationToken ct = default) =>
        await db.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
}
