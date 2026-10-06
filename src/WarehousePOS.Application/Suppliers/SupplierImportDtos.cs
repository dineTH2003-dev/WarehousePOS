namespace WarehousePOS.Application.Suppliers;

public sealed record SupplierImportOptions(
    bool UpdateExisting = true,
    bool AutoCreateMissingProducts = true);

public sealed record SupplierImportErrorDto(
    int RowNumber,
    string SupplierName,
    string Reason);

public sealed record SupplierImportResultDto(
    int TotalRows,
    int SuccessCount,
    int UpdatedCount,
    int SkippedCount,
    int ErrorCount,
    IReadOnlyList<SupplierImportErrorDto> Errors,
    string SummaryMessage);
