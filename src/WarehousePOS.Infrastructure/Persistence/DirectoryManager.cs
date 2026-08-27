using System.Runtime.InteropServices;

namespace WarehousePOS.Infrastructure.Persistence;

public static class DirectoryManager
{
    public static string GetBaseDataDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WarehousePOS");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".warehousepos");
    }

    public static string GetDatabasePath()
    {
        var baseDir = GetBaseDataDirectory();
        return Path.Combine(baseDir, "Data", "WarehousePOS.db");
    }

    public static string GetBackupDirectory()
    {
        var baseDir = GetBaseDataDirectory();
        return Path.Combine(baseDir, "Backups");
    }

    public static string GetLogFilePath()
    {
        var baseDir = GetBaseDataDirectory();
        return Path.Combine(baseDir, "Logs", "application.log");
    }

    public static void EnsureDirectoriesExist()
    {
        var baseDir = GetBaseDataDirectory();
        Directory.CreateDirectory(Path.Combine(baseDir, "Data"));
        Directory.CreateDirectory(Path.Combine(baseDir, "Backups"));
        Directory.CreateDirectory(Path.Combine(baseDir, "Logs"));
    }
}
