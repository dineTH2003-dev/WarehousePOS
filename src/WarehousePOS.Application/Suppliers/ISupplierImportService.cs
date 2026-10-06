namespace WarehousePOS.Application.Suppliers;

public interface ISupplierImportService
{
    Task<SupplierImportResultDto> ImportCsvAsync(
        Stream csvStream,
        SupplierImportOptions options,
        int userId = 1,
        CancellationToken ct = default);

    byte[] GenerateSampleTemplateCsv();
}
