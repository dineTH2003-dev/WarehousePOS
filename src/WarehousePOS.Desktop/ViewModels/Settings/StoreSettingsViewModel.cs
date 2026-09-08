using System.Collections.ObjectModel;
using WarehousePOS.Application.Common;
using WarehousePOS.Application.Settings;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Settings;

public sealed class StoreSettingsViewModel : ViewModelBase
{
    private readonly IStoreSettingService _settingService;
    private readonly IBackupService _backupService;
    private readonly ICloudBackupService _cloudBackupService;

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

    public bool IsSyncPending => _cloudBackupService.IsSyncPending;
    public string? PendingSyncReason => _cloudBackupService.PendingSyncReason;

    public ObservableCollection<LocalBackupItemDto> RecentLocalBackups { get; } = [];
    public ObservableCollection<CloudBackupItemDto> RecentCloudBackups { get; } = [];

    // Commands
    public RelayCommand SaveCommand { get; }
    public RelayCommand BackupNowCommand { get; }
    public RelayCommand ConnectDriveCommand { get; }
    public RelayCommand DisconnectDriveCommand { get; }
    public RelayCommand RefreshBackupsCommand { get; }

    public StoreSettingsViewModel(
        IStoreSettingService settingService,
        IBackupService backupService,
        ICloudBackupService cloudBackupService)
    {
        _settingService = settingService;
        _backupService = backupService;
        _cloudBackupService = cloudBackupService;

        _cloudBackupService.SyncStatusChanged += () =>
        {
            OnPropertyChanged(nameof(IsSyncPending));
            OnPropertyChanged(nameof(PendingSyncReason));
        };

        SaveCommand = new RelayCommand(async () => await SaveSettingsAsync());
        BackupNowCommand = new RelayCommand(async () => await ExecuteBackupAsync(), () => !IsBackupInProgress);
        ConnectDriveCommand = new RelayCommand(async () => await ConnectGoogleDriveAsync(), () => !IsBackupInProgress);
        DisconnectDriveCommand = new RelayCommand(async () => await DisconnectGoogleDriveAsync(), () => !IsBackupInProgress);
        RefreshBackupsCommand = new RelayCommand(async () => await RefreshBackupListsAsync(), () => !IsBackupInProgress);
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
}
