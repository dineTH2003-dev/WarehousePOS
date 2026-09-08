using System.IO.Compression;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WarehousePOS.Infrastructure.Backup;
using Xunit;

namespace WarehousePOS.Infrastructure.Tests;

public sealed class BackupServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dummyDbPath;

    public BackupServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"warehousepos_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _dummyDbPath = Path.Combine(_tempDir, "test.db");
        File.WriteAllText(_dummyDbPath, "SQLite format 3 dummy header and content");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldCreateZipFileWithDatabase()
    {
        // Arrange
        var service = new BackupService(_dummyDbPath, NullLogger<BackupService>.Instance);

        // Act
        var backupZipPath = await service.CreateBackupAsync();

        // Assert
        File.Exists(backupZipPath).Should().BeTrue();
        Path.GetExtension(backupZipPath).Should().Be(".zip");

        // Verify content inside the zip
        using (var archive = ZipFile.OpenRead(backupZipPath))
        {
            var dbEntry = archive.GetEntry("WarehousePOS.db");
            dbEntry.Should().NotBeNull();
            dbEntry!.Length.Should().BeGreaterThan(0);
        }

        // Verify GetLocalBackups returns the created file
        var localBackups = service.GetLocalBackups();
        localBackups.Should().Contain(b => b.FilePath == backupZipPath);

        // Cleanup created backup
        try { File.Delete(backupZipPath); } catch { }
    }

    [Fact]
    public async Task CreateBackupAsync_NonExistentDb_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var service = new BackupService(Path.Combine(_tempDir, "nonexistent.db"), NullLogger<BackupService>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.CreateBackupAsync());
    }
}
