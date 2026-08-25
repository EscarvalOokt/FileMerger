namespace FileMerger.Wpf.Shared.Integration;

public interface IFileSystemLauncher
{
    bool CanOpenDirectory(string? directoryPath);

    void OpenDirectory(string directoryPath);
}