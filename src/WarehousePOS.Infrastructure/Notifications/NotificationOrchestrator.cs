using Microsoft.Extensions.Logging;
using WarehousePOS.Application.Notifications;
using WarehousePOS.Application.Reports;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Infrastructure.Notifications;

public sealed class NotificationOrchestrator : INotificationOrchestrator
{
    private readonly IStoreSettingRepository _settingRepo;
    private readonly IProductRepository _productRepo;
    private readonly IReportService _reportService;
    private readonly BrevoEmailService _emailService;
    private readonly WhatsAppNotificationService _whatsappService;
    private readonly ILogger<NotificationOrchestrator> _logger;

    public NotificationOrchestrator(
        IStoreSettingRepository settingRepo,
        IProductRepository productRepo,
        IReportService reportService,
        BrevoEmailService emailService,
        WhatsAppNotificationService whatsappService,
        ILogger<NotificationOrchestrator> logger)
    {
        _settingRepo = settingRepo;
        _productRepo = productRepo;
        _reportService = reportService;
        _emailService = emailService;
        _whatsappService = whatsappService;
        _logger = logger;
    }

    public async Task<NotificationSettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        var apiKey        = await _settingRepo.GetValueAsync("NOTIF_BREVO_API_KEY", ct) ?? string.Empty;
        var senderEmail   = await _settingRepo.GetValueAsync("NOTIF_BREVO_SENDER_EMAIL", ct) ?? string.Empty;
        var senderName    = await _settingRepo.GetValueAsync("NOTIF_BREVO_SENDER_NAME", ct) ?? "WarehousePOS";
        var ownerEmail    = await _settingRepo.GetValueAsync("NOTIF_OWNER_EMAIL", ct) ?? string.Empty;
        var emailStock    = await _settingRepo.GetValueAsync("NOTIF_EMAIL_LOW_STOCK_ENABLED", ct) ?? "true";
        var emailMonthly  = await _settingRepo.GetValueAsync("NOTIF_EMAIL_MONTHLY_REPORT_ENABLED", ct) ?? "true";

        var whatsAppOn    = await _settingRepo.GetValueAsync("NOTIF_WHATSAPP_ENABLED", ct) ?? "false";
        var ownerPhone    = await _settingRepo.GetValueAsync("NOTIF_WHATSAPP_PHONE", ct) ?? string.Empty;
        var waUrl         = await _settingRepo.GetValueAsync("NOTIF_WHATSAPP_GATEWAY_URL", ct) ?? string.Empty;
        var waKey         = await _settingRepo.GetValueAsync("NOTIF_WHATSAPP_API_KEY", ct) ?? string.Empty;

