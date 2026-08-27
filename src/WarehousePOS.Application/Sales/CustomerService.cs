using Microsoft.Extensions.Logging;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Sales;

public sealed class CustomerService(
    ICustomerRepository repo,
    ILogger<CustomerService> logger) : ICustomerService
{
    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default) =>
        (await repo.GetAllAsync(ct)).Select(Map).ToList();

    public async Task<IReadOnlyList<CustomerDto>> GetActiveAsync(CancellationToken ct = default) =>
        (await repo.GetActiveAsync(ct)).Select(Map).ToList();

    public async Task<IReadOnlyList<CustomerDto>> SearchAsync(string term, CancellationToken ct = default) =>
        (await repo.SearchAsync(term, ct)).Select(Map).ToList();

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var c = await repo.GetByIdAsync(id, ct);
        return c is null ? null : Map(c);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest req, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(req.Name);
        var customer = Customer.Create(req.Name, req.Type, req.Phone, req.Email, req.Address);
        await repo.AddAsync(customer, ct);
        logger.LogInformation("Customer created: {Name} ({Type})", customer.Name, customer.Type);
        return Map(customer);
    }

    public async Task<CustomerDto> UpdateAsync(UpdateCustomerRequest req, CancellationToken ct = default)
    {
        var customer = await repo.GetByIdAsync(req.Id, ct)
            ?? throw new EntityNotFoundException(nameof(Customer), req.Id);

        customer.Update(req.Name, req.Type, req.Phone, req.Email, req.Address);
        await repo.UpdateAsync(customer, ct);
        return Map(customer);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var customer = await repo.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Customer), id);
        customer.Deactivate();
        await repo.UpdateAsync(customer, ct);
    }

    public async Task ActivateAsync(int id, CancellationToken ct = default)
    {
        var customer = await repo.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Customer), id);
        customer.Activate();
        await repo.UpdateAsync(customer, ct);
    }

    private static CustomerDto Map(Customer c) =>
        new(c.Id, c.Name, c.Type, c.Type.ToString(), c.Phone, c.Email, c.Address, c.IsActive);
}
