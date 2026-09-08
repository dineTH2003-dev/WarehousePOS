using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WarehousePOS.Application.Notifications;

namespace WarehousePOS.Infrastructure.Notifications;

public sealed class WhatsAppNotificationService : IWhatsAppNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppNotificationService> _logger;

    private string _gatewayUrl = string.Empty;
    private string _apiKey = string.Empty;

    public WhatsAppNotificationService(HttpClient httpClient, ILogger<WhatsAppNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public void Configure(string gatewayUrl, string apiKey)
    {
        _gatewayUrl = gatewayUrl.Trim();
        _apiKey = apiKey.Trim();
    }

    public async Task<(bool Success, string Message)> SendTextMessageAsync(
        string recipientPhone,
        string message,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(recipientPhone))
            return (false, "Recipient phone number is required.");

        if (string.IsNullOrWhiteSpace(_gatewayUrl))
            return (false, "WhatsApp Gateway URL is not configured. Please enter your gateway URL in Settings > Notifications.");

        try
        {
            // Standard normalized phone (remove spaces, hyphens, plus if needed by gateway)
            string normalizedPhone = recipientPhone.Replace(" ", "").Replace("-", "");

            var payload = new Dictionary<string, object>
            {
                ["to"] = normalizedPhone,
                ["phone"] = normalizedPhone,
                ["number"] = normalizedPhone,
                ["body"] = message,
                ["message"] = message,
                ["text"] = message
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, _gatewayUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                request.Headers.Add("apikey", _apiKey);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            }

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent WhatsApp notification to {Phone}", recipientPhone);
                return (true, "WhatsApp message sent successfully.");
            }

            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("WhatsApp Gateway error ({StatusCode}): {Response}", response.StatusCode, err);
            return (false, $"WhatsApp gateway error ({response.StatusCode}): {err}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error sending WhatsApp message to {Phone}", recipientPhone);
            return (false, $"Network error connecting to WhatsApp gateway: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending WhatsApp message to {Phone}", recipientPhone);
            return (false, $"Failed to send WhatsApp message: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> SendLowStockAlertAsync(
        IReadOnlyList<LowStockItemDto> items,
        string recipientPhone,
        CancellationToken ct = default)
    {
        if (items.Count == 0)
            return (true, "No low stock items to report.");

        var outOfStockCount = items.Count(i => i.IsOutOfStock);
        var lowStockCount = items.Count - outOfStockCount;

        var sb = new StringBuilder();
        sb.AppendLine("⚠️ *WarehousePOS — LOW STOCK ALERT*");
        sb.AppendLine($"📅 {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n");

        if (outOfStockCount > 0)
            sb.AppendLine($"🔴 *{outOfStockCount} OUT OF STOCK*");
        if (lowStockCount > 0)
            sb.AppendLine($"🟡 *{lowStockCount} BELOW REORDER LEVEL*");

        sb.AppendLine("\n*Depleted Products:*");
        foreach (var item in items.Take(15).OrderBy(i => i.CurrentStock))
        {
            string status = item.IsOutOfStock ? "❌ OUT OF STOCK" : $"⚠️ Qty: {item.CurrentStock}/{item.ReorderLevel}";
            sb.AppendLine($"• *{item.ProductName}* ({item.Sku})");
            sb.AppendLine($"  {status} | Cat: {item.CategoryName ?? "General"}");
        }

        if (items.Count > 15)
            sb.AppendLine($"\n...and {items.Count - 15} more items. Check POS dashboard.");

        sb.AppendLine("\n_Please restock soon to prevent missed sales._");

        return await SendTextMessageAsync(recipientPhone, sb.ToString(), ct);
    }

    public async Task<(bool Success, string Message)> SendMonthlySummaryAsync(
        MonthlyReportSummaryDto report,
        string recipientPhone,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📊 *WarehousePOS — Executive Monthly Report*");
        sb.AppendLine($"🗓️ *Period:* {report.MonthLabel}\n");

        sb.AppendLine("💰 *Financial Overview:*");
        sb.AppendLine($"• Total Sales: Rs. {report.GrossSales:N2} ({report.TotalTransactions} txns)");
        sb.AppendLine($"• Discounts: Rs. {report.TotalDiscounts:N2}");
        sb.AppendLine($"• Net Revenue: Rs. {report.NetSales:N2}");
        sb.AppendLine($"• Expenses: Rs. {report.TotalExpenses:N2}");
        sb.AppendLine($"• *Net Profit:* *Rs. {report.NetProfit:N2}*\n");

        sb.AppendLine("📦 *Inventory Valuation:*");
        sb.AppendLine($"• Products: {report.TotalProducts:N0} ({report.TotalInventoryUnits:N0} total units)");
        sb.AppendLine($"• Valuation (Cost): Rs. {report.InventoryCostValuation:N2}");
        sb.AppendLine($"• Valuation (Retail): Rs. {report.InventoryRetailValuation:N2}\n");

        if (report.TopSellingProducts.Count > 0)
        {
            sb.AppendLine("⭐ *Top Sellers:*");
            int rank = 1;
            foreach (var top in report.TopSellingProducts.Take(5))
            {
                sb.AppendLine($"{rank++}. {top.Name} — {top.QuantitySold:N0} sold (Rs. {top.TotalRevenue:N2})");
            }
        }

        sb.AppendLine("\n_Full HTML report sent to registered owner email._");

        return await SendTextMessageAsync(recipientPhone, sb.ToString(), ct);
    }
}
