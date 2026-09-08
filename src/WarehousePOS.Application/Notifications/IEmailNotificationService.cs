namespace WarehousePOS.Application.Notifications;

public interface IEmailNotificationService
{
    Task<(bool Success, string Message)> SendTestEmailAsync(string recipientEmail, CancellationToken ct = default);
    Task<(bool Success, string Message)> SendLowStockAlertAsync(IReadOnlyList<LowStockItemDto> items, string recipientEmail, CancellationToken ct = default);
    Task<(bool Success, string Message)> SendMonthlyReportAsync(MonthlyReportSummaryDto report, string recipientEmail, CancellationToken ct = default);
}
