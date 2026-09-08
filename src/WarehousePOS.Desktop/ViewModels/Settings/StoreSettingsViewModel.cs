using System.Collections.ObjectModel;
using WarehousePOS.Application.Common;
using WarehousePOS.Application.Notifications;
using WarehousePOS.Application.Settings;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Settings;

public sealed class StoreSettingsViewModel : ViewModelBase
{
    private readonly IStoreSettingService _settingService;
    private readonly IBackupService _backupService;
    private readonly ICloudBackupService _cloudBackupService;
    private readonly INotificationOrchestrator _notificationOrchestrator;

    // Store settings fields
    private string _storeName = string.Empty;
    private string _storeAddress = string.Empty;
    private string _storePhone = string.Empty;
    private string _taxRegNo = string.Empty;
    private string _footerMessage = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    // Backup & Cloud settings fields
    private bool _isGoogleDriveConnected;
    private string _connectedEmail = string.Empty;
    private string _clientIdInput = string.Empty;
    private string _clientSecretInput = string.Empty;
    private bool _isBackupInProgress;
    private int _backupProgress;
    private string _backupStatusText = string.Empty;
    private string _lastLocalBackupDisplay = "No local backups found";
    private string _lastCloudBackupDisplay = "No cloud backups found";

    // Notifications fields (Brevo & WhatsApp)
    private string _brevoApiKey = string.Empty;
    private string _brevoSenderEmail = string.Empty;
    private string _brevoSenderName = "WarehousePOS";
    private string _ownerEmail = string.Empty;
    private bool _isEmailLowStockAlertEnabled = true;
    private bool _isEmailMonthlyReportEnabled = true;

    private bool _isWhatsAppEnabled;
    private string _ownerPhone = string.Empty;
    private string _whatsAppGatewayUrl = string.Empty;
    private string _whatsAppApiKey = string.Empty;
    private string _notificationStatusMessage = string.Empty;
    private bool _isNotificationBusy;

