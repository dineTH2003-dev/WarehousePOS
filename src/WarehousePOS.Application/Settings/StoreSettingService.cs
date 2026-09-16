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
        var name    = await repo.GetValueAsync("STORE_NAME", ct) ?? "WAREHOUSE POS & WHOLESALE";
        var address = await repo.GetValueAsync("STORE_ADDRESS", ct) ?? "123 Main Street, Colombo, Sri Lanka";
        var phone   = await repo.GetValueAsync("STORE_PHONE", ct) ?? "Tel: 011-2345678 / 077-1234567";
        var tax     = await repo.GetValueAsync("STORE_TAX_REG", ct) ?? "VAT Reg: 123456789-9000";
        var footer  = await repo.GetValueAsync("STORE_FOOTER", ct) ?? "Thank you for your business!";

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
