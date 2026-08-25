using FileMerger.Wpf.Shared.Integration;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeFileSystemLauncher : IFileSystemLauncher
{
    public HashSet<string> ExistingDirectories { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string? LastOpenedDirectoryPath { get; private set; }

    public int OpenDirectoryCalls { get; private set; }

    public Exception? ExceptionToThrow { get; set; }

    public bool CanOpenDirectory(string? directoryPath)
    {
        return !string.IsNullOrWhiteSpace(directoryPath) &&
               ExistingDirectories.Contains(directoryPath);
    }

    public void OpenDirectory(string directoryPath)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        LastOpenedDirectoryPath = directoryPath;
        OpenDirectoryCalls++;
    }
}