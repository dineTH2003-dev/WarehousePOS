namespace WarehousePOS.Application.Products;

public sealed record ProductImportOptions(
    bool UpdateExisting = false,
    string DefaultCategory = "General");

public sealed record ProductImportErrorDto(
    int RowNumber,
    string Sku,
    string ProductName,
    string ErrorMessage);

public sealed record ProductImportResultDto(
    int TotalRows,
    int SuccessCount,
    int SkippedCount,
    int ErrorCount,
    IReadOnlyList<ProductImportErrorDto> Errors,
    string SummaryMessage);
