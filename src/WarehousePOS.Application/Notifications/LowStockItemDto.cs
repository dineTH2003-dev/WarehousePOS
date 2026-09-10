namespace WarehousePOS.Application.Notifications;

public sealed record LowStockItemDto(
    int ProductId,
    string ProductName,
    string Sku,
    string? CategoryName,
    int CurrentStock,
    int ReorderLevel,
    bool IsOutOfStock);
