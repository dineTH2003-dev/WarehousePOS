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
}
