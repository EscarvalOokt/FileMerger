namespace FileMerger.Wpf.Diagnostics;

public interface ICrashLogMaintenanceService
{
    string GetCrashLogDirectory();

    CrashLogFileListResult GetCrashLogFiles();

    CrashLogFolderOpenResult OpenCrashLogDirectory();

    CrashLogCleanupResult CleanupOldCrashLogs();

    CrashLogCleanupResult ClearAllCrashLogs();
}