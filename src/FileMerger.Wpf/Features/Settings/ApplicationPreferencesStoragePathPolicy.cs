using System.IO;

namespace FileMerger.Wpf.Features.Settings;

public sealed class ApplicationPreferencesStoragePathPolicy(string? localApplicationDataRoot = null)
{
    private const string ApplicationDirectoryName = "FileMerger";
    private const string StorageFileName = "application-preferences.json";

    private readonly string _localApplicationDataRoot = string.IsNullOrWhiteSpace(localApplicationDataRoot)
        ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        : localApplicationDataRoot;

    public string GetStorageFilePath()
    {
        return Path.Combine(_localApplicationDataRoot, ApplicationDirectoryName, StorageFileName);
    }
}