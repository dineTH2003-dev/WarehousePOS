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
    private const string DefaultStoreName    = "HAPPY PRODUCTS";
    private const string DefaultStoreAddress = "Bandaragama Rd, Waskaduwa";
    private const string DefaultStorePhone   = "Tel: 0711435343";
    private const string DefaultStoreTaxReg  = "Damro, Abans, Singer, Soft Logic, Arpico Authorised Dealer";
    private const string DefaultFooterTerms  = "During the warranty period, all goods must be delivered to the manufacturing facility for repairs. The company warranty or corporate bill must be presented. Items cannot be returned after sale; items should be fully inspected and accepted upon receipt.";

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
    /// Formats receipt text for 80-column continuous tractor paper (Epson LQ-310 dot-matrix format).
    /// </summary>
    public static string FormatReceiptText(
        SaleDto sale,
        string storeName = DefaultStoreName,
        string storeAddress = DefaultStoreAddress,
        string storePhone = DefaultStorePhone,
        string footerMessage = DefaultFooterTerms,
        string taxRegNo = DefaultStoreTaxReg)
    {
        var sb = new StringBuilder();
        const int width = 80;

        string activeStoreName = string.IsNullOrWhiteSpace(storeName) ? DefaultStoreName : storeName;
        string activeStoreAddress = string.IsNullOrWhiteSpace(storeAddress) ? DefaultStoreAddress : storeAddress;
        string activeStorePhone = string.IsNullOrWhiteSpace(storePhone) ? DefaultStorePhone : storePhone;
        string activeTaxReg = string.IsNullOrWhiteSpace(taxRegNo) ? DefaultStoreTaxReg : taxRegNo;
        string activeFooter = string.IsNullOrWhiteSpace(footerMessage) ? DefaultFooterTerms : footerMessage;

        // 1. Header Logo & Store Title Banner
        sb.AppendLine(new string('=', width));
        sb.AppendLine(Center(activeStoreName.ToUpperInvariant(), width));
        if (!string.IsNullOrWhiteSpace(activeStoreAddress))
            sb.AppendLine(Center(activeStoreAddress, width));
        if (!string.IsNullOrWhiteSpace(activeStorePhone))
            sb.AppendLine(Center(activeStorePhone, width));
        if (!string.IsNullOrWhiteSpace(activeTaxReg))
            sb.AppendLine(Center(activeTaxReg, width));

        sb.AppendLine(Center("INVOICE  (Furniture & Electronic Stores)  Rg. No. B.B. 10500", width));
        sb.AppendLine(new string('=', width));

        // 2. Customer & Bill Information Box
        string custName = string.IsNullOrWhiteSpace(sale.CustomerName) ? "Walk-in Customer" : sale.CustomerName;
        string custPhone = string.IsNullOrWhiteSpace(sale.CustomerPhone) ? "-" : sale.CustomerPhone;
        string custAddr = string.IsNullOrWhiteSpace(sale.DeliveryAddress) ? "-" : sale.DeliveryAddress;
        string dateStr = sale.SaleDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        string invoiceNo = $"INV-{sale.Id:D6}";
        string payStr = sale.PaymentMethod.ToString();

        sb.AppendLine("+" + new string('-', 38) + "+" + new string('-', 39) + "+");
        sb.AppendLine($"| Customer: {TruncateOrPad(custName, 26),-26} | Tel No: {TruncateOrPad(custPhone, 31),-31} |");
        sb.AppendLine($"| Address : {TruncateOrPad(custAddr, 26),-26} | Date  : {TruncateOrPad(dateStr, 31),-31} |");
        sb.AppendLine($"| Invoice : {TruncateOrPad(invoiceNo, 26),-26} | Pay   : {TruncateOrPad(payStr, 31),-31} |");
        sb.AppendLine("+" + new string('-', 38) + "+" + new string('-', 39) + "+");

        // 3. Items Table Headers
        sb.AppendLine("+" + new string('-', 44) + "+" + new string('-', 6) + "+" + new string('-', 12) + "+" + new string('-', 13) + "+");
        sb.AppendLine("| Description                                 |  Qty. | Unit Price |   Total (Rs) |");
        sb.AppendLine("+" + new string('-', 44) + "+" + new string('-', 6) + "+" + new string('-', 12) + "+" + new string('-', 13) + "+");

        int idx = 1;
        foreach (var item in sale.Items)
        {
            string displayName = $"{idx++}. {item.ProductName}";
            List<string> nameLines = WrapText(displayName, 44);

            for (int i = 0; i < nameLines.Count; i++)
            {
                if (i == 0)
                {
                    sb.AppendLine(string.Format(
                        "|{0,-44}|{1,6}|{2,12:N2}|{3,13:N2}|",
                        nameLines[i],
                        item.Quantity,
                        item.UnitPrice,
                        item.LineTotal));
                }
                else
                {
                    sb.AppendLine(string.Format(
                        "|{0,-44}|{1,6}|{2,12}|{3,13}|",
                        nameLines[i],
                        "", "", ""));
                }
            }

            // Per-item Warranty note
            string warrantyStr = FormatWarrantyString(item.WarrantyYears, item.WarrantyMonths, item.WarrantyDays);
            if (!string.IsNullOrWhiteSpace(warrantyStr))
            {
                sb.AppendLine(string.Format(
                    "|   * Warranty: {0,-31}|{1,6}|{2,12}|{3,13}|",
                    warrantyStr, "", "", ""));
            }
        }

        sb.AppendLine("+" + new string('-', 44) + "+" + new string('-', 6) + "+" + new string('-', 12) + "+" + new string('-', 13) + "+");

        // 4. Totals & Payment Summary
        if (sale.DiscountAmount > 0)
        {
            sb.AppendLine(string.Format("| {0,48} : {1,25:N2} |", "Sub Total (Rs)", sale.SubTotal));
            sb.AppendLine(string.Format("| {0,48} : {1,25:N2} |", "Discount (Rs)", -sale.DiscountAmount));
        }

        sb.AppendLine(string.Format("| {0,48} : {1,25:N2} |", "Advance Payment (Rs)", sale.AmountPaid));
        sb.AppendLine(string.Format("| {0,48} : {1,25:N2} |", "Delivery Charge (Rs)", sale.DeliveryFee));
        sb.AppendLine(string.Format("| {0,48} : {1,25:N2} |", "TOTAL AMOUNT (Rs)", sale.TotalAmount));
        sb.AppendLine(string.Format("| {0,48} : {1,25:N2} |", "Amount Paid / Tendered (Rs)", sale.AmountPaid));

        if (sale.Change > 0)
        {
            sb.AppendLine(string.Format("| {0,48} : {1,25:N2} |", "Change Due (Rs)", sale.Change));
        }

        if (sale.Status == Domain.Enums.SaleStatus.AdvancePaid || sale.UnpaidAmount > 0)
        {
            decimal balDue = sale.UnpaidAmount > 0 ? sale.UnpaidAmount : Math.Max(0, sale.TotalAmount - sale.AmountPaid);
            string balLabel = sale.Status == Domain.Enums.SaleStatus.AdvancePaid ? "ADVANCE BAL DUE (Rs)" : "OUTSTANDING CREDIT (Rs)";
            sb.AppendLine(string.Format("| {0,48} : {1,25:N2} |", balLabel, balDue));
        }
        else if (sale.AmountPaid < sale.TotalAmount)
        {
            decimal creditDue = sale.TotalAmount - sale.AmountPaid;
            sb.AppendLine(string.Format("| {0,48} : {1,25:N2} |", "OUTSTANDING CREDIT (Rs)", creditDue));
        }

        sb.AppendLine("+" + new string('-', 76) + "+");
        sb.AppendLine();

        // 5. Terms and Conditions Footer
        sb.AppendLine(new string('=', width));
        sb.AppendLine("TERMS & CONDITIONS:");

        List<string> termsList = FormatTermsList(activeFooter);
        foreach (var term in termsList)
        {
            sb.AppendLine(term);
        }

        sb.AppendLine(new string('=', width));
        sb.AppendLine(Center("Thank you for your business!", width));
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
        if (purchase.DiscountAmount > 0)
        {
            sb.AppendLine(string.Format("{0,64} {1,15:N2}", "Sub Total:", purchase.SubTotal));
            sb.AppendLine(string.Format("{0,64} {1,15:N2}", "Discount:", -purchase.DiscountAmount));
        }
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

    private static string TruncateOrPad(string str, int length)
    {
        if (string.IsNullOrEmpty(str)) return new string(' ', length);
        if (str.Length > length) return str[..length];
        return str.PadRight(length);
    }

    private static string FormatWarrantyString(int years, int months, int days)
    {
        var parts = new List<string>();
        if (years > 0) parts.Add($"{years} {(years == 1 ? "Year" : "Years")}");
        if (months > 0) parts.Add($"{months} {(months == 1 ? "Month" : "Months")}");
        if (days > 0) parts.Add($"{days} {(days == 1 ? "Day" : "Days")}");

        if (parts.Count == 0) return string.Empty;
        return string.Join(" ", parts) + " Warranty";
    }

    private static List<string> FormatTermsList(string termsText)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(termsText))
            return result;

        var rawClauses = termsText
            .Split(new[] { "\r\n", "\n", ". " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        int itemNo = 1;
        foreach (var clause in rawClauses)
        {
            if (string.IsNullOrWhiteSpace(clause)) continue;

            string cleanClause = clause.TrimEnd('.');
            string prefix = $"{itemNo++}. ";
            int indent = prefix.Length;
            int maxLineLen = 78 - indent;

            var wrapped = WrapText(cleanClause + ".", maxLineLen);
            for (int i = 0; i < wrapped.Count; i++)
            {
                if (i == 0)
                    result.Add(prefix + wrapped[i]);
                else
                    result.Add(new string(' ', indent) + wrapped[i]);
            }
        }

        return result;
    }

    private static List<string> WrapText(string text, int maxCharsPerLine)
    {
        var lines = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return lines;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            if (currentLine.Length + (currentLine.Length > 0 ? 1 : 0) + word.Length <= maxCharsPerLine)
            {
                if (currentLine.Length > 0) currentLine.Append(' ');
                currentLine.Append(word);
            }
            else
            {
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                }

                if (word.Length > maxCharsPerLine)
                {
                    lines.Add(word[..maxCharsPerLine]);
                    currentLine.Append(word[maxCharsPerLine..]);
                }
                else
                {
                    currentLine.Append(word);
                }
            }
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine.ToString());

        return lines;
    }
}
