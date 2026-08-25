using System.IO;
using FileMerger.Wpf.Diagnostics;

namespace FileMerger.Tests.Wpf.Diagnostics;

public sealed class CrashLogPathPolicyTests : IDisposable
{
    private readonly string _tempRoot;

    public CrashLogPathPolicyTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FileMerger.Tests",
            nameof(CrashLogPathPolicyTests),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void GetCrashLogDirectory_Should_Use_FileMerger_CrashLogs_Subdirectory()
    {
        CrashLogPathPolicy policy = new(_tempRoot);

        string result = policy.GetCrashLogDirectory();

        Assert.Equal(
            Path.Combine(_tempRoot, "FileMerger", "CrashLogs"),
            result);
    }

    [Fact]
    public void CreateCrashLogPath_Should_Use_Timestamp_And_Log_Extension()
    {
        CrashLogPathPolicy policy = new(_tempRoot);

        DateTime occurredAtUtc = new(2026, 6, 23, 18, 42, 11, 123, DateTimeKind.Utc);

        string result = policy.CreateCrashLogPath(occurredAtUtc);

        string expectedPrefix = Path.Combine(
            _tempRoot,
            "FileMerger",
            "CrashLogs",
            "crash-20260623-184211-123-");

        Assert.StartsWith(expectedPrefix, result);
        Assert.EndsWith(".log", result);
    }

    [Fact]
    public void CreateCrashLogPath_Should_Create_Unique_File_Names()
    {
        CrashLogPathPolicy policy = new(_tempRoot);

        DateTime occurredAtUtc = new(2026, 6, 23, 18, 42, 11, 123, DateTimeKind.Utc);

        string first = policy.CreateCrashLogPath(occurredAtUtc);
        string second = policy.CreateCrashLogPath(occurredAtUtc);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void CreateCrashLogPath_Should_Not_Contain_Invalid_FileName_Characters()
    {
        CrashLogPathPolicy policy = new(_tempRoot);

        DateTime occurredAtUtc = new(2026, 6, 23, 18, 42, 11, 123, DateTimeKind.Utc);

        string result = policy.CreateCrashLogPath(occurredAtUtc);
        string fileName = Path.GetFileName(result);

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            Assert.False(
                fileName.Any(x => x == invalidChar),
                $"File name contains invalid character U+{(int)invalidChar:X4}: {fileName}");
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }
}