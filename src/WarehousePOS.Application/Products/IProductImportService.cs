namespace WarehousePOS.Application.Products;

public interface IProductImportService
{
    Task<ProductImportResultDto> ImportCsvAsync(
        Stream csvStream,
        ProductImportOptions options,
        int userId = 1,
        CancellationToken ct = default);

    byte[] GenerateSampleTemplateCsv();
}