    public string StoreName { get => _storeName; set => SetField(ref _storeName, value); }
    public string StoreAddress { get => _storeAddress; set => SetField(ref _storeAddress, value); }
    public string StorePhone { get => _storePhone; set => SetField(ref _storePhone, value); }
    public string TaxRegNo { get => _taxRegNo; set => SetField(ref _taxRegNo, value); }
    public string FooterMessage { get => _footerMessage; set => SetField(ref _footerMessage, value); }
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }
    public bool IsBusy { get => _isBusy; private set => SetField(ref _isBusy, value); }

    // Cloud & Backup Properties
    public bool IsGoogleDriveConnected { get => _isGoogleDriveConnected; private set => SetField(ref _isGoogleDriveConnected, value); }
    public string ConnectedEmail { get => _connectedEmail; private set => SetField(ref _connectedEmail, value); }
    public string ClientIdInput { get => _clientIdInput; set => SetField(ref _clientIdInput, value); }
    public string ClientSecretInput { get => _clientSecretInput; set => SetField(ref _clientSecretInput, value); }
    public bool IsBackupInProgress { get => _isBackupInProgress; private set => SetField(ref _isBackupInProgress, value); }
    public int BackupProgress { get => _backupProgress; private set => SetField(ref _backupProgress, value); }
    public string BackupStatusText { get => _backupStatusText; private set => SetField(ref _backupStatusText, value); }
    public string LastLocalBackupDisplay { get => _lastLocalBackupDisplay; private set => SetField(ref _lastLocalBackupDisplay, value); }
    public string LastCloudBackupDisplay { get => _lastCloudBackupDisplay; private set => SetField(ref _lastCloudBackupDisplay, value); }

    // Notifications Properties
    public string BrevoApiKey { get => _brevoApiKey; set => SetField(ref _brevoApiKey, value); }
    public string BrevoSenderEmail { get => _brevoSenderEmail; set => SetField(ref _brevoSenderEmail, value); }
    public string BrevoSenderName { get => _brevoSenderName; set => SetField(ref _brevoSenderName, value); }
    public string OwnerEmail { get => _ownerEmail; set => SetField(ref _ownerEmail, value); }
    public bool IsEmailLowStockAlertEnabled { get => _isEmailLowStockAlertEnabled; set => SetField(ref _isEmailLowStockAlertEnabled, value); }
    public bool IsEmailMonthlyReportEnabled { get => _isEmailMonthlyReportEnabled; set => SetField(ref _isEmailMonthlyReportEnabled, value); }

    public bool IsWhatsAppEnabled { get => _isWhatsAppEnabled; set => SetField(ref _isWhatsAppEnabled, value); }
    public string OwnerPhone { get => _ownerPhone; set => SetField(ref _ownerPhone, value); }
    public string WhatsAppGatewayUrl { get => _whatsAppGatewayUrl; set => SetField(ref _whatsAppGatewayUrl, value); }
    public string WhatsAppApiKey { get => _whatsAppApiKey; set => SetField(ref _whatsAppApiKey, value); }
    public string NotificationStatusMessage { get => _notificationStatusMessage; set => SetField(ref _notificationStatusMessage, value); }
    public bool IsNotificationBusy { get => _isNotificationBusy; private set => SetField(ref _isNotificationBusy, value); }

    private int _selectedTabIndex = 0;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetField(ref _selectedTabIndex, value))
            {
                OnPropertyChanged(nameof(IsStoreTabActive));
                OnPropertyChanged(nameof(IsBackupTabActive));
                OnPropertyChanged(nameof(IsNotificationTabActive));
            }
        }
    }

    public bool IsStoreTabActive
    {
        get => _selectedTabIndex == 0;
        set
        {
            if (value) SelectedTabIndex = 0;
        }
    }

    public bool IsBackupTabActive
    {
        get => _selectedTabIndex == 1;
        set
        {
            if (value) SelectedTabIndex = 1;
        }
    }

    public bool IsNotificationTabActive
    {
        get => _selectedTabIndex == 2;
        set
        {
            if (value) SelectedTabIndex = 2;
        }
    }

    public bool IsSyncPending => _cloudBackupService.IsSyncPending;
    public string? PendingSyncReason => _cloudBackupService.PendingSyncReason;

    public ObservableCollection<LocalBackupItemDto> RecentLocalBackups { get; } = [];
    public ObservableCollection<CloudBackupItemDto> RecentCloudBackups { get; } = [];

    // Commands
    public RelayCommand SelectStoreTabCommand { get; }
    public RelayCommand SelectBackupTabCommand { get; }
    public RelayCommand SelectNotificationTabCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand BackupNowCommand { get; }
    public RelayCommand ConnectDriveCommand { get; }
    public RelayCommand DisconnectDriveCommand { get; }
    public RelayCommand RefreshBackupsCommand { get; }
    public RelayCommand SaveNotificationSettingsCommand { get; }
    public RelayCommand SendTestEmailCommand { get; }
    public RelayCommand SendTestWhatsAppCommand { get; }
    public RelayCommand SendMonthlyReportNowCommand { get; }
    public RelayCommand CheckLowStockAlertsNowCommand { get; }

    public StoreSettingsViewModel(
        IStoreSettingService settingService,
        IBackupService backupService,
        ICloudBackupService cloudBackupService,
        INotificationOrchestrator notificationOrchestrator)
    {
        _settingService = settingService;
        _backupService = backupService;
        _cloudBackupService = cloudBackupService;
        _notificationOrchestrator = notificationOrchestrator;

        _cloudBackupService.SyncStatusChanged += () =>
        {
            OnPropertyChanged(nameof(IsSyncPending));
            OnPropertyChanged(nameof(PendingSyncReason));
        };

        SelectStoreTabCommand = new RelayCommand(() => SelectedTabIndex = 0);
        SelectBackupTabCommand = new RelayCommand(() => SelectedTabIndex = 1);
        SelectNotificationTabCommand = new RelayCommand(() => SelectedTabIndex = 2);

        SaveCommand = new RelayCommand(async () => await SaveSettingsAsync());
        BackupNowCommand = new RelayCommand(async () => await ExecuteBackupAsync(), () => !IsBackupInProgress);
        ConnectDriveCommand = new RelayCommand(async () => await ConnectGoogleDriveAsync(), () => !IsBackupInProgress);
        DisconnectDriveCommand = new RelayCommand(async () => await DisconnectGoogleDriveAsync(), () => !IsBackupInProgress);
        RefreshBackupsCommand = new RelayCommand(async () => await RefreshBackupListsAsync(), () => !IsBackupInProgress);

        SaveNotificationSettingsCommand = new RelayCommand(async () => await SaveNotificationSettingsAsync(), () => !IsNotificationBusy);
        SendTestEmailCommand = new RelayCommand(async () => await SendTestEmailAsync(), () => !IsNotificationBusy);
        SendTestWhatsAppCommand = new RelayCommand(async () => await SendTestWhatsAppAsync(), () => !IsNotificationBusy);
        SendMonthlyReportNowCommand = new RelayCommand(async () => await SendMonthlyReportNowAsync(), () => !IsNotificationBusy);
        CheckLowStockAlertsNowCommand = new RelayCommand(async () => await CheckLowStockAlertsNowAsync(), () => !IsNotificationBusy);
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

            await CheckGoogleDriveStatusAsync();
            await RefreshBackupListsAsync();
            await LoadNotificationSettingsAsync();
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

    private async Task CheckGoogleDriveStatusAsync()
    {
        try
        {
            IsGoogleDriveConnected = await _cloudBackupService.IsConnectedAsync();
            if (IsGoogleDriveConnected)
            {
                var email = await _cloudBackupService.GetConnectedAccountEmailAsync();
                ConnectedEmail = email ?? "Connected User";
            }
            else
            {
                ConnectedEmail = string.Empty;
            }
        }
        catch
        {
            IsGoogleDriveConnected = false;
            ConnectedEmail = string.Empty;
        }
    }

    public async Task RefreshBackupListsAsync()
    {
        // 1. Refresh local backups
        try
        {
            var localFiles = _backupService.GetLocalBackups();
            RecentLocalBackups.Clear();
            foreach (var item in localFiles.Take(10))
            {
                RecentLocalBackups.Add(item);
            }

            LastLocalBackupDisplay = localFiles.Count > 0
                ? $"{localFiles[0].FileName} ({localFiles[0].DateDisplay})"
                : "No local backups found";
        }
        catch
        {
            LastLocalBackupDisplay = "Error reading local backups";
        }

        // 2. Refresh cloud backups if connected
        if (IsGoogleDriveConnected)
        {
            try
            {
                var cloudFiles = await _cloudBackupService.GetCloudBackupsAsync();
                RecentCloudBackups.Clear();
                foreach (var item in cloudFiles.Take(10))
                {
                    RecentCloudBackups.Add(item);
                }

                LastCloudBackupDisplay = cloudFiles.Count > 0
                    ? $"{cloudFiles[0].Name} ({cloudFiles[0].DateDisplay})"
                    : "No cloud backups found on Google Drive";
            }
            catch
            {
                LastCloudBackupDisplay = "Unable to fetch Google Drive backups (Check Internet connection)";
            }
        }
        else
        {
            RecentCloudBackups.Clear();
            LastCloudDisplayNotConnected();
        }
    }

    private void LastCloudDisplayNotConnected()
    {
        LastCloudBackupDisplay = "Google Drive not connected";
    }

    private async Task ConnectGoogleDriveAsync()
    {
        IsBackupInProgress = true;
        BackupStatusText = "Connecting to Google Drive in browser...";
        try
        {
            var success = await _cloudBackupService.AuthenticateAsync(ClientIdInput, ClientSecretInput);
            if (success)
            {
                await CheckGoogleDriveStatusAsync();
                BackupStatusText = $"Connected successfully to Google Drive ({ConnectedEmail})!";
                await RefreshBackupListsAsync();
            }
            else
            {
                BackupStatusText = "Failed to connect to Google Drive. Please check credentials or network.";
            }
        }
        catch (Exception ex)
        {
            BackupStatusText = $"Connection error: {ex.Message}";
        }
        finally
        {
            IsBackupInProgress = false;
        }
    }

    private async Task DisconnectGoogleDriveAsync()
    {
        await _cloudBackupService.DisconnectAsync();
        IsGoogleDriveConnected = false;
        ConnectedEmail = string.Empty;
        BackupStatusText = "Disconnected from Google Drive.";
        await RefreshBackupListsAsync();
    }

    public async Task ExecuteBackupAsync()
    {
        IsBackupInProgress = true;
        BackupProgress = 10;
        BackupStatusText = "Creating local compressed database backup...";

        try
        {
            // Step 1: Create local compressed .zip backup
            var localZipPath = await _backupService.CreateBackupAsync();
            BackupProgress = 40;
            BackupStatusText = $"Local backup created: {System.IO.Path.GetFileName(localZipPath)}";

            await RefreshBackupListsAsync();

            // Step 2: Upload to Google Drive if connected
            if (IsGoogleDriveConnected)
            {
                BackupStatusText = "Uploading backup to Google Drive folder 'WarehousePOS_Backups'...";
                var progressHandler = new Progress<int>(p =>
                {
                    BackupProgress = 40 + (int)(p * 0.6);
                });

                var uploadResult = await _cloudBackupService.UploadBackupAsync(localZipPath, progressHandler);

                if (uploadResult.Success)
                {
                    BackupProgress = 100;
                    BackupStatusText = $"Backup completed! Successfully uploaded {uploadResult.FileName} to Google Drive.";
                    await RefreshBackupListsAsync();
                }
                else
                {
                    BackupProgress = 100;
                    BackupStatusText = $"Local backup saved, but Google Drive sync failed: {uploadResult.ErrorMessage}";
                }
            }
            else
            {
                BackupProgress = 100;
                BackupStatusText = "Local backup created successfully! (Connect Google Drive to enable cloud sync).";
            }
        }
        catch (Exception ex)
        {
            BackupStatusText = $"Backup error: {ex.Message}";
        }
        finally
        {
            IsBackupInProgress = false;
        }
    }

    public async Task LoadNotificationSettingsAsync()
    {
        try
        {
            var s = await _notificationOrchestrator.GetSettingsAsync();
            BrevoApiKey = s.BrevoApiKey;
            BrevoSenderEmail = s.BrevoSenderEmail;
            BrevoSenderName = s.BrevoSenderName;
            OwnerEmail = s.OwnerEmail;
            IsEmailLowStockAlertEnabled = s.IsEmailLowStockAlertEnabled;
            IsEmailMonthlyReportEnabled = s.IsEmailMonthlyReportEnabled;
            IsWhatsAppEnabled = s.IsWhatsAppEnabled;
            OwnerPhone = s.OwnerPhone;
            WhatsAppGatewayUrl = s.WhatsAppGatewayUrl;
            WhatsAppApiKey = s.WhatsAppApiKey;
        }
        catch (Exception ex)
        {
            NotificationStatusMessage = $"Failed to load notification settings: {ex.Message}";
        }
    }

    public async Task SaveNotificationSettingsAsync()
    {
        IsNotificationBusy = true;
        NotificationStatusMessage = "Saving notification settings...";
        try
        {
            var dto = new NotificationSettingsDto(
                BrevoApiKey,
                BrevoSenderEmail,
                BrevoSenderName,
                OwnerEmail,
                IsEmailLowStockAlertEnabled,
                IsEmailMonthlyReportEnabled,
                IsWhatsAppEnabled,
                OwnerPhone,
                WhatsAppGatewayUrl,
                WhatsAppApiKey);

            await _notificationOrchestrator.SaveSettingsAsync(dto);
            NotificationStatusMessage = "Notification settings saved successfully!";
        }
        catch (Exception ex)
        {
            NotificationStatusMessage = $"Failed to save settings: {ex.Message}";
        }
        finally
        {
            IsNotificationBusy = false;
        }
    }

    public async Task SendTestEmailAsync()
    {
        IsNotificationBusy = true;
        NotificationStatusMessage = "Sending test email via Brevo...";
        try
        {
            await SaveNotificationSettingsAsync();
            var (ok, msg) = await _notificationOrchestrator.SendTestEmailAsync(OwnerEmail);
            NotificationStatusMessage = ok ? "✅ Test email sent! Please check your inbox." : $"❌ {msg}";
        }
        catch (Exception ex)
        {
            NotificationStatusMessage = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsNotificationBusy = false;
        }
    }

    public async Task SendTestWhatsAppAsync()
    {
        IsNotificationBusy = true;
        NotificationStatusMessage = "Sending test WhatsApp message...";
        try
        {
            await SaveNotificationSettingsAsync();
            var (ok, msg) = await _notificationOrchestrator.SendTestWhatsAppAsync(OwnerPhone);
            NotificationStatusMessage = ok ? "✅ Test WhatsApp message sent!" : $"❌ {msg}";
        }
        catch (Exception ex)
        {
            NotificationStatusMessage = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsNotificationBusy = false;
        }
    }

    public async Task SendMonthlyReportNowAsync()
    {
        IsNotificationBusy = true;
        NotificationStatusMessage = "Generating and dispatching monthly report...";
        try
        {
            await SaveNotificationSettingsAsync();
            var (ok, msg) = await _notificationOrchestrator.CheckAndSendMonthlyReportAsync(force: true);
            NotificationStatusMessage = ok ? $"✅ {msg}" : $"❌ {msg}";
        }
        catch (Exception ex)
        {
            NotificationStatusMessage = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsNotificationBusy = false;
        }
    }

    public async Task CheckLowStockAlertsNowAsync()
    {
        IsNotificationBusy = true;
        NotificationStatusMessage = "Checking inventory levels and dispatching alerts...";
        try
        {
            await SaveNotificationSettingsAsync();
            var (ok, msg) = await _notificationOrchestrator.CheckAndSendLowStockAlertsAsync(force: true);
            NotificationStatusMessage = ok ? $"✅ {msg}" : $"❌ {msg}";
        }
        catch (Exception ex)
        {
            NotificationStatusMessage = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsNotificationBusy = false;
        }
    }
}