        return new NotificationSettingsDto(
            apiKey,
            senderEmail,
            senderName,
            ownerEmail,
            emailStock.Equals("true", StringComparison.OrdinalIgnoreCase),
            emailMonthly.Equals("true", StringComparison.OrdinalIgnoreCase),
            whatsAppOn.Equals("true", StringComparison.OrdinalIgnoreCase),
            ownerPhone,
            waUrl,
            waKey);
    }

    public async Task SaveSettingsAsync(NotificationSettingsDto s, CancellationToken ct = default)
    {
        await _settingRepo.SetValueAsync("NOTIF_BREVO_API_KEY", s.BrevoApiKey, "Brevo API Key for transactional email", ct);
        await _settingRepo.SetValueAsync("NOTIF_BREVO_SENDER_EMAIL", s.BrevoSenderEmail, "Sender email address for Brevo", ct);
        await _settingRepo.SetValueAsync("NOTIF_BREVO_SENDER_NAME", s.BrevoSenderName, "Sender display name for Brevo", ct);
        await _settingRepo.SetValueAsync("NOTIF_OWNER_EMAIL", s.OwnerEmail, "Recipient owner email for stock & reports", ct);
        await _settingRepo.SetValueAsync("NOTIF_EMAIL_LOW_STOCK_ENABLED", s.IsEmailLowStockAlertEnabled ? "true" : "false", "Send low stock email alerts", ct);
        await _settingRepo.SetValueAsync("NOTIF_EMAIL_MONTHLY_REPORT_ENABLED", s.IsEmailMonthlyReportEnabled ? "true" : "false", "Send monthly summary report email", ct);

        await _settingRepo.SetValueAsync("NOTIF_WHATSAPP_ENABLED", s.IsWhatsAppEnabled ? "true" : "false", "Enable WhatsApp notification gateway", ct);
        await _settingRepo.SetValueAsync("NOTIF_WHATSAPP_PHONE", s.OwnerPhone, "Owner phone number for WhatsApp alerts", ct);
        await _settingRepo.SetValueAsync("NOTIF_WHATSAPP_GATEWAY_URL", s.WhatsAppGatewayUrl, "HTTP endpoint for WhatsApp API gateway", ct);
        await _settingRepo.SetValueAsync("NOTIF_WHATSAPP_API_KEY", s.WhatsAppApiKey, "API Token/Key for WhatsApp API gateway", ct);

        _logger.LogInformation("Saved notification settings for owner: {OwnerEmail}, phone: {OwnerPhone}", s.OwnerEmail, s.OwnerPhone);
    }

    private async Task EnsureConfiguredAsync(CancellationToken ct)
    {
        var s = await GetSettingsAsync(ct);
        _emailService.Configure(s.BrevoApiKey, s.BrevoSenderEmail, s.BrevoSenderName);
        _whatsappService.Configure(s.WhatsAppGatewayUrl, s.WhatsAppApiKey);
    }

    public async Task<(bool Success, string Message)> SendTestEmailAsync(string? recipientEmail = null, CancellationToken ct = default)
    {
        await EnsureConfiguredAsync(ct);
        var s = await GetSettingsAsync(ct);
        var to = string.IsNullOrWhiteSpace(recipientEmail) ? s.OwnerEmail : recipientEmail;

        if (string.IsNullOrWhiteSpace(to))
            return (false, "Please specify an owner/recipient email address in Settings > Notifications.");

        return await _emailService.SendTestEmailAsync(to, ct);
    }

    public async Task<(bool Success, string Message)> SendTestWhatsAppAsync(string? recipientPhone = null, CancellationToken ct = default)
    {
        await EnsureConfiguredAsync(ct);
        var s = await GetSettingsAsync(ct);
        var phone = string.IsNullOrWhiteSpace(recipientPhone) ? s.OwnerPhone : recipientPhone;

        if (string.IsNullOrWhiteSpace(phone))
            return (false, "Please specify an owner phone number in Settings > Notifications.");

        return await _whatsappService.SendTextMessageAsync(
            phone,
            "🚀 *WarehousePOS Test Alert*\n\nYour WhatsApp notification gateway is successfully connected and ready to send inventory alerts!",
            ct);
    }

    public async Task<(bool Success, string Message)> CheckAndSendLowStockAlertsAsync(bool force = false, CancellationToken ct = default)
    {
        await EnsureConfiguredAsync(ct);
        var s = await GetSettingsAsync(ct);

        if (!s.IsEmailLowStockAlertEnabled && !s.IsWhatsAppEnabled && !force)
            return (true, "Low stock notifications are disabled in settings.");

        var lowStockProducts = await _productRepo.GetLowStockAsync(ct);
        if (lowStockProducts.Count == 0)
            return (true, "All products have healthy stock levels. No low stock alerts needed.");

        // Throttling: only send once per day unless force is requested
        string todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (!force)
        {
            var lastSentDate = await _settingRepo.GetValueAsync("NOTIF_LAST_LOW_STOCK_SENT_DATE", ct);
            if (string.Equals(lastSentDate, todayStr, StringComparison.OrdinalIgnoreCase))
            {
                return (true, "Low stock alert already sent today. Throttled to avoid repetitive alerts.");
            }
        }

        var items = lowStockProducts.Select(p => new WarehousePOS.Application.Notifications.LowStockItemDto(
            p.Id,
            p.Name,
            p.SKU,
            p.Category?.Name,
            p.StockQuantity,
            p.ReorderLevel,
            p.StockQuantity <= 0)).ToList();

        var errors = new List<string>();
        bool anySent = false;

        // Dispatch email
        if ((s.IsEmailLowStockAlertEnabled || force) && !string.IsNullOrWhiteSpace(s.OwnerEmail))
        {
            var (ok, msg) = await _emailService.SendLowStockAlertAsync(items, s.OwnerEmail, ct);
            if (ok) anySent = true;
            else errors.Add($"Email: {msg}");
        }

        // Dispatch WhatsApp
        if ((s.IsWhatsAppEnabled || force) && !string.IsNullOrWhiteSpace(s.OwnerPhone))
        {
            var (ok, msg) = await _whatsappService.SendLowStockAlertAsync(items, s.OwnerPhone, ct);
            if (ok) anySent = true;
            else errors.Add($"WhatsApp: {msg}");
        }

        if (anySent)
        {
            await _settingRepo.SetValueAsync("NOTIF_LAST_LOW_STOCK_SENT_DATE", todayStr, "Date of last low stock alert", ct);
        }

        if (errors.Count > 0)
        {
            return (anySent, anySent
                ? $"Alert partially sent. Issues: {string.Join("; ", errors)}"
                : $"Failed to send alert: {string.Join("; ", errors)}");
        }

        return (true, $"Stock alert successfully sent for {items.Count} items.");
    }

    public async Task<(bool Success, string Message)> CheckAndSendMonthlyReportAsync(bool force = false, CancellationToken ct = default)
    {
        await EnsureConfiguredAsync(ct);
        var s = await GetSettingsAsync(ct);

        if (!s.IsEmailMonthlyReportEnabled && !s.IsWhatsAppEnabled && !force)
            return (true, "Monthly reports are disabled in settings.");

        // Target month: previous month (e.g. if today is Sep 1, report on August)
        var now = DateTime.UtcNow;
        var targetMonth = force ? (now.Day <= 5 ? now.AddMonths(-1) : now) : now.AddMonths(-1);
        string monthKey = $"{targetMonth.Year:D4}-{targetMonth.Month:D2}";

        if (!force)
        {
            // Only fire automatically on 1st of month
            if (now.Day != 1)
                return (true, "Monthly report is scheduled for the 1st of each month.");

            var lastSent = await _settingRepo.GetValueAsync("NOTIF_LAST_MONTHLY_REPORT_SENT", ct);
            if (string.Equals(lastSent, monthKey, StringComparison.OrdinalIgnoreCase))
            {
                return (true, $"Monthly report for {monthKey} was already generated and sent.");
            }
        }

        var report = await _reportService.GetMonthlyReportSummaryAsync(targetMonth.Year, targetMonth.Month, ct);

        var errors = new List<string>();
        bool anySent = false;

        if ((s.IsEmailMonthlyReportEnabled || force) && !string.IsNullOrWhiteSpace(s.OwnerEmail))
        {
            var (ok, msg) = await _emailService.SendMonthlyReportAsync(report, s.OwnerEmail, ct);
            if (ok) anySent = true;
            else errors.Add($"Email: {msg}");
        }

        if ((s.IsWhatsAppEnabled || force) && !string.IsNullOrWhiteSpace(s.OwnerPhone))
        {
            var (ok, msg) = await _whatsappService.SendMonthlySummaryAsync(report, s.OwnerPhone, ct);
            if (ok) anySent = true;
            else errors.Add($"WhatsApp: {msg}");
        }

        if (anySent)
        {
            await _settingRepo.SetValueAsync("NOTIF_LAST_MONTHLY_REPORT_SENT", monthKey, "Last monthly report sent key", ct);
        }

        if (errors.Count > 0)
        {
            return (anySent, anySent
                ? $"Monthly report partially sent. Issues: {string.Join("; ", errors)}"
                : $"Failed to send monthly report: {string.Join("; ", errors)}");
        }

        return (true, $"Monthly report for {report.MonthLabel} successfully delivered.");
    }
}
