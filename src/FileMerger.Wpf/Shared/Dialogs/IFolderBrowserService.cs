namespace FileMerger.Wpf.Shared.Dialogs;

public interface IFolderBrowserService
{
    IReadOnlyList<string> SelectFolders(string? initialDirectory = null);
}