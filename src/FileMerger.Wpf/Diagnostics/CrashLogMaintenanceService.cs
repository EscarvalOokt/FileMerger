using System.IO;
using System.Security;
using FileMerger.Wpf.Features.Settings;
using FileMerger.Wpf.Shared.Integration;

namespace FileMerger.Wpf.Diagnostics;

public sealed class CrashLogMaintenanceService : ICrashLogMaintenanceService
{
    private const string CrashLogSearchPattern = "crash-*.log";

    private readonly CrashLogPathPolicy _pathPolicy;
    private readonly IApplicationPreferencesStore _applicationPreferencesStore;
    private readonly IFileSystemLauncher _fileSystemLauncher;

    public CrashLogMaintenanceService(
        CrashLogPathPolicy pathPolicy,
        IApplicationPreferencesStore applicationPreferencesStore,
        IFileSystemLauncher fileSystemLauncher)
    {
        ArgumentNullException.ThrowIfNull(pathPolicy);
        ArgumentNullException.ThrowIfNull(applicationPreferencesStore);
        ArgumentNullException.ThrowIfNull(fileSystemLauncher);

        _pathPolicy = pathPolicy;
        _applicationPreferencesStore = applicationPreferencesStore;
        _fileSystemLauncher = fileSystemLauncher;
    }

    public string GetCrashLogDirectory()
    {
        return _pathPolicy.GetCrashLogDirectory();
    }

    public CrashLogFileListResult GetCrashLogFiles()
    {
        string directory = GetCrashLogDirectory();

        if (!Directory.Exists(directory))
            return CrashLogFileListResult.Success([]);

        try
        {
            List<CrashLogFileInfo> files = [];

            foreach (string path in Directory.EnumerateFiles(
                         directory,
                         CrashLogSearchPattern,
                         SearchOption.TopDirectoryOnly))
            {
                try
                {
                    FileInfo file = new(path);
                    file.Refresh();

                    if (!file.Exists)
                        continue;

                    files.Add(new CrashLogFileInfo(
                        Path: file.FullName,
                        FileName: file.Name,
                        LastWriteTimeUtc: file.LastWriteTimeUtc,
                        SizeInBytes: file.Length));
                }
                catch (FileNotFoundException)
                {
                    // The file was deleted after directory enumeration.
                }
                catch (DirectoryNotFoundException)
                {
                    // The directory or file was deleted after enumeration.
                }
            }

            CrashLogFileInfo[] orderedFiles =
            [
                .. files
                    .OrderByDescending(x => x.LastWriteTimeUtc)
                    .ThenByDescending(
                        x => x.FileName,
                        StringComparer.OrdinalIgnoreCase)
            ];

            return CrashLogFileListResult.Success(orderedFiles);
        }
        catch (DirectoryNotFoundException)
        {
            return CrashLogFileListResult.Success([]);
        }
        catch (Exception ex) when (IsExpectedFileSystemException(ex))
        {
            return CrashLogFileListResult.Failure(ex);
        }
    }

    public CrashLogFolderOpenResult OpenCrashLogDirectory()
    {
        string directory = GetCrashLogDirectory();

        try
        {
            Directory.CreateDirectory(directory);
            _fileSystemLauncher.OpenDirectory(directory);

            return CrashLogFolderOpenResult.Success(directory);
        }
        catch (Exception ex)
        {
            return CrashLogFolderOpenResult.Failure(directory, ex);
        }
    }

    public CrashLogCleanupResult CleanupOldCrashLogs()
    {
        CrashLogFileListResult filesResult = GetCrashLogFiles();
        if (!filesResult.IsSuccessful)
            return CleanupFailureFrom(filesResult);

        int retentionLimit =
            _applicationPreferencesStore.Current.CrashLogRetentionLimit;

        CrashLogFileInfo[] filesToDelete =
        [
            .. filesResult.Files.Skip(retentionLimit)
        ];

        return DeleteFiles(filesToDelete);
    }

    public CrashLogCleanupResult ClearAllCrashLogs()
    {
        CrashLogFileListResult filesResult = GetCrashLogFiles();
        if (!filesResult.IsSuccessful)
            return CleanupFailureFrom(filesResult);

        return DeleteFiles(filesResult.Files);
    }

    private CrashLogCleanupResult CleanupFailureFrom(
        CrashLogFileListResult filesResult)
    {
        Exception error = filesResult.Error
                          ?? new IOException("Failed to enumerate crash log files.");

        CrashLogMaintenanceFailure[] failures =
        [
            new CrashLogMaintenanceFailure(
                Path: GetCrashLogDirectory(),
                Error: error)
        ];

        return CrashLogCleanupResult.From([], failures);
    }

    private static CrashLogCleanupResult DeleteFiles(
        IEnumerable<CrashLogFileInfo> files)
    {
        List<string> deletedPaths = [];
        List<CrashLogMaintenanceFailure> failures = [];

        foreach (CrashLogFileInfo file in files)
        {
            try
            {
                File.Delete(file.Path);
                deletedPaths.Add(file.Path);
            }
            catch (FileNotFoundException)
            {
                // The file was already removed by another process.
            }
            catch (DirectoryNotFoundException)
            {
                // The crash log directory was already removed.
            }
            catch (Exception ex) when (IsExpectedFileSystemException(ex))
            {
                failures.Add(new CrashLogMaintenanceFailure(file.Path, ex));
            }
        }

        return CrashLogCleanupResult.From(deletedPaths, failures);
    }

    private static bool IsExpectedFileSystemException(Exception exception)
    {
        return exception is IOException or
            UnauthorizedAccessException or
            NotSupportedException or
            SecurityException;
    }
}