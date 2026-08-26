namespace WarehousePOS.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Every entity has an integer primary key and audit timestamps.
/// </summary>
public abstract class Entity
{
    public int Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    protected void SetUpdatedAt()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
