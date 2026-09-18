using System.IO;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Wpf.Diagnostics;
using FileMerger.Wpf.Features.Settings;

namespace FileMerger.Tests.Wpf.Diagnostics;

public sealed class CrashLogMaintenanceServiceTests : IDisposable
{
    private readonly CrashLogPathPolicy _pathPolicy;
    private readonly string _tempRoot;

    public CrashLogMaintenanceServiceTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FileMerger.Tests",
            nameof(CrashLogMaintenanceServiceTests),
            Guid.NewGuid().ToString("N"));

        _pathPolicy = new CrashLogPathPolicy(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Fact]
    public void GetCrashLogDirectory_Should_Return_Path_From_CrashLogPathPolicy()
    {
        CrashLogMaintenanceService service = CreateService();

        string result = service.GetCrashLogDirectory();

        Assert.Equal(_pathPolicy.GetCrashLogDirectory(), result);
    }

    [Fact]
    public void GetCrashLogFiles_Should_Return_Empty_List_When_Directory_Does_Not_Exist()
    {
        CrashLogMaintenanceService service = CreateService();

        CrashLogFileListResult result = service.GetCrashLogFiles();

        Assert.True(result.IsSuccessful);
        Assert.Empty(result.Files);
        Assert.Null(result.Error);
    }

    [Fact]
    public void GetCrashLogFiles_Should_Return_Crash_Log_Files_Only()
    {
        string first = CreateFile("crash-20260701-120000-000-aaaaaaaa.log");
        string second = CreateFile("crash-20260702-120000-000-bbbbbbbb.log");
        CreateFile("notes.log");
        CreateFile("crash-other.txt");
        CreateFile("random.txt");

        CrashLogMaintenanceService service = CreateService();

        CrashLogFileListResult result = service.GetCrashLogFiles();

        Assert.True(result.IsSuccessful);
        Assert.Equal(2, result.Files.Count);
        Assert.Contains(result.Files, x => x.Path == first);
        Assert.Contains(result.Files, x => x.Path == second);
        Assert.All(result.Files, x => Assert.StartsWith("crash-", x.FileName));
        Assert.All(result.Files, x => Assert.EndsWith(".log", x.FileName));
    }

    [Fact]
    public void GetCrashLogFiles_Should_Order_Files_By_LastWriteTimeUtc_Descending()
    {
        string oldest = CreateFile(
            "crash-20260701-120000-000-aaaaaaaa.log",
            new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
        string middle = CreateFile(
            "crash-20260702-120000-000-bbbbbbbb.log",
            new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc));
        string newest = CreateFile(
            "crash-20260703-120000-000-cccccccc.log",
            new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc));

        CrashLogMaintenanceService service = CreateService();

        CrashLogFileListResult result = service.GetCrashLogFiles();

        Assert.True(result.IsSuccessful);
        Assert.Equal([newest, middle, oldest], result.Files.Select(x => x.Path));
    }

    [Fact]
    public void CleanupOldCrashLogs_Should_Keep_Newest_Files_By_Retention_Limit()
    {
        string oldest = CreateFile(
            "crash-20260701-120000-000-aaaaaaaa.log",
            new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
        string middle = CreateFile(
            "crash-20260702-120000-000-bbbbbbbb.log",
            new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc));
        string newest = CreateFile(
            "crash-20260703-120000-000-cccccccc.log",
            new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc));

        CrashLogMaintenanceService service = CreateService(retentionLimit: 2);

        CrashLogCleanupResult result = service.CleanupOldCrashLogs();

        Assert.True(result.IsSuccessful);
        Assert.Equal(1, result.DeletedCount);
        Assert.Equal(oldest, Assert.Single(result.DeletedPaths));
        Assert.False(File.Exists(oldest));
        Assert.True(File.Exists(middle));
        Assert.True(File.Exists(newest));
    }

    [Fact]
    public void CleanupOldCrashLogs_Should_Not_Delete_When_File_Count_Is_Within_Retention_Limit()
    {
        string first = CreateFile("crash-20260701-120000-000-aaaaaaaa.log");
        string second = CreateFile("crash-20260702-120000-000-bbbbbbbb.log");

        CrashLogMaintenanceService service = CreateService(retentionLimit: 2);

        CrashLogCleanupResult result = service.CleanupOldCrashLogs();

        Assert.True(result.IsSuccessful);
        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.DeletedPaths);
        Assert.True(File.Exists(first));
        Assert.True(File.Exists(second));
    }

    [Fact]
    public void CleanupOldCrashLogs_Should_Return_Success_When_Directory_Does_Not_Exist()
    {
        CrashLogMaintenanceService service = CreateService(retentionLimit: 2);

        CrashLogCleanupResult result = service.CleanupOldCrashLogs();

        Assert.True(result.IsSuccessful);
        Assert.Equal(0, result.DeletedCount);
        Assert.Empty(result.DeletedPaths);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void ClearAllCrashLogs_Should_Delete_All_Crash_Log_Files()
    {
        string first = CreateFile("crash-20260701-120000-000-aaaaaaaa.log");
        string second = CreateFile("crash-20260702-120000-000-bbbbbbbb.log");
        string third = CreateFile("crash-20260703-120000-000-cccccccc.log");

        CrashLogMaintenanceService service = CreateService();

        CrashLogCleanupResult result = service.ClearAllCrashLogs();

        Assert.True(result.IsSuccessful);
        Assert.Equal(3, result.DeletedCount);
        Assert.False(File.Exists(first));
        Assert.False(File.Exists(second));
        Assert.False(File.Exists(third));
        Assert.True(Directory.Exists(_pathPolicy.GetCrashLogDirectory()));
    }

    [Fact]
    public void ClearAllCrashLogs_Should_Not_Delete_Unrelated_Files()
    {
        CreateFile("crash-20260701-120000-000-aaaaaaaa.log");
        string notes = CreateFile("notes.log");
        string otherCrash = CreateFile("crash-other.txt");
        string random = CreateFile("random.txt");

        CrashLogMaintenanceService service = CreateService();

        CrashLogCleanupResult result = service.ClearAllCrashLogs();

        Assert.True(result.IsSuccessful);
        Assert.Equal(1, result.DeletedCount);
        Assert.True(File.Exists(notes));
        Assert.True(File.Exists(otherCrash));
        Assert.True(File.Exists(random));
    }

    [Fact]
    public void OpenCrashLogDirectory_Should_Create_And_Open_Directory()
    {
        FakeFileSystemLauncher launcher = new();
        CrashLogMaintenanceService service = CreateService(launcher: launcher);

        CrashLogFolderOpenResult result = service.OpenCrashLogDirectory();

        string directory = _pathPolicy.GetCrashLogDirectory();
        Assert.True(result.IsSuccessful);
        Assert.Equal(directory, result.DirectoryPath);
        Assert.Null(result.Error);
        Assert.True(Directory.Exists(directory));
        Assert.Equal(1, launcher.OpenDirectoryCalls);
        Assert.Equal(directory, launcher.LastOpenedDirectoryPath);
    }

    [Fact]
    public void OpenCrashLogDirectory_Should_Return_Failure_When_Launcher_Throws()
    {
        IOException expectedError = new("Unable to open directory.");
        FakeFileSystemLauncher launcher = new()
        {
            ExceptionToThrow = expectedError
        };
        CrashLogMaintenanceService service = CreateService(launcher: launcher);

        CrashLogFolderOpenResult result = service.OpenCrashLogDirectory();

        Assert.False(result.IsSuccessful);
        Assert.Equal(_pathPolicy.GetCrashLogDirectory(), result.DirectoryPath);
        Assert.Same(expectedError, result.Error);
    }

    private CrashLogMaintenanceService CreateService(
        int retentionLimit = ApplicationPreferences.DefaultCrashLogRetentionLimit,
        FakeFileSystemLauncher? launcher = null)
    {
        FakeApplicationPreferencesStore preferencesStore = new();
        preferencesStore.SetCurrent(
            new ApplicationPreferences(
                isPreviewLineWrapEnabledByDefault: ApplicationPreferences.DefaultIsPreviewLineWrapEnabledByDefault,
                previewDisplayCharacterLimit: ApplicationPreferences.DefaultPreviewDisplayCharacterLimit,
                crashLogRetentionLimit: retentionLimit));

        return new CrashLogMaintenanceService(_pathPolicy, preferencesStore, launcher ?? new FakeFileSystemLauncher());
    }

    private string CreateFile(string fileName, DateTime? lastWriteTimeUtc = null)
    {
        string directory = _pathPolicy.GetCrashLogDirectory();
        Directory.CreateDirectory(directory);

        string path = Path.Combine(directory, fileName);
        File.WriteAllText(path, fileName);

        if (lastWriteTimeUtc is not null)
            File.SetLastWriteTimeUtc(path, lastWriteTimeUtc.Value);

        return path;
    }
}