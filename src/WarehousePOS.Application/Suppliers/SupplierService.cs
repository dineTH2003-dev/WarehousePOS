using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Suppliers;

public sealed class SupplierService(ISupplierRepository repo) : ISupplierService
{
    public async Task<IReadOnlyList<SupplierDto>> GetAllAsync(CancellationToken ct = default) =>
        (await repo.GetAllAsync(ct)).Select(Map).ToList();

    public async Task<IReadOnlyList<SupplierDto>> GetActiveAsync(CancellationToken ct = default) =>
        (await repo.GetActiveAsync(ct)).Select(Map).ToList();

    public async Task<SupplierDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var s = await repo.GetByIdAsync(id, ct);
        return s is null ? null : Map(s);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest req, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(req.Name);
        if (await repo.ExistsByNameAsync(req.Name, ct: ct))
            throw new BusinessRuleViolationException("UniqueSupplier", $"Supplier '{req.Name}' already exists.");

        var supplier = Supplier.Create(req.Name, req.ContactPerson, req.Phone, req.Email, req.Address);
        await repo.AddAsync(supplier, ct);
        return Map(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(UpdateSupplierRequest req, CancellationToken ct = default)
    {
        var supplier = await repo.GetByIdAsync(req.Id, ct)
            ?? throw new EntityNotFoundException(nameof(Supplier), req.Id);

        if (await repo.ExistsByNameAsync(req.Name, req.Id, ct))
            throw new BusinessRuleViolationException("UniqueSupplier", $"Supplier '{req.Name}' already exists.");

        supplier.Update(req.Name, req.ContactPerson, req.Phone, req.Email, req.Address);
        await repo.UpdateAsync(supplier, ct);
        return Map(supplier);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var s = await repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException(nameof(Supplier), id);
        s.Deactivate();
        await repo.UpdateAsync(s, ct);
    }

    public async Task ActivateAsync(int id, CancellationToken ct = default)
    {
        var s = await repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException(nameof(Supplier), id);
        s.Activate();
        await repo.UpdateAsync(s, ct);
    }

    private static SupplierDto Map(Supplier s) =>
        new(s.Id, s.Name, s.ContactPerson, s.Phone, s.Email, s.Address, s.Balance, s.IsActive);
}
