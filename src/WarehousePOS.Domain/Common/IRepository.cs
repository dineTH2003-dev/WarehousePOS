namespace WarehousePOS.Domain.Common;

/// <summary>
/// Generic repository abstraction.
/// Implementations live in WarehousePOS.Infrastructure.
/// </summary>
/// <typeparam name="T">The aggregate root type.</typeparam>
public interface IRepository<T> where T : AggregateRoot
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}

/// <summary>
/// Unit of Work — wraps a database transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
