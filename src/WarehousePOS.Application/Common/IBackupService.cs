namespace WarehousePOS.Application.Common;

public interface IBackupService
{
    /// <summary>
    /// Creates an automatic or manual SQLite database backup in C:\ProgramData\WarehousePOS\Backups\
    /// </summary>
    Task<string> CreateBackupAsync(CancellationToken ct = default);

    /// <summary>
    /// Lists all existing backup files sorted by creation timestamp descending.
    /// </summary>
    IReadOnlyList<FileInfo> GetBackupFiles();

    /// <summary>
    /// Returns structured DTO information for existing local backups.
    /// </summary>
    IReadOnlyList<LocalBackupItemDto> GetLocalBackups();

    /// <summary>
    /// Deletes local backups beyond the specified retention count.
    /// </summary>
    void PruneOldLocalBackups(int keepCount = 30);
}
