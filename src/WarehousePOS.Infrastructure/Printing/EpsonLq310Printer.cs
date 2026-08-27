using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using Microsoft.Extensions.Logging;
using WarehousePOS.Application.Printing;
using WarehousePOS.Application.Purchasing;
using WarehousePOS.Application.Sales;

namespace WarehousePOS.Infrastructure.Printing;

/// <summary>
/// Hardware printing implementation targeting the Epson LQ-310 dot-matrix printer
/// via the Windows Printing Subsystem (System.Drawing.Printing).
/// Formats receipts cleanly with header, itemized table, total summary, and footer.
/// </summary>
public sealed class EpsonLq310Printer(ILogger<EpsonLq310Printer> logger) : IReceiptPrinter
{
    private const string StoreName    = "WAREHOUSE POS & WHOLESALE";
    private const string StoreAddress = "123 Main Street, Colombo, Sri Lanka";
    private const string StorePhone   = "Tel: 011-2345678 / 077-1234567";

    public Task PrintReceiptAsync(SaleDto sale, CancellationToken ct = default)
    {
        try
        {
            var receiptText = FormatReceiptText(sale);

            var printDoc = new PrintDocument();
            printDoc.DocumentName = $"Receipt_Sale_{sale.Id}";

            printDoc.PrintPage += (sender, ev) =>
            {
                using var font = new Font("Courier New", 9, FontStyle.Regular);
                using var brush = new SolidBrush(Color.Black);
                ev.Graphics?.DrawString(receiptText, font, brush, 10, 10);
                ev.HasMorePages = false;
            };

            printDoc.Print();
            logger.LogInformation("Receipt printed successfully for Sale #{SaleId}", sale.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to print receipt for Sale #{SaleId} on Epson LQ-310", sale.Id);
            // Non-blocking for offline fallback: log error gracefully without crashing POS
        }

        return Task.CompletedTask;
    }

    public Task PrintPurchaseOrderAsync(PurchaseDto purchase, CancellationToken ct = default)
    {
        try
        {
            var text = FormatPurchaseOrderText(purchase);

            var printDoc = new PrintDocument();
            printDoc.DocumentName = $"PurchaseOrder_{purchase.Id}";

            printDoc.PrintPage += (sender, ev) =>
            {
                using var font = new Font("Courier New", 9, FontStyle.Regular);
                using var brush = new SolidBrush(Color.Black);
                ev.Graphics?.DrawString(text, font, brush, 10, 10);
                ev.HasMorePages = false;
            };

            printDoc.Print();
            logger.LogInformation("Purchase Order #{Id} printed successfully", purchase.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to print Purchase Order #{Id}", purchase.Id);
        }

        return Task.CompletedTask;
    }

    private static string FormatReceiptText(SaleDto sale)
    {
        var sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine(Center(StoreName, 40));
        sb.AppendLine(Center(StoreAddress, 40));
        sb.AppendLine(Center(StorePhone, 40));
        sb.AppendLine("========================================");
        sb.AppendLine($"Receipt #: {sale.Id,-10} Date: {sale.SaleDate.ToLocalTime():yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Customer : {sale.CustomerName,-25}");
        sb.AppendLine($"Sale Type: {sale.SaleTypeLabel,-25}");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine(string.Format("{0,-18} {1,4} {2,7} {3,8}", "Item", "Qty", "Price", "Total"));
        sb.AppendLine("----------------------------------------");

        foreach (var item in sale.Items)
        {
            string name = item.ProductName.Length > 18 ? item.ProductName[..18] : item.ProductName;
            sb.AppendLine(string.Format("{0,-18} {1,4} {2,7:N0} {3,8:N2}", name, item.Quantity, item.UnitPrice, item.LineTotal));
        }

        sb.AppendLine("----------------------------------------");
        sb.AppendLine(string.Format("{0,-28} {1,10:N2}", "Sub Total:", sale.SubTotal));
        if (sale.DiscountAmount > 0)
            sb.AppendLine(string.Format("{0,-28} {1,10:N2}", "Discount:", -sale.DiscountAmount));
        sb.AppendLine(string.Format("{0,-28} {1,10:N2}", "TOTAL AMOUNT:", sale.TotalAmount));
        sb.AppendLine(string.Format("{0,-28} {1,10:N2}", "Amount Paid:", sale.AmountPaid));
        sb.AppendLine(string.Format("{0,-28} {1,10:N2}", "Change Due:", sale.Change));
        sb.AppendLine("========================================");
        sb.AppendLine(Center("Thank you for your business!", 40));
        sb.AppendLine(Center("Software by WarehousePOS", 40));
        sb.AppendLine("========================================");

        return sb.ToString();
    }

    private static string FormatPurchaseOrderText(PurchaseDto purchase)
    {
        var sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine(Center("PURCHASE ORDER", 40));
        sb.AppendLine(Center(StoreName, 40));
        sb.AppendLine("========================================");
        sb.AppendLine($"PO #: {purchase.Id,-12} Date: {purchase.PurchaseDate.ToLocalTime():yyyy-MM-dd}");
        sb.AppendLine($"Supplier: {purchase.SupplierName,-25}");
        sb.AppendLine($"Status  : {purchase.StatusLabel,-25}");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine(string.Format("{0,-18} {1,4} {2,7} {3,8}", "Item", "Qty", "Cost", "Total"));
        sb.AppendLine("----------------------------------------");

        foreach (var item in purchase.Items)
        {
            string name = item.ProductName.Length > 18 ? item.ProductName[..18] : item.ProductName;
            sb.AppendLine(string.Format("{0,-18} {1,4} {2,7:N0} {3,8:N2}", name, item.Quantity, item.UnitCost, item.TotalCost));
        }

        sb.AppendLine("----------------------------------------");
        sb.AppendLine(string.Format("{0,-28} {1,10:N2}", "TOTAL COST:", purchase.TotalAmount));
        sb.AppendLine("========================================");

        return sb.ToString();
    }

    private static string Center(string text, int width)
    {
        if (text.Length >= width) return text[..width];
        int leftPadding = (width - text.Length) / 2;
        return text.PadLeft(leftPadding + text.Length).PadRight(width);
    }
}
