using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Application.Sales;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CustomerDto>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CustomerDto>> SearchAsync(string term, CancellationToken ct = default);
    Task<CustomerDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDto> UpdateAsync(UpdateCustomerRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
    Task ActivateAsync(int id, CancellationToken ct = default);
}

public interface ISaleService
{
    Task<IReadOnlyList<SaleDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SaleDto>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<SaleDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SaleDto> ProcessSaleAsync(CreateSaleRequest request, CancellationToken ct = default);
    Task CancelSaleAsync(int saleId, CancellationToken ct = default);
}
