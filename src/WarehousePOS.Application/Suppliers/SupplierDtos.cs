namespace WarehousePOS.Application.Suppliers;

public sealed record SupplierDto(
    int Id,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    decimal Balance,
    bool IsActive,
    string? ProvidedProducts = null)
{
    public string DisplayName => string.IsNullOrWhiteSpace(ContactPerson) ? Name : $"{Name} ({ContactPerson})";
}

public sealed record CreateSupplierRequest(
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? ProvidedProducts = null);

public sealed record UpdateSupplierRequest(
    int Id,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? ProvidedProducts = null);
