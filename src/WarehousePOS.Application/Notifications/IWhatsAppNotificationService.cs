namespace WarehousePOS.Application.Notifications;

public interface IWhatsAppNotificationService
{
    Task<(bool Success, string Message)> SendTextMessageAsync(string recipientPhone, string message, CancellationToken ct = default);
    Task<(bool Success, string Message)> SendLowStockAlertAsync(IReadOnlyList<LowStockItemDto> items, string recipientPhone, CancellationToken ct = default);
    Task<(bool Success, string Message)> SendMonthlySummaryAsync(MonthlyReportSummaryDto report, string recipientPhone, CancellationToken ct = default);
}
