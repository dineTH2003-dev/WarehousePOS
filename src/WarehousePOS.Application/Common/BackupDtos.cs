namespace WarehousePOS.Application.Common;

public record LocalBackupItemDto(
    string FilePath,
    string FileName,
    long SizeBytes,
    DateTime CreatedTimeUtc)
{
    public string SizeDisplay => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024.0):F2} MB"
    };

    public string DateDisplay => CreatedTimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
}

public record CloudBackupItemDto(
    string Id,
    string Name,
    long? SizeBytes,
    DateTime? CreatedTimeUtc)
{
    public string SizeDisplay => SizeBytes switch
    {
        null => "Unknown",
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes.Value / 1024.0:F1} KB",
        _ => $"{SizeBytes.Value / (1024.0 * 1024.0):F2} MB"
    };

    public string DateDisplay => CreatedTimeUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown";
}

public record CloudUploadResult(
    bool Success,
    string? FileId = null,
    string? FileName = null,
    string? ErrorMessage = null);

public record BackupSummaryDto(
    bool IsGoogleDriveConnected,
    string? AccountEmail,
    LocalBackupItemDto? LastLocalBackup,
    CloudBackupItemDto? LastCloudBackup,
    int TotalLocalBackups,
    int TotalCloudBackups);
