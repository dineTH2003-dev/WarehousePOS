using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WarehousePOS.Application.Notifications;

namespace WarehousePOS.Infrastructure.Notifications;

public sealed class BrevoEmailService : IEmailNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BrevoEmailService> _logger;
    private const string BrevoEndpoint = "https://api.brevo.com/v3/smtp/email";

    public BrevoEmailService(HttpClient httpClient, ILogger<BrevoEmailService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SendTestEmailAsync(string recipientEmail, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
            return (false, "Recipient email address is required.");

        string html = """
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8"/>
                <style>
                    body { font-family: 'Segoe UI', Arial, sans-serif; background-color: #f3f4f6; margin: 0; padding: 24px; }
                    .card { max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 8px; border: 1px solid #e5e7eb; padding: 28px; }
                    .badge { background: #dcfce7; color: #166534; font-weight: bold; padding: 4px 10px; border-radius: 9999px; font-size: 12px; }
                    .footer { margin-top: 24px; font-size: 12px; color: #9ca3af; text-align: center; }
                </style>
            </head>
            <body>
                <div class="card">
                    <span class="badge">TEST NOTIFICATION</span>
                    <h2 style="color: #1f2937; margin-top: 12px;">WarehousePOS Email Service is Connected!</h2>
                    <p style="color: #4b5563; font-size: 14px; line-height: 1.6;">
                        This is a verification email from your <strong>WarehousePOS</strong> desktop system via <strong>Brevo</strong>.
                    </p>
                    <p style="color: #4b5563; font-size: 14px; line-height: 1.6;">
                        Your email notification pipeline is active. You will receive automatic alerts when product stocks run low or reach zero, as well as executive end-of-month business reports.
                    </p>
                    <div style="background: #f9fafb; border-left: 4px solid #2563eb; padding: 12px 16px; margin-top: 20px;">
                        <p style="margin: 0; font-size: 13px; color: #374151;">
                            <strong>Timestamp (UTC):</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                        </p>
                    </div>
                    <div class="footer">
                        WarehousePOS Desktop &bull; Single-PC Offline Architecture with Cloud Alerts
                    </div>
                </div>
            </body>
            </html>
            """;

        return await SendViaBrevoApiAsync(
            recipientEmail,
            "WarehousePOS — Email Notification Test",
            html,
            ct);
    }

    public async Task<(bool Success, string Message)> SendLowStockAlertAsync(
        IReadOnlyList<LowStockItemDto> items,
        string recipientEmail,
        CancellationToken ct = default)
    {
        if (items.Count == 0)
            return (true, "No low stock items to report.");

        if (string.IsNullOrWhiteSpace(recipientEmail))
            return (false, "Recipient email address is required.");

        var outOfStockCount = items.Count(i => i.IsOutOfStock);
        var lowStockCount = items.Count - outOfStockCount;

        var sbTable = new StringBuilder();
        foreach (var item in items.OrderBy(i => i.CurrentStock))
        {
            string badge = item.IsOutOfStock
                ? "<span style='background:#fee2e2;color:#991b1b;font-weight:bold;padding:2px 8px;border-radius:4px;font-size:11px;'>OUT OF STOCK</span>"
                : "<span style='background:#fef3c7;color:#92400e;font-weight:bold;padding:2px 8px;border-radius:4px;font-size:11px;'>LOW STOCK</span>";

            string stockColor = item.IsOutOfStock ? "#dc2626" : "#d97706";

            sbTable.Append($"""
                <tr style="border-bottom: 1px solid #f3f4f6;">
                    <td style="padding: 10px 12px; font-weight: 600; color: #1f2937;">{item.ProductName}</td>
                    <td style="padding: 10px 12px; color: #6b7280; font-family: monospace;">{item.Sku}</td>
                    <td style="padding: 10px 12px; color: #6b7280;">{item.CategoryName ?? "General"}</td>
                    <td style="padding: 10px 12px; text-align: center; font-weight: bold; color: {stockColor};">{item.CurrentStock}</td>
                    <td style="padding: 10px 12px; text-align: center; color: #4b5563;">{item.ReorderLevel}</td>
                    <td style="padding: 10px 12px; text-align: center;">{badge}</td>
                </tr>
                """);
        }

        string subject = outOfStockCount > 0
            ? $"⚠️ Critical Alert: {outOfStockCount} Out-of-Stock & {lowStockCount} Low-Stock Products"
            : $"⚠️ Inventory Alert: {lowStockCount} Products Below Reorder Level";

        string html = $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8"/>
                <style>
                    body { font-family: 'Segoe UI', Arial, sans-serif; background-color: #f3f4f6; margin: 0; padding: 24px; }
                    .card { max-width: 680px; margin: 0 auto; background: #ffffff; border-radius: 8px; border: 1px solid #e5e7eb; padding: 28px; }
                    table { width: 100%; border-collapse: collapse; font-size: 13px; margin-top: 16px; }
                    th { background: #f9fafb; color: #4b5563; font-weight: 600; text-align: left; padding: 10px 12px; border-bottom: 2px solid #e5e7eb; }
                    .footer { margin-top: 24px; font-size: 12px; color: #9ca3af; text-align: center; }
                </style>
            </head>
            <body>
                <div class="card">
                    <div style="display:flex; justify-content:space-between; align-items:center;">
                        <span style="background:#fee2e2;color:#991b1b;font-weight:bold;padding:4px 12px;border-radius:9999px;font-size:12px;">INVENTORY NOTICE</span>
                        <span style="font-size:12px;color:#6b7280;">{{DateTime.UtcNow:yyyy-MM-dd HH:mm}} UTC</span>
                    </div>
                    <h2 style="color: #111827; margin: 14px 0 6px 0;">Product Stock Alert</h2>
                    <p style="color: #4b5563; font-size: 14px; margin: 0 0 16px 0;">
                        The following items have depleted or dropped to or below their configured reorder thresholds. Please initiate purchasing or restock from suppliers promptly.
                    </p>

                    <table>
                        <thead>
                            <tr>
                                <th>Product</th>
                                <th>SKU</th>
                                <th>Category</th>
                                <th style="text-align:center;">Current</th>
                                <th style="text-align:center;">Reorder</th>
                                <th style="text-align:center;">Status</th>
                            </tr>
                        </thead>
                        <tbody>
                            {{sbTable}}
                        </tbody>
                    </table>

                    <div class="footer">
                        WarehousePOS Automated Inventory Service &bull; Generated from POS Terminal
                    </div>
                </div>
            </body>
            </html>
            """;

        return await SendViaBrevoApiAsync(recipientEmail, subject, html, ct);
    }

    public async Task<(bool Success, string Message)> SendMonthlyReportAsync(
        MonthlyReportSummaryDto report,
        string recipientEmail,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
            return (false, "Recipient email address is required.");

        var sbTopSellers = new StringBuilder();
        if (report.TopSellingProducts.Count > 0)
        {
            int rank = 1;
            foreach (var p in report.TopSellingProducts)
            {
                sbTopSellers.Append($"""
                    <tr style="border-bottom: 1px solid #f3f4f6;">
                        <td style="padding: 8px 10px; color: #6b7280; font-weight: bold;">#{rank++}</td>
                        <td style="padding: 8px 10px; font-weight: 600; color: #1f2937;">{p.Name}</td>
                        <td style="padding: 8px 10px; color: #6b7280; font-family: monospace;">{p.Sku}</td>
                        <td style="padding: 8px 10px; text-align: center; font-weight: bold;">{p.QuantitySold:N0}</td>
                        <td style="padding: 8px 10px; text-align: right; color: #059669; font-weight: bold;">Rs. {p.TotalRevenue:N2}</td>
                    </tr>
                    """);
            }
        }
        else
        {
            sbTopSellers.Append("<tr><td colspan='5' style='padding:12px;text-align:center;color:#9ca3af;'>No sales recorded for this month.</td></tr>");
        }

        string subject = $"📊 WarehousePOS Monthly Executive Report — {report.MonthLabel}";

        string html = $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8"/>
                <style>
                    body { font-family: 'Segoe UI', Arial, sans-serif; background-color: #f3f4f6; margin: 0; padding: 24px; }
                    .card { max-width: 680px; margin: 0 auto; background: #ffffff; border-radius: 8px; border: 1px solid #e5e7eb; padding: 28px; }
                    .grid-2 { display: flex; gap: 12px; margin-bottom: 16px; }
                    .kpi { flex: 1; background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 6px; padding: 14px; text-align: center; }
                    .kpi-title { font-size: 11px; text-transform: uppercase; color: #6b7280; font-weight: 600; margin-bottom: 4px; }
                    .kpi-value { font-size: 18px; font-weight: bold; color: #111827; }
                    table { width: 100%; border-collapse: collapse; font-size: 13px; margin-top: 12px; }
                    th { background: #f9fafb; color: #4b5563; font-weight: 600; text-align: left; padding: 8px 10px; border-bottom: 2px solid #e5e7eb; }
                    .footer { margin-top: 24px; font-size: 12px; color: #9ca3af; text-align: center; }
                </style>
            </head>
            <body>
                <div class="card">
                    <span style="background:#dbeafe;color:#1e40af;font-weight:bold;padding:4px 10px;border-radius:9999px;font-size:12px;">EXECUTIVE SUMMARY</span>
                    <h2 style="color: #111827; margin: 12px 0 4px 0;">Monthly Performance Report</h2>
                    <p style="color: #6b7280; font-size: 14px; margin: 0 0 20px 0;">
                        Statement for <strong>{{report.MonthLabel}}</strong> &bull; Total Transactions: <strong>{{report.TotalTransactions:N0}}</strong>
                    </p>

                    <!-- Financial KPIs -->
                    <table style="margin-bottom: 20px; border: 1px solid #e5e7eb; border-radius: 6px;">
                        <tr style="background: #f9fafb;">
                            <td style="padding: 12px; text-align: center; border-right: 1px solid #e5e7eb; width: 33%;">
                                <div style="font-size: 11px; color: #6b7280; font-weight: 600;">GROSS SALES</div>
                                <div style="font-size: 16px; font-weight: bold; color: #1f2937; margin-top: 4px;">Rs. {{report.GrossSales:N2}}</div>
                            </td>
                            <td style="padding: 12px; text-align: center; border-right: 1px solid #e5e7eb; width: 33%;">
                                <div style="font-size: 11px; color: #6b7280; font-weight: 600;">NET REVENUE</div>
                                <div style="font-size: 16px; font-weight: bold; color: #2563eb; margin-top: 4px;">Rs. {{report.NetSales:N2}}</div>
                            </td>
                            <td style="padding: 12px; text-align: center; width: 33%;">
                                <div style="font-size: 11px; color: #6b7280; font-weight: 600;">NET OPERATING PROFIT</div>
                                <div style="font-size: 16px; font-weight: bold; color: #059669; margin-top: 4px;">Rs. {{report.NetProfit:N2}}</div>
                            </td>
                        </tr>
                    </table>

                    <p style="font-size: 13px; color: #4b5563; margin: 0 0 16px 0;">
                        &bull; Total Discounts: <strong>Rs. {{report.TotalDiscounts:N2}}</strong> &nbsp;|&nbsp;
                        &bull; Operating Expenses: <strong>Rs. {{report.TotalExpenses:N2}}</strong>
                    </p>

                    <!-- Inventory Health -->
                    <h3 style="font-size: 14px; color: #111827; margin: 20px 0 8px 0; border-bottom: 1px solid #e5e7eb; padding-bottom: 6px;">Inventory &amp; Stock Valuation</h3>
                    <table style="margin-bottom: 20px; font-size: 13px;">
                        <tr>
                            <td style="padding: 6px 0; color: #4b5563;">Active Catalog Products:</td>
                            <td style="text-align: right; font-weight: bold; color: #111827;">{{report.TotalProducts:N0}} items</td>
                        </tr>
                        <tr>
                            <td style="padding: 6px 0; color: #4b5563;">Total Units in Warehouse:</td>
                            <td style="text-align: right; font-weight: bold; color: #111827;">{{report.TotalInventoryUnits:N0}} units</td>
                        </tr>
                        <tr>
                            <td style="padding: 6px 0; color: #4b5563;">Stock Valuation at Cost:</td>
                            <td style="text-align: right; font-weight: bold; color: #111827;">Rs. {{report.InventoryCostValuation:N2}}</td>
                        </tr>
                        <tr>
                            <td style="padding: 6px 0; color: #4b5563;">Stock Valuation at Retail:</td>
                            <td style="text-align: right; font-weight: bold; color: #059669;">Rs. {{report.InventoryRetailValuation:N2}}</td>
                        </tr>
                    </table>

                    <!-- Top Selling Items -->
                    <h3 style="font-size: 14px; color: #111827; margin: 20px 0 8px 0; border-bottom: 1px solid #e5e7eb; padding-bottom: 6px;">Top 10 Fast-Moving Products</h3>
                    <table>
                        <thead>
                            <tr>
                                <th style="width: 30px;">#</th>
                                <th>Product Name</th>
                                <th>SKU</th>
                                <th style="text-align:center;">Qty Sold</th>
                                <th style="text-align:right;">Total Sales</th>
                            </tr>
                        </thead>
                        <tbody>
                            {{sbTopSellers}}
                        </tbody>
                    </table>

                    <div class="footer">
                        WarehousePOS Automated Reporting &bull; End of Month Summary
                    </div>
                </div>
            </body>
            </html>
            """;

        return await SendViaBrevoApiAsync(recipientEmail, subject, html, ct);
    }

    private string _apiKey = string.Empty;
    private string _senderEmail = string.Empty;
    private string _senderName = "WarehousePOS Notifications";

    public void Configure(string apiKey, string senderEmail, string senderName)
    {
        _apiKey = apiKey.Trim();
        _senderEmail = senderEmail.Trim();
        if (!string.IsNullOrWhiteSpace(senderName))
            _senderName = senderName.Trim();
    }

    private async Task<(bool Success, string Message)> SendViaBrevoApiAsync(
        string recipientEmail,
        string subject,
        string htmlContent,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return (false, "Brevo API key is not configured. Please enter your API key in Settings > Notifications.");

        if (string.IsNullOrWhiteSpace(_senderEmail))
            return (false, "Sender email address is not configured. Please configure it in Settings > Notifications.");

        try
        {
            var payload = new BrevoSendRequest
            {
                Sender = new BrevoContact { Name = _senderName, Email = _senderEmail },
                To = [new BrevoContact { Email = recipientEmail.Trim(), Name = "Store Owner" }],
                Subject = subject,
                HtmlContent = htmlContent
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, BrevoEndpoint);
            request.Headers.Add("api-key", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent email '{Subject}' to {Recipient} via Brevo", subject, recipientEmail);
                return (true, "Email sent successfully via Brevo.");
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Brevo API returned error status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
            return (false, $"Brevo API error ({response.StatusCode}): {errorBody}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error sending email via Brevo to {Recipient}", recipientEmail);
            return (false, $"Network error connecting to Brevo: {ex.Message}. (Email will be queued if offline).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Recipient}", recipientEmail);
            return (false, $"Failed to send email: {ex.Message}");
        }
    }

    private sealed class BrevoSendRequest
    {
        [JsonPropertyName("sender")]
        public BrevoContact Sender { get; set; } = null!;

        [JsonPropertyName("to")]
        public List<BrevoContact> To { get; set; } = [];

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("htmlContent")]
        public string HtmlContent { get; set; } = string.Empty;
    }

    private sealed class BrevoContact
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }
}
