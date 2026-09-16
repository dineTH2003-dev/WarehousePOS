using WarehousePOS.Application.Purchasing;
using WarehousePOS.Application.Sales;

namespace WarehousePOS.Application.Printing;

public interface IReceiptPrinter
{
    /// <summary>
    /// Prints a sales receipt/invoice formatted for dot-matrix printers (Epson LQ-310)
    /// or standard Windows print queue.
    /// </summary>
    Task PrintReceiptAsync(SaleDto sale, CancellationToken ct = default);

    /// <summary>
    /// Prints a purchase order document.
    /// </summary>
    Task PrintPurchaseOrderAsync(PurchaseDto purchase, CancellationToken ct = default);

    /// <summary>
    /// Prints a report document formatted for dot-matrix 80-column printing.
    /// </summary>
    Task PrintReportAsync(string title, string content, CancellationToken ct = default);

    /// <summary>
    /// Sends a diagnostic test print to the specified printer name.
    /// </summary>
    Task<bool> TestPrintAsync(string printerName, CancellationToken ct = default);

    /// <summary>
    /// Gets list of printer device names installed on the host system.
    /// </summary>
    IReadOnlyList<string> GetInstalledPrinters();
}
