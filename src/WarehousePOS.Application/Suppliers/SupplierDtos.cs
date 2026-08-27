namespace WarehousePOS.Application.Suppliers;

public sealed record SupplierDto(
    int Id,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    decimal Balance,
    bool IsActive);

public sealed record CreateSupplierRequest(
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address);

public sealed record UpdateSupplierRequest(
    int Id,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address);
