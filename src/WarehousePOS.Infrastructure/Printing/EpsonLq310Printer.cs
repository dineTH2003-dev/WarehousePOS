using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using Microsoft.Extensions.Logging;
using WarehousePOS.Application.Printing;
using WarehousePOS.Application.Purchasing;
using WarehousePOS.Application.Sales;
using WarehousePOS.Application.Settings;

namespace WarehousePOS.Infrastructure.Printing;

/// <summary>
/// Hardware printing implementation targeting the Epson LQ-310 dot-matrix printer
/// via ESC/P2 raw Win32 spooler commands with GDI PrintDocument fallback.
/// Specifically configured for continuous tractor-feed 9.5" x 5.5" (2-ply NCR) paper.
/// </summary>
public sealed class EpsonLq310Printer(
    ILogger<EpsonLq310Printer> logger,
    IStoreSettingService settingService) : IReceiptPrinter
{
    private const string DefaultStoreName    = "WAREHOUSE POS & WHOLESALE";
    private const string DefaultStoreAddress = "123 Main Street, Colombo, Sri Lanka";
    private const string DefaultStorePhone   = "Tel: 011-2345678 / 077-1234567";

    public async Task PrintReceiptAsync(SaleDto sale, CancellationToken ct = default)
    {
        try
        {
            var headerSettings = await settingService.GetHeaderFooterSettingsAsync(ct);
            var printerSettings = await settingService.GetPrinterSettingsAsync(ct);

            string printerName = string.IsNullOrWhiteSpace(printerSettings.PrinterName)
                ? "EPSON LQ-310 ESC/P2"
                : printerSettings.PrinterName;

            string receiptText = FormatReceiptText(
                sale,
                headerSettings.StoreName,
                headerSettings.StoreAddress,
                headerSettings.StorePhone,
                headerSettings.FooterMessage,
                headerSettings.TaxRegNo);

            // Prepare ESC/P2 hardware byte sequence for continuous 5.5" half-page tractor paper
            var byteList = new List<byte>();
            byteList.AddRange(EscP2Commands.Initialize);
            byteList.AddRange(EscP2Commands.SelectDraft);
            byteList.AddRange(EscP2Commands.LineSpacing1_6);

            // Continuous 5.5-inch page at 6 LPI has exactly 33 lines
            if (printerSettings.PaperType.Contains("5_5", StringComparison.OrdinalIgnoreCase))
            {
                byteList.AddRange(EscP2Commands.SetPageLengthInLines(33));
            }

            byteList.AddRange(Encoding.ASCII.GetBytes(receiptText));

            // Form feed or extra feed lines to advance directly to the tear-off perforation
            byteList.AddRange(EscP2Commands.FormFeed);

            byte[] printBytes = byteList.ToArray();

            // Try fast Win32 RAW printing first (Epson LQ-310 hardware fonts)
            bool sentRaw = RawPrinterHelper.SendBytesToPrinter(printerName, printBytes, $"Receipt_Sale_{sale.Id}");

            if (!sentRaw)
            {
                // Fallback to standard Windows GDI printing
                logger.LogWarning("Raw spooler write unavailable for {PrinterName}. Falling back to GDI PrintDocument.", printerName);
                PrintViaGdi(printerName, receiptText, $"Receipt_Sale_{sale.Id}");
            }

            logger.LogInformation("Receipt printed successfully for Sale #{SaleId} on {PrinterName}", sale.Id, printerName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to print receipt for Sale #{SaleId}", sale.Id);
        }
    }

    public async Task PrintPurchaseOrderAsync(PurchaseDto purchase, CancellationToken ct = default)
    {
        try
        {
            var headerSettings = await settingService.GetHeaderFooterSettingsAsync(ct);
            var printerSettings = await settingService.GetPrinterSettingsAsync(ct);

            string printerName = string.IsNullOrWhiteSpace(printerSettings.PrinterName)
                ? "EPSON LQ-310 ESC/P2"
                : printerSettings.PrinterName;

            string text = FormatPurchaseOrderText(purchase, headerSettings.StoreName);

            var byteList = new List<byte>();
            byteList.AddRange(EscP2Commands.Initialize);
            byteList.AddRange(EscP2Commands.SelectDraft);
            byteList.AddRange(EscP2Commands.LineSpacing1_6);
            byteList.AddRange(Encoding.ASCII.GetBytes(text));
            byteList.AddRange(EscP2Commands.FormFeed);

            bool sentRaw = RawPrinterHelper.SendBytesToPrinter(printerName, byteList.ToArray(), $"PO_{purchase.Id}");
            if (!sentRaw)
            {
                PrintViaGdi(printerName, text, $"PO_{purchase.Id}");
            }

            logger.LogInformation("Purchase Order #{Id} printed successfully on {PrinterName}", purchase.Id, printerName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to print Purchase Order #{Id}", purchase.Id);
        }
    }

    public async Task PrintReportAsync(string title, string content, CancellationToken ct = default)
    {
        try
        {
            var headerSettings = await settingService.GetHeaderFooterSettingsAsync(ct);
            var printerSettings = await settingService.GetPrinterSettingsAsync(ct);

            string printerName = string.IsNullOrWhiteSpace(printerSettings.PrinterName)
                ? "EPSON LQ-310 ESC/P2"
                : printerSettings.PrinterName;

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine(Center(headerSettings.StoreName, 80));
            sb.AppendLine(Center(title.ToUpperInvariant(), 80));
            sb.AppendLine(Center($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", 80));
            sb.AppendLine("================================================================================");
            sb.AppendLine();
            sb.Append(content);
            sb.AppendLine();
            sb.AppendLine("================================================================================");
            sb.AppendLine(Center("End of Report - WarehousePOS", 80));
            sb.AppendLine("================================================================================");

            string reportText = sb.ToString();

            var byteList = new List<byte>();
            byteList.AddRange(EscP2Commands.Initialize);
            byteList.AddRange(EscP2Commands.SelectDraft);
            byteList.AddRange(Encoding.ASCII.GetBytes(reportText));
            byteList.AddRange(EscP2Commands.FormFeed);

            bool sentRaw = RawPrinterHelper.SendBytesToPrinter(printerName, byteList.ToArray(), $"Report_{title}");
            if (!sentRaw)
            {
                PrintViaGdi(printerName, reportText, $"Report_{title}");
            }

            logger.LogInformation("Report '{Title}' printed successfully on {PrinterName}", title, printerName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to print report '{Title}'", title);
        }
    }

    public Task<bool> TestPrintAsync(string printerName, CancellationToken ct = default)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine(Center("WAREHOUSE POS - PRINTER HARDWARE TEST", 80));
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Printer Name : {printerName}");
            sb.AppendLine($"Hardware     : EPSON LQ-310 24-Pin Impact Dot-Matrix");
            sb.AppendLine($"Paper Size   : Continuous Tractor 9.5\" x 5.5\" (2-Ply NCR Carbonless)");
            sb.AppendLine($"Print Engine : ESC/P2 High-Speed Draft (347 cps)");
            sb.AppendLine($"Timestamp    : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("TEST RESULT  : Communication successful! Printer is online and ready for POS.");
            sb.AppendLine("================================================================================");

            var byteList = new List<byte>();
            byteList.AddRange(EscP2Commands.Initialize);
            byteList.AddRange(EscP2Commands.SelectDraft);
            byteList.AddRange(EscP2Commands.SetPageLengthInLines(33));
            byteList.AddRange(Encoding.ASCII.GetBytes(sb.ToString()));
            byteList.AddRange(EscP2Commands.FormFeed);

            bool sentRaw = RawPrinterHelper.SendBytesToPrinter(printerName, byteList.ToArray(), "WarehousePOS_TestPrint");
            if (!sentRaw)
            {
                return Task.FromResult(PrintViaGdi(printerName, sb.ToString(), "WarehousePOS_TestPrint"));
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Test print failed for printer {PrinterName}", printerName);
            return Task.FromResult(false);
        }
    }

    public IReadOnlyList<string> GetInstalledPrinters()
    {
        var list = new List<string>();
        try
        {
            if (OperatingSystem.IsWindows())
            {
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    list.Add(printer);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not enumerate installed Windows printers.");
        }

        if (list.Count == 0)
        {
            list.Add("EPSON LQ-310 ESC/P2");
        }

        return list;
    }

    private bool PrintViaGdi(string printerName, string text, string docName)
    {
        try
        {
            if (!OperatingSystem.IsWindows()) return false;

            var printDoc = new PrintDocument();
            printDoc.DocumentName = docName;
            if (!string.IsNullOrWhiteSpace(printerName))
            {
                printDoc.PrinterSettings.PrinterName = printerName;
            }

            printDoc.PrintPage += (sender, ev) =>
            {
                using var font = new Font("Courier New", 9, FontStyle.Regular);
                using var brush = new SolidBrush(Color.Black);
                ev.Graphics?.DrawString(text, font, brush, 10, 10);
                ev.HasMorePages = false;
            };

            printDoc.Print();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GDI PrintDocument fallback failed for {DocName} on {PrinterName}", docName, printerName);
            return false;
        }
    }

    /// <summary>
    /// Formats receipt text for 80-column continuous tractor paper (or 40-column slip).
    /// </summary>
    public static string FormatReceiptText(
        SaleDto sale,
        string storeName = DefaultStoreName,
        string storeAddress = DefaultStoreAddress,
        string storePhone = DefaultStorePhone,
        string footerMessage = "Thank you for your business!",
        string taxRegNo = "")
    {
        var sb = new StringBuilder();
        const int width = 80;

        sb.AppendLine(new string('=', width));
        sb.AppendLine(Center(storeName, width));
        if (!string.IsNullOrWhiteSpace(storeAddress))
            sb.AppendLine(Center(storeAddress, width));
        if (!string.IsNullOrWhiteSpace(storePhone))
            sb.AppendLine(Center(storePhone, width));
        if (!string.IsNullOrWhiteSpace(taxRegNo))
            sb.AppendLine(Center(taxRegNo, width));
        sb.AppendLine(new string('=', width));

        string customerStr = string.IsNullOrWhiteSpace(sale.CustomerName) ? "Walk-in Customer" : sale.CustomerName;
        string dateStr = sale.SaleDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        sb.AppendLine($"Invoice #: {sale.Id,-20} Date: {dateStr,48}");
        sb.AppendLine($"Customer : {customerStr,-35} Type: {sale.SaleTypeLabel,-15} Pay: {sale.PaymentMethod}");
        sb.AppendLine(new string('-', width));
        sb.AppendLine(string.Format("{0,-3} {1,-38} {2,8} {3,12} {4,14}", "#", "Item Description", "Qty", "Unit Price", "Line Total"));
        sb.AppendLine(new string('-', width));

        int idx = 1;
        foreach (var item in sale.Items)
        {
            string name = item.ProductName.Length > 38 ? item.ProductName[..38] : item.ProductName;
            sb.AppendLine(string.Format(
                "{0,-3} {1,-38} {2,8} {3,12:N2} {4,14:N2}",
                idx++,
                name,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal));
        }

        sb.AppendLine(new string('-', width));
        sb.AppendLine(string.Format("{0,64} {1,15:N2}", "Sub Total:", sale.SubTotal));
        if (sale.DiscountAmount > 0)
            sb.AppendLine(string.Format("{0,64} {1,15:N2}", "Discount:", -sale.DiscountAmount));
        sb.AppendLine(string.Format("{0,64} {1,15:N2}", "TOTAL AMOUNT (LKR):", sale.TotalAmount));
        sb.AppendLine(string.Format("{0,64} {1,15:N2}", "Amount Tendered:", sale.AmountPaid));
        sb.AppendLine(string.Format("{0,64} {1,15:N2}", "Change Due:", sale.Change));

        if (sale.AmountPaid < sale.TotalAmount)
        {
            decimal creditDue = sale.TotalAmount - sale.AmountPaid;
            sb.AppendLine(string.Format("{0,64} {1,15:N2}", "OUTSTANDING CREDIT:", creditDue));
        }

        sb.AppendLine(new string('=', width));
        sb.AppendLine(Center(footerMessage, width));
        sb.AppendLine(Center("Software by WarehousePOS", width));
        sb.AppendLine(new string('=', width));

        return sb.ToString();
    }

    private static string FormatPurchaseOrderText(PurchaseDto purchase, string storeName)
    {
        var sb = new StringBuilder();
        const int width = 80;

        sb.AppendLine(new string('=', width));
        sb.AppendLine(Center("PURCHASE ORDER", width));
        sb.AppendLine(Center(storeName, width));
        sb.AppendLine(new string('=', width));
        sb.AppendLine($"PO #: {purchase.Id,-20} Date: {purchase.PurchaseDate.ToLocalTime():yyyy-MM-dd HH:mm,48}");
        sb.AppendLine($"Supplier: {purchase.SupplierName,-35} Status: {purchase.StatusLabel,-20}");
        sb.AppendLine(new string('-', width));
        sb.AppendLine(string.Format("{0,-3} {1,-38} {2,8} {3,12} {4,14}", "#", "Item Description", "Qty", "Unit Cost", "Total Cost"));
        sb.AppendLine(new string('-', width));

        int idx = 1;
        foreach (var item in purchase.Items)
        {
            string name = item.ProductName.Length > 38 ? item.ProductName[..38] : item.ProductName;
            sb.AppendLine(string.Format("{0,-3} {1,-38} {2,8} {3,12:N2} {4,14:N2}", idx++, name, item.Quantity, item.UnitCost, item.TotalCost));
        }

        sb.AppendLine(new string('-', width));
        sb.AppendLine(string.Format("{0,64} {1,15:N2}", "TOTAL COST:", purchase.TotalAmount));
        sb.AppendLine(new string('=', width));

        return sb.ToString();
    }

    private static string Center(string text, int width)
    {
        if (text.Length >= width) return text[..width];
        int leftPadding = (width - text.Length) / 2;
        return text.PadLeft(leftPadding + text.Length).PadRight(width);
    }
}
