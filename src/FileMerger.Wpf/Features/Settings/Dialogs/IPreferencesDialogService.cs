namespace FileMerger.Wpf.Features.Settings.Dialogs;

public interface IPreferencesDialogService
{
    Task<bool> ShowDialogAsync();
}