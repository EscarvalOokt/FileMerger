using FileMerger.Wpf.Diagnostics;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeCrashLogMaintenanceService :
    ICrashLogMaintenanceService
{
    public string DirectoryPath { get; set; } =
        @"C:\FileMerger\CrashLogs";

    public List<CrashLogFileInfo> Files { get; } = [];

    public int GetDirectoryCalls { get; private set; }

    public int GetFilesCalls { get; private set; }

    public int OpenCalls { get; private set; }

    public int CleanupOldCalls { get; private set; }

    public int ClearAllCalls { get; private set; }

    public CrashLogFileListResult? FileListResult { get; set; }

    public CrashLogFolderOpenResult? OpenResult { get; set; }

    public CrashLogCleanupResult? CleanupOldResult { get; set; }

    public CrashLogCleanupResult? ClearAllResult { get; set; }

    public string GetCrashLogDirectory()
    {
        GetDirectoryCalls++;
        return DirectoryPath;
    }

    public CrashLogFileListResult GetCrashLogFiles()
    {
        GetFilesCalls++;
        return FileListResult ??
               CrashLogFileListResult.Success([.. Files]);
    }

    public CrashLogFolderOpenResult OpenCrashLogDirectory()
    {
        OpenCalls++;
        return OpenResult ??
               CrashLogFolderOpenResult.Success(DirectoryPath);
    }

    public CrashLogCleanupResult CleanupOldCrashLogs()
    {
        CleanupOldCalls++;
        return CleanupOldResult ??
               CrashLogCleanupResult.From([], []);
    }

    public CrashLogCleanupResult ClearAllCrashLogs()
    {
        ClearAllCalls++;
        return ClearAllResult ??
               CrashLogCleanupResult.From([], []);
    }
}