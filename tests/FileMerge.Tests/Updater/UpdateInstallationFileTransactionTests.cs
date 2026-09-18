using System.IO;
using FileMerger.Updater;

namespace FileMerger.Tests.Updater;

public sealed class UpdateInstallationFileTransactionTests
{
    [Fact]
    public async Task Transaction_Should_Backup_Replace_And_Restore_Installation()
    {
        using TemporaryDirectory temp = new();
        string installation = Path.Combine(temp.Path, "installation");
        string payload = Path.Combine(temp.Path, "payload");
        string rollback = Path.Combine(temp.Path, "rollback");
        Directory.CreateDirectory(installation);
        Directory.CreateDirectory(payload);
        await File.WriteAllTextAsync(Path.Combine(installation, "old.txt"), "old");
        Directory.CreateDirectory(Path.Combine(installation, "nested"));
        await File.WriteAllTextAsync(Path.Combine(installation, "nested", "old-nested.txt"), "old-nested");
        await File.WriteAllTextAsync(Path.Combine(payload, "new.txt"), "new");

        UpdateInstallationFileTransaction transaction = new();
        await transaction.CreateBackupAsync(installation, rollback);
        await transaction.ReplaceInstallationAsync(installation, payload);

        Assert.False(File.Exists(Path.Combine(installation, "old.txt")));
        Assert.Equal("new", await File.ReadAllTextAsync(Path.Combine(installation, "new.txt")));
        Assert.Equal("old", await File.ReadAllTextAsync(Path.Combine(rollback, "old.txt")));

        await transaction.RestoreBackupAsync(installation, rollback);

        Assert.False(File.Exists(Path.Combine(installation, "new.txt")));
        Assert.Equal("old", await File.ReadAllTextAsync(Path.Combine(installation, "old.txt")));
        Assert.Equal("old-nested", await File.ReadAllTextAsync(Path.Combine(installation, "nested", "old-nested.txt")));
    }

    [Fact]
    public async Task Transaction_Should_Not_Touch_External_Data_Directory()
    {
        using TemporaryDirectory temp = new();
        string installation = Path.Combine(temp.Path, "installation");
        string payload = Path.Combine(temp.Path, "payload");
        string rollback = Path.Combine(temp.Path, "rollback");
        string userData = Path.Combine(temp.Path, "user-data");
        Directory.CreateDirectory(installation);
        Directory.CreateDirectory(payload);
        Directory.CreateDirectory(userData);
        await File.WriteAllTextAsync(Path.Combine(installation, "old.txt"), "old");
        await File.WriteAllTextAsync(Path.Combine(payload, "new.txt"), "new");
        string userDataFile = Path.Combine(userData, "profile.json");
        await File.WriteAllTextAsync(userDataFile, "user-data");

        UpdateInstallationFileTransaction transaction = new();
        await transaction.CreateBackupAsync(installation, rollback);
        await transaction.ReplaceInstallationAsync(installation, payload);

        Assert.Equal("user-data", await File.ReadAllTextAsync(userDataFile));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"FileMergerTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Test cleanup is best-effort.
            }
            catch (UnauthorizedAccessException)
            {
                // Test cleanup is best-effort.
            }
        }
    }
}