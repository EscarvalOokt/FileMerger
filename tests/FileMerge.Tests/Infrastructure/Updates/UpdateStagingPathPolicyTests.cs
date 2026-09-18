using System.IO;
using FileMerger.Infrastructure.Updates;

namespace FileMerger.Tests.Infrastructure.Updates;

public sealed class UpdateStagingPathPolicyTests
{
    [Fact]
    public void GetUpdatesRootDirectory_Should_Use_FileMerger_LocalAppData_Subdirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        UpdateStagingPathPolicy policy = new(root);

        string result = policy.GetUpdatesRootDirectory();

        Assert.Equal(Path.Combine(root, "FileMerger", "Updates"), result);
    }

    [Fact]
    public void CreateAttemptDirectoryPath_Should_Return_Isolated_Attempts()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        UpdateStagingPathPolicy policy = new(root);

        string first = policy.CreateAttemptDirectoryPath();
        string second = policy.CreateAttemptDirectoryPath();

        Assert.NotEqual(first, second);

        string expectedPrefix = policy.GetUpdatesRootDirectory() + Path.DirectorySeparatorChar;
        Assert.StartsWith(expectedPrefix, first);
        Assert.StartsWith(expectedPrefix, second);
    }

    [Fact]
    public void Artifact_Paths_Should_Stay_Inside_Attempt_Directory()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        UpdateStagingPathPolicy policy = new(root);
        string attempt = policy.CreateAttemptDirectoryPath();

        Assert.Equal(Path.Combine(attempt, "package.zip.partial"), policy.GetPartialArchivePath(attempt));
        Assert.Equal(Path.Combine(attempt, "package.zip"), policy.GetArchivePath(attempt));
        Assert.Equal(Path.Combine(attempt, "payload"), policy.GetPayloadDirectory(attempt));
    }
}