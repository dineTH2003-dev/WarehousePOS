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
}
