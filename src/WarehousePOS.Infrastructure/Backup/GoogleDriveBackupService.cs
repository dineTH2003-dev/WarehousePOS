using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using WarehousePOS.Application.Common;

namespace WarehousePOS.Infrastructure.Backup;

public sealed class GoogleDriveBackupService : ICloudBackupService
{
    private const string BackupFolderName = "WarehousePOS_Backups";
    private static readonly string[] Scopes = [DriveService.Scope.DriveFile];

    private readonly ILogger<GoogleDriveBackupService> _logger;
    private readonly string _secretsDirectory;
    private readonly string _tokenStoreDirectory;
    private readonly string _clientSecretPath;
    private readonly string _credentialsConfigPath;

    public GoogleDriveBackupService(ILogger<GoogleDriveBackupService> logger)
    {
        _logger = logger;

        var baseAppData = OperatingSystem.IsWindows()
            ? @"C:\ProgramData\WarehousePOS"
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".warehousepos");

        _secretsDirectory = Path.Combine(baseAppData, "Secrets");
        _tokenStoreDirectory = Path.Combine(_secretsDirectory, "GoogleDriveTokens");
        _clientSecretPath = Path.Combine(_secretsDirectory, "client_secret.json");
        _credentialsConfigPath = Path.Combine(_secretsDirectory, "google_credentials.txt");

