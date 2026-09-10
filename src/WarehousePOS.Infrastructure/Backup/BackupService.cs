using System.IO.Compression;
using Microsoft.Extensions.Logging;
using WarehousePOS.Application.Common;

namespace WarehousePOS.Infrastructure.Backup;

public sealed class BackupService(
    string dbFilePath,
    ILogger<BackupService> logger,
    string? backupDirectory = null) : IBackupService
{
    private static readonly string DefaultBackupDirectory =
        OperatingSystem.IsWindows()
            ? @"C:\ProgramData\WarehousePOS\Backups\"
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".warehousepos", "Backups");

    private const string MainBackupFileName = "WarehousePOS_Backup.zip";

    private readonly string _backupDirectory = backupDirectory ?? DefaultBackupDirectory;

    public Task<string> CreateBackupAsync(CancellationToken ct = default)
    {
        if (!File.Exists(dbFilePath))
            throw new FileNotFoundException($"Database file not found at: {dbFilePath}");

        Directory.CreateDirectory(_backupDirectory);

        var destinationZipPath = Path.Combine(_backupDirectory, MainBackupFileName);
        var tempZipPath = Path.Combine(_backupDirectory, $"WarehousePOS_Backup_temp_{Guid.NewGuid():N}.zip");
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"WarehousePOS_temp_{Guid.NewGuid():N}.db");

        try
        {
            // 1. Create a clean staging copy of the database
            File.Copy(dbFilePath, tempDbPath, overwrite: true);

            // 2. Compress into temporary zip
            using (var zipArchive = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
            {
                zipArchive.CreateEntryFromFile(tempDbPath, "WarehousePOS.db", CompressionLevel.Optimal);
            }

            // 3. Atomically overwrite the single main backup file
            File.Move(tempZipPath, destinationZipPath, overwrite: true);

            logger.LogInformation("Database backup updated successfully at {Path}", destinationZipPath);
            return Task.FromResult(destinationZipPath);
        }
        finally
        {
            if (File.Exists(tempDbPath))
            {
                try { File.Delete(tempDbPath); } catch { /* best effort */ }
            }
            if (File.Exists(tempZipPath))
            {
                try { File.Delete(tempZipPath); } catch { /* best effort */ }
            }
        }
    }

    public IReadOnlyList<FileInfo> GetBackupFiles()
    {
        if (!Directory.Exists(_backupDirectory))
            return Array.Empty<FileInfo>();

        var dir = new DirectoryInfo(_backupDirectory);
        return dir.GetFiles("WarehousePOS_Backup*.zip")
                  .OrderByDescending(f => f.LastWriteTimeUtc)
                  .ToList();
    }

    public IReadOnlyList<LocalBackupItemDto> GetLocalBackups()
    {
        var files = GetBackupFiles();
        return files.Select(f => new LocalBackupItemDto(
            f.FullName,
            f.Name,
            f.Length,
            f.LastWriteTimeUtc)).ToList();
    }

    public void PruneOldLocalBackups(int keepCount = 30)
    {
        // Single backup file strategy: No automatic deletion
    }
}
