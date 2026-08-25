using System.IO;

namespace FileMerger.Wpf.Features.Workspace.Recent;

public sealed record RecentWorkspaceEntry(
    string FilePath,
    DateTime LastUsedAtUtc,
    bool Exists)
{
    public bool IsMissing => !Exists;

    public string DisplayName => Path.GetFileName(FilePath);
}