namespace FileMerger.Wpf.Shared.Dialogs;

public interface ISaveFileDialogService
{
    string? SelectSaveFilePath(string? initialPath = null, string? filter = null);
}