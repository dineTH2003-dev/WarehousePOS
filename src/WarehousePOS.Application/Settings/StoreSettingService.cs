using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Settings;

public sealed record StoreHeaderFooterDto(
    string StoreName,
    string StoreAddress,
    string StorePhone,
    string TaxRegNo,
    string FooterMessage);

public sealed record PrinterSettingsDto(
    string PrinterName,
    bool AutoPrintEnabled,
    string PaperType,
    int FeedLines);

public interface IStoreSettingService
{
    Task<StoreHeaderFooterDto> GetHeaderFooterSettingsAsync(CancellationToken ct = default);
    Task SaveHeaderFooterSettingsAsync(StoreHeaderFooterDto dto, CancellationToken ct = default);
    Task<PrinterSettingsDto> GetPrinterSettingsAsync(CancellationToken ct = default);
    Task SavePrinterSettingsAsync(PrinterSettingsDto dto, CancellationToken ct = default);
}

public sealed class StoreSettingService(IStoreSettingRepository repo) : IStoreSettingService
{
    public async Task<StoreHeaderFooterDto> GetHeaderFooterSettingsAsync(CancellationToken ct = default)
    {
        var name    = await repo.GetValueAsync("STORE_NAME", ct) ?? "HAPPY PRODUCTS";
        var address = await repo.GetValueAsync("STORE_ADDRESS", ct) ?? "Bandaragama Rd, Waskaduwa";
        var phone   = await repo.GetValueAsync("STORE_PHONE", ct) ?? "Tel: 0711435343";
        var tax     = await repo.GetValueAsync("STORE_TAX_REG", ct) ?? await repo.GetValueAsync("STORE_TAX_NO", ct) ?? "Damro, Abans, Singer, Soft Logic, Arpico Authorised Dealer";
        var footer  = await repo.GetValueAsync("STORE_FOOTER", ct) ?? await repo.GetValueAsync("RECEIPT_FOOTER", ct) ?? "During the warranty period, all goods must be delivered to the manufacturing facility for repairs. The company warranty or corporate bill must be presented. Items cannot be returned after sale; items should be fully inspected and accepted upon receipt.";

        if (name.Contains("WAREHOUSEPOS", StringComparison.OrdinalIgnoreCase)) name = "HAPPY PRODUCTS";
        if (address.Contains("123 Main Street", StringComparison.OrdinalIgnoreCase)) address = "Bandaragama Rd, Waskaduwa";
        if (phone.Contains("+94 11 234 5678", StringComparison.OrdinalIgnoreCase)) phone = "Tel: 0711435343";
        if (tax.Contains("VAT-12345678-0000", StringComparison.OrdinalIgnoreCase) || tax.Contains("Rg. No. B.B. 10500", StringComparison.OrdinalIgnoreCase)) tax = "Damro, Abans, Singer, Soft Logic, Arpico Authorised Dealer";
        if (footer.Contains("Please come again", StringComparison.OrdinalIgnoreCase))
            footer = "During the warranty period, all goods must be delivered to the manufacturing facility for repairs. The company warranty or corporate bill must be presented. Items cannot be returned after sale; items should be fully inspected and accepted upon receipt.";

        return new StoreHeaderFooterDto(name, address, phone, tax, footer);
    }

    public async Task SaveHeaderFooterSettingsAsync(StoreHeaderFooterDto dto, CancellationToken ct = default)
    {
        await repo.SetValueAsync("STORE_NAME", dto.StoreName, "Store Name for receipt header", ct);
        await repo.SetValueAsync("STORE_ADDRESS", dto.StoreAddress, "Store Address for receipt header", ct);
        await repo.SetValueAsync("STORE_PHONE", dto.StorePhone, "Store Phone for receipt header", ct);
        await repo.SetValueAsync("STORE_TAX_REG", dto.TaxRegNo, "VAT/Tax registration number", ct);
        await repo.SetValueAsync("STORE_FOOTER", dto.FooterMessage, "Receipt footer message", ct);
    }

    public async Task<PrinterSettingsDto> GetPrinterSettingsAsync(CancellationToken ct = default)
    {
        var printerName = await repo.GetValueAsync("PRINTER_NAME", ct) ?? "EPSON LQ-310 ESC/P2";
        var autoPrintStr = await repo.GetValueAsync("PRINTER_AUTO_PRINT", ct) ?? "true";
        var paperType = await repo.GetValueAsync("PRINTER_PAPER_TYPE", ct) ?? "Continuous_5_5_Inch";
        var feedLinesStr = await repo.GetValueAsync("PRINTER_FEED_LINES", ct) ?? "3";

        bool autoPrint = !bool.TryParse(autoPrintStr, out var ap) || ap;
        int feedLines = int.TryParse(feedLinesStr, out var fl) ? fl : 3;

        return new PrinterSettingsDto(printerName, autoPrint, paperType, feedLines);
    }

    public async Task SavePrinterSettingsAsync(PrinterSettingsDto dto, CancellationToken ct = default)
    {
        await repo.SetValueAsync("PRINTER_NAME", dto.PrinterName, "Selected Windows printer device name", ct);
        await repo.SetValueAsync("PRINTER_AUTO_PRINT", dto.AutoPrintEnabled ? "true" : "false", "Auto-print bill on checkout", ct);
        await repo.SetValueAsync("PRINTER_PAPER_TYPE", dto.PaperType, "Continuous paper size type", ct);
        await repo.SetValueAsync("PRINTER_FEED_LINES", dto.FeedLines.ToString(), "Feed lines after receipt for tear-off", ct);
    }
}
