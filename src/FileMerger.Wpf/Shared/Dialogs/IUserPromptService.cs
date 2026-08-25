namespace FileMerger.Wpf.Shared.Dialogs;

public interface IUserPromptService
{
    UnsavedChangesDecision ConfirmUnsavedChanges(
        string title,
        string message);

    bool Confirm(
        string title,
        string message);
}