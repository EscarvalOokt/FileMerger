using System.Globalization;
using System.IO;

namespace FileMerger.Wpf.Diagnostics;

public sealed class CrashLogPathPolicy(string? localApplicationDataRoot = null)
{
    private const string ApplicationDirectoryName = "FileMerger";
    private const string CrashLogsDirectoryName = "CrashLogs";

    private readonly string _localApplicationDataRoot = string.IsNullOrWhiteSpace(localApplicationDataRoot)
        ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        : localApplicationDataRoot;

    public string GetCrashLogDirectory()
    {
        return Path.Combine(
            _localApplicationDataRoot,
            ApplicationDirectoryName,
            CrashLogsDirectoryName);
    }

    public string CreateCrashLogPath(DateTime occurredAtUtc)
    {
        string timestamp = occurredAtUtc
            .ToUniversalTime()
            .ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);

        string suffix = Guid.NewGuid().ToString("N")[..8];

        string fileName = $"crash-{timestamp}-{suffix}.log";

        return Path.Combine(GetCrashLogDirectory(), fileName);
    }
}