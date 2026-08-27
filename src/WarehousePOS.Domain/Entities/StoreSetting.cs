using WarehousePOS.Domain.Common;

namespace WarehousePOS.Domain.Entities;

/// <summary>
/// System configuration key-value pair for store details and print headers.
/// </summary>
public sealed class StoreSetting : Entity
{
    private StoreSetting() { }

    public string Key         { get; private set; } = string.Empty;
    public string Value       { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public static StoreSetting Create(string key, string value, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return new StoreSetting
        {
            Key         = key.Trim().ToUpperInvariant(),
            Value       = value.Trim(),
            Description = description?.Trim()
        };
    }

    public void UpdateValue(string value)
    {
        Value = value.Trim();
        SetUpdatedAt();
    }
}
