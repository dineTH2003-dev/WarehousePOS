namespace WarehousePOS.Application.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> SearchAsync(string searchTerm, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> GetByCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> GetLowStockAsync(CancellationToken ct = default);
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductDto?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(UpdateProductRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
    Task ActivateAsync(int id, CancellationToken ct = default);
}