        Directory.CreateDirectory(_secretsDirectory);
    }

    public bool IsSyncPending { get; private set; }
    public string? PendingSyncReason { get; private set; }
    public event Action? SyncStatusChanged;

    public bool IsConfigured =>
        File.Exists(_clientSecretPath) || File.Exists(_credentialsConfigPath);

    public async Task<bool> IsConnectedAsync(CancellationToken ct = default)
    {
        try
        {
            var credential = await GetUserCredentialAsync(ct);
            return credential is not null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetConnectedAccountEmailAsync(CancellationToken ct = default)
    {
        try
        {
            var service = await GetDriveServiceAsync(ct);
            if (service is null) return null;

            var aboutReq = service.About.Get();
            aboutReq.Fields = "user(emailAddress,displayName)";
            var about = await aboutReq.ExecuteAsync(ct);
            return about?.User?.EmailAddress ?? about?.User?.DisplayName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve Google Drive account email");
            return null;
        }
    }

    public async Task<bool> AuthenticateAsync(string? clientId = null, string? clientSecret = null, CancellationToken ct = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
            {
                var content = $"{clientId.Trim()}{Environment.NewLine}{clientSecret.Trim()}";
                await File.WriteAllTextAsync(_credentialsConfigPath, content, ct);
            }

            var credential = await GetUserCredentialAsync(ct);
            if (credential is null) return false;

            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "WarehousePOS"
            });

            var aboutReq = service.About.Get();
            aboutReq.Fields = "user(emailAddress)";
            var about = await aboutReq.ExecuteAsync(ct);

            _logger.LogInformation("Successfully connected to Google Drive account: {Email}", about?.User?.EmailAddress);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Drive authentication failed");
            return false;
        }
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        try
        {
            if (Directory.Exists(_tokenStoreDirectory))
            {
                Directory.Delete(_tokenStoreDirectory, recursive: true);
            }
            IsSyncPending = false;
            PendingSyncReason = null;
            SyncStatusChanged?.Invoke();
            _logger.LogInformation("Disconnected from Google Drive and cleared local tokens");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while disconnecting Google Drive");
        }

        return Task.CompletedTask;
    }

    public async Task<CloudUploadResult> UploadBackupAsync(string localFilePath, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        if (!File.Exists(localFilePath))
        {
            return new CloudUploadResult(false, null, null, $"Local backup file not found at: {localFilePath}");
        }

        try
        {
            var service = await GetDriveServiceAsync(ct);
            if (service is null)
            {
                return new CloudUploadResult(false, null, null, "Google Drive is not authenticated. Please connect your Google account in Settings.");
            }

            var folderId = await GetOrCreateBackupFolderIdAsync(service, ct);
            var fileName = Path.GetFileName(localFilePath);

            // Check if the single main backup file already exists on Google Drive
            var findReq = service.Files.List();
            findReq.Q = $"name = '{fileName}' and '{folderId}' in parents and trashed = false";
            findReq.Fields = "files(id, name)";
            var findResult = await findReq.ExecuteAsync(ct);
            var existingFile = findResult?.Files?.FirstOrDefault();

            await using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
            ResumableUpload<Google.Apis.Drive.v3.Data.File, Google.Apis.Drive.v3.Data.File> uploadRequest;

            if (existingFile is not null)
            {
                // Update the existing single main backup file
                var updateRequest = service.Files.Update(new Google.Apis.Drive.v3.Data.File(), existingFile.Id, fileStream, "application/zip");
                updateRequest.Fields = "id, name, size, modifiedTime";
                uploadRequest = updateRequest;
            }
            else
            {
                // Create the single main backup file on Google Drive
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = [folderId]
                };
                var createRequest = service.Files.Create(fileMetadata, fileStream, "application/zip");
                createRequest.Fields = "id, name, size, modifiedTime";
                uploadRequest = createRequest;
            }

            uploadRequest.ProgressChanged += uploadProgress =>
            {
                if (uploadProgress.Status == UploadStatus.Uploading)
                {
                    var percent = (int)((uploadProgress.BytesSent * 100) / fileStream.Length);
                    progress?.Report(percent);
                }
                else if (uploadProgress.Status == UploadStatus.Completed)
                {
                    progress?.Report(100);
                }
            };

            var uploadResult = await uploadRequest.UploadAsync(ct);

            if (uploadResult.Status == UploadStatus.Completed)
            {
                var uploadedFile = uploadRequest.ResponseBody;
                _logger.LogInformation("Successfully updated {FileName} on Google Drive (ID: {FileId})", fileName, uploadedFile?.Id ?? existingFile?.Id);

                IsSyncPending = false;
                PendingSyncReason = null;
                SyncStatusChanged?.Invoke();

                return new CloudUploadResult(true, uploadedFile?.Id ?? existingFile?.Id, fileName, null);
            }

            var error = uploadResult.Exception?.Message ?? "Upload did not complete successfully.";
            _logger.LogWarning("Google Drive upload failed: {Error}", error);

            IsSyncPending = true;
            PendingSyncReason = "Internet connection offline. Local backup is safe; cloud backup will auto-upload when reconnected.";
            SyncStatusChanged?.Invoke();

            return new CloudUploadResult(false, null, fileName, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Google Drive upload for {Path}", localFilePath);

            IsSyncPending = true;
            PendingSyncReason = "Internet connection offline. Local backup is safe; cloud backup will auto-upload when reconnected.";
            SyncStatusChanged?.Invoke();

            return new CloudUploadResult(false, null, Path.GetFileName(localFilePath), ex.Message);
        }
    }

    public async Task<IReadOnlyList<CloudBackupItemDto>> GetCloudBackupsAsync(CancellationToken ct = default)
    {
        try
        {
            var service = await GetDriveServiceAsync(ct);
            if (service is null) return [];

            var folderId = await GetOrCreateBackupFolderIdAsync(service, ct);

            var listRequest = service.Files.List();
            listRequest.Q = $"'{folderId}' in parents and trashed = false";
            listRequest.Fields = "files(id, name, size, createdTime)";
            listRequest.OrderBy = "createdTime desc";
            listRequest.PageSize = 100;

            var result = await listRequest.ExecuteAsync(ct);
            if (result?.Files is null) return [];

            return result.Files.Select(f => new CloudBackupItemDto(
                f.Id,
                f.Name,
                f.Size,
                f.CreatedTimeDateTimeOffset?.UtcDateTime)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list cloud backups from Google Drive");
            return [];
        }
    }

    public Task PruneOldCloudBackupsAsync(int keepCount = 30, CancellationToken ct = default)
    {
        // Single backup file strategy: No automatic deletion
        return Task.CompletedTask;
    }

    // ── Internal Helpers ──────────────────────────────────────────────────

    private async Task<UserCredential?> GetUserCredentialAsync(CancellationToken ct)
    {
        ClientSecrets? secrets = null;

        if (File.Exists(_clientSecretPath))
        {
            await using var stream = new FileStream(_clientSecretPath, FileMode.Open, FileAccess.Read);
            secrets = (await GoogleClientSecrets.FromStreamAsync(stream, ct)).Secrets;
        }
        else if (File.Exists(_credentialsConfigPath))
        {
            var lines = await File.ReadAllLinesAsync(_credentialsConfigPath, ct);
            if (lines.Length >= 2)
            {
                secrets = new ClientSecrets
                {
                    ClientId = lines[0].Trim(),
                    ClientSecret = lines[1].Trim()
                };
            }
        }

        if (secrets is null)
        {
            return null;
        }

        Directory.CreateDirectory(_tokenStoreDirectory);
        var dataStore = new FileDataStore(_tokenStoreDirectory, fullPath: true);

        return await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            Scopes,
            "user",
            ct,
            dataStore);
    }

    private async Task<DriveService?> GetDriveServiceAsync(CancellationToken ct)
    {
        var credential = await GetUserCredentialAsync(ct);
        if (credential is null) return null;

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "WarehousePOS"
        });
    }

    private async Task<string> GetOrCreateBackupFolderIdAsync(DriveService service, CancellationToken ct)
    {
        var listReq = service.Files.List();
        listReq.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{BackupFolderName}' and trashed = false";
        listReq.Fields = "files(id, name)";
        var list = await listReq.ExecuteAsync(ct);

        if (list?.Files?.Count > 0)
        {
            return list.Files[0].Id;
        }

        var folderMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = BackupFolderName,
            MimeType = "application/vnd.google-apps.folder"
        };

        var folder = await service.Files.Create(folderMetadata).ExecuteAsync(ct);
        _logger.LogInformation("Created Google Drive folder '{Folder}' with ID {Id}", BackupFolderName, folder.Id);
        return folder.Id;
    }
}
