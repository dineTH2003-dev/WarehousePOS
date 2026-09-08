namespace WarehousePOS.Application.Common;

/// <summary>
/// Service abstraction for cloud backups (Google Drive).
/// </summary>
public interface ICloudBackupService
{
    /// <summary>
    /// Checks whether Google Drive credentials / client secrets are configured.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Checks whether the user is currently authenticated with Google Drive.
    /// </summary>
    Task<bool> IsConnectedAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the connected Google account email, or null if not connected.
    /// </summary>
    Task<string?> GetConnectedAccountEmailAsync(CancellationToken ct = default);

    /// <summary>
    /// Initiates interactive or credentials-based authentication with Google Drive.
    /// </summary>
    Task<bool> AuthenticateAsync(string? clientId = null, string? clientSecret = null, CancellationToken ct = default);

    /// <summary>
    /// Disconnects and removes stored Google Drive credentials.
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Uploads a local backup file (typically .zip) to the dedicated Google Drive folder.
    /// Reports progress percentage from 0 to 100.
    /// </summary>
    Task<CloudUploadResult> UploadBackupAsync(string localFilePath, IProgress<int>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Lists all backup files in the Google Drive folder sorted descending by creation date.
    /// </summary>
    Task<IReadOnlyList<CloudBackupItemDto>> GetCloudBackupsAsync(CancellationToken ct = default);

    /// <summary>
    /// Indicates whether a local backup is waiting to be uploaded to Google Drive when internet reconnects.
    /// </summary>
    bool IsSyncPending { get; }

    /// <summary>
    /// Descriptive reason or error for the pending sync (e.g. offline).
    /// </summary>
    string? PendingSyncReason { get; }

    /// <summary>
    /// Raised when sync pending status changes.
    /// </summary>
    event Action? SyncStatusChanged;

    /// <summary>
    /// Prunes backups in Google Drive older than the retention limit (e.g. keeping the last 30).
    /// </summary>
    Task PruneOldCloudBackupsAsync(int keepCount = 30, CancellationToken ct = default);
}
