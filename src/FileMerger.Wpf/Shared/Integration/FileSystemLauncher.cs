using System.Diagnostics;
using System.IO;

namespace FileMerger.Wpf.Shared.Integration;

public sealed class FileSystemLauncher : IFileSystemLauncher
{
    public bool CanOpenDirectory(string? directoryPath)
    {
        return !string.IsNullOrWhiteSpace(directoryPath) &&
               Directory.Exists(directoryPath);
    }

    public void OpenDirectory(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory does not exist: {directoryPath}");

        Process.Start(new ProcessStartInfo
        {
            FileName = directoryPath,
            UseShellExecute = true
        });
    }
}