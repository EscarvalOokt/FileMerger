using System.Globalization;
using System.IO;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Workspace.Recent;

public sealed class RecentWorkspaceMenuItemViewModel : ViewModelBase
{
    private const string WorkspaceFileSuffix = ".filemerger.workspace.json";

    public RecentWorkspaceMenuItemViewModel(RecentWorkspaceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        FilePath = entry.FilePath;
        LastUsedAtUtc = entry.LastUsedAtUtc;
        IsMissing = entry.IsMissing;

        DisplayName = BuildDisplayName(FilePath);
        DirectoryPath = Path.GetDirectoryName(FilePath) ?? string.Empty;

        MenuHeader = IsMissing
            ? $"{DisplayName} (missing)"
            : DisplayName;

        ToolTip = BuildToolTip();
    }

    public string FilePath { get; }

    public string DisplayName { get; }

    public string DirectoryPath { get; }

    public DateTime LastUsedAtUtc { get; }

    public bool IsMissing { get; }

    public bool Exists => !IsMissing;

    public string MenuHeader { get; }

    public string ToolTip { get; }

    public string LastUsedText =>
        LastUsedAtUtc
            .ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);

    private static string BuildDisplayName(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        if (string.IsNullOrWhiteSpace(fileName))
            return filePath;

        if (fileName.EndsWith(WorkspaceFileSuffix, StringComparison.OrdinalIgnoreCase))
            return fileName[..^WorkspaceFileSuffix.Length];

        string withoutExtension = Path.GetFileNameWithoutExtension(fileName);

        return string.IsNullOrWhiteSpace(withoutExtension)
            ? fileName
            : withoutExtension;
    }

    private string BuildToolTip()
    {
        List<string> lines =
        [
            FilePath,
            $"Last used: {LastUsedText}"
        ];

        if (IsMissing)
            lines.Add("File is missing.");

        return string.Join(Environment.NewLine, lines);
    }
}