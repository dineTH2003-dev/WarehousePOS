using Microsoft.EntityFrameworkCore;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        await db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) =>
        await db.Users.OrderBy(u => u.FullName).ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await db.Users.AddAsync(user, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
    }
}
