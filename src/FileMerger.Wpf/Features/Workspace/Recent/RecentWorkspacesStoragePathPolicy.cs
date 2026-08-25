using System.IO;

namespace FileMerger.Wpf.Features.Workspace.Recent;

public sealed class RecentWorkspacesStoragePathPolicy(string? localApplicationDataRoot = null)
{
    private const string ApplicationDirectoryName = "FileMerger";
    private const string StorageFileName = "recent-workspaces.json";

    private readonly string _localApplicationDataRoot = string.IsNullOrWhiteSpace(localApplicationDataRoot)
        ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        : localApplicationDataRoot;

    public string GetStorageFilePath()
    {
        return Path.Combine(
            _localApplicationDataRoot,
            ApplicationDirectoryName,
            StorageFileName);
    }
}