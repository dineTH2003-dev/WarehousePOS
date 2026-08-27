using WarehousePOS.Application.Settings;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Settings;

public sealed class StoreSettingsViewModel : ViewModelBase
{
    private readonly IStoreSettingService _settingService;

    private string _storeName = string.Empty;
    private string _storeAddress = string.Empty;
    private string _storePhone = string.Empty;
    private string _taxRegNo = string.Empty;
    private string _footerMessage = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public string StoreName
    {
        get => _storeName;
        set => SetField(ref _storeName, value);
    }

    public string StoreAddress
    {
        get => _storeAddress;
        set => SetField(ref _storeAddress, value);
    }

    public string StorePhone
    {
        get => _storePhone;
        set => SetField(ref _storePhone, value);
    }

    public string TaxRegNo
    {
        get => _taxRegNo;
        set => SetField(ref _taxRegNo, value);
    }

    public string FooterMessage
    {
        get => _footerMessage;
        set => SetField(ref _footerMessage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public RelayCommand SaveCommand { get; }

    public StoreSettingsViewModel(IStoreSettingService settingService)
    {
        _settingService = settingService;
        SaveCommand = new RelayCommand(async () => await SaveSettingsAsync());
    }

    public async Task LoadSettingsAsync()
    {
        IsBusy = true;
        try
        {
            var dto = await _settingService.GetHeaderFooterSettingsAsync();
            StoreName = dto.StoreName;
            StoreAddress = dto.StoreAddress;
            StorePhone = dto.StorePhone;
            TaxRegNo = dto.TaxRegNo;
            FooterMessage = dto.FooterMessage;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveSettingsAsync()
    {
        StatusMessage = string.Empty;
        try
        {
            var dto = new StoreHeaderFooterDto(StoreName, StoreAddress, StorePhone, TaxRegNo, FooterMessage);
            await _settingService.SaveHeaderFooterSettingsAsync(dto);
            StatusMessage = "Settings saved successfully! Receipts will now use these updated details.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}
