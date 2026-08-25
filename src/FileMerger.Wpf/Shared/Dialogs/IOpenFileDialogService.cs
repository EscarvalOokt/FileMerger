namespace FileMerger.Wpf.Shared.Dialogs;

public interface IOpenFileDialogService
{
    IReadOnlyList<string> SelectFiles(string? initialPath = null, string? filter = null);
    string? SelectFile(string? initialPath = null, string? filter = null);
}