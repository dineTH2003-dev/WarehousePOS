using WarehousePOS.Domain.Common;

namespace WarehousePOS.Domain.Entities;

/// <summary>
/// Audit trail for recording security and critical business actions.
/// </summary>
public sealed class AuditLog : Entity
{
    private AuditLog() { }

    public int UserId          { get; private set; }
    public string Username     { get; private set; } = string.Empty;
    public string Action       { get; private set; } = string.Empty;   // e.g. "SALE_PROCESSED", "STOCK_ADJUSTED", "USER_LOGIN"
    public string EntityName   { get; private set; } = string.Empty;   // e.g. "Sale", "Product"
    public string? EntityId    { get; private set; }
    public string? Details     { get; private set; }

    public static AuditLog Create(
        int userId,
        string username,
        string action,
        string entityName,
        string? entityId = null,
        string? details  = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        return new AuditLog
        {
            UserId     = userId,
            Username   = username.Trim(),
            Action     = action.Trim(),
            EntityName = entityName.Trim(),
            EntityId   = entityId?.Trim(),
            Details    = details?.Trim()
        };
    }
}
