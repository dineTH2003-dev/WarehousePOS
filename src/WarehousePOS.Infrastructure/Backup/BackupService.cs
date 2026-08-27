using Microsoft.Extensions.Logging;
using WarehousePOS.Application.Common;

namespace WarehousePOS.Infrastructure.Backup;

public sealed class BackupService(
    string dbFilePath,
    ILogger<BackupService> logger) : IBackupService
{
    private static readonly string BackupDirectory =
        OperatingSystem.IsWindows()
            ? @"C:\ProgramData\WarehousePOS\Backups\"
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".warehousepos", "Backups");

    public Task<string> CreateBackupAsync(CancellationToken ct = default)
    {
        if (!File.Exists(dbFilePath))
            throw new FileNotFoundException($"Database file not found at: {dbFilePath}");

        Directory.CreateDirectory(BackupDirectory);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"WarehousePOS_Backup_{timestamp}.db";
        var destinationPath = Path.Combine(BackupDirectory, backupFileName);

        File.Copy(dbFilePath, destinationPath, overwrite: true);

        logger.LogInformation("Database backup created successfully at {Path}", destinationPath);
        return Task.FromResult(destinationPath);
    }

    public IReadOnlyList<FileInfo> GetBackupFiles()
    {
        if (!Directory.Exists(BackupDirectory))
            return Array.Empty<FileInfo>();

        var dir = new DirectoryInfo(BackupDirectory);
        return dir.GetFiles("*.db")
                  .OrderByDescending(f => f.CreationTimeUtc)
                  .ToList();
    }
}
