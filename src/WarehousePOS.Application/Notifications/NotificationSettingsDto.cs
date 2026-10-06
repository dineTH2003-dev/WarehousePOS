namespace WarehousePOS.Application.Notifications;

public sealed record NotificationSettingsDto(
    string BrevoApiKey,
    string BrevoSenderEmail,
    string BrevoSenderName,
    string OwnerEmail,
    bool IsEmailLowStockAlertEnabled,
    bool IsEmailMonthlyReportEnabled);
