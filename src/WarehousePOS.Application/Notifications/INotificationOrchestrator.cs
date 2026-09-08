namespace WarehousePOS.Application.Notifications;

public interface INotificationOrchestrator
{
    Task<NotificationSettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task SaveSettingsAsync(NotificationSettingsDto settings, CancellationToken ct = default);
    Task<(bool Success, string Message)> SendTestEmailAsync(string? recipientEmail = null, CancellationToken ct = default);
    Task<(bool Success, string Message)> SendTestWhatsAppAsync(string? recipientPhone = null, CancellationToken ct = default);
    Task<(bool Success, string Message)> CheckAndSendLowStockAlertsAsync(bool force = false, CancellationToken ct = default);
    Task<(bool Success, string Message)> CheckAndSendMonthlyReportAsync(bool force = false, CancellationToken ct = default);
}
