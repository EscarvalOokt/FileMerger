namespace FileMerger.Wpf.Shared.Dialogs;

public interface ISaveOutputDialogFilterBuilder
{
    string BuildDefaultSaveFileFilter();
    string GetDefaultExtension();
}