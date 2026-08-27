using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Settings;

public sealed record StoreHeaderFooterDto(
    string StoreName,
    string StoreAddress,
    string StorePhone,
    string TaxRegNo,
    string FooterMessage);

public interface IStoreSettingService
{
    Task<StoreHeaderFooterDto> GetHeaderFooterSettingsAsync(CancellationToken ct = default);
    Task SaveHeaderFooterSettingsAsync(StoreHeaderFooterDto dto, CancellationToken ct = default);
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
}
