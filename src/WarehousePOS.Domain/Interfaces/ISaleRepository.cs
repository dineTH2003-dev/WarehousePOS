using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> SearchAsync(string term, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetByTypeAsync(SaleType type, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    Task UpdateAsync(Customer customer, CancellationToken ct = default);
}

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Sale>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Sale>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<Sale>> GetByCustomerAsync(int customerId, CancellationToken ct = default);
    Task AddAsync(Sale sale, CancellationToken ct = default);
    Task UpdateAsync(Sale sale, CancellationToken ct = default);
    Task<IReadOnlyList<(int ProductId, string Sku, string Name, string CategoryName, int QuantitySold, decimal TotalSales)>> GetTopSellingProductsAsync(int topCount = 10, CancellationToken ct = default);
}
