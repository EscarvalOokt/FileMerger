namespace FileMerger.Wpf.Shared.Dialogs;

public sealed class ConfirmDialogViewModel
{
    public ConfirmDialogViewModel(
        string title,
        string message)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);

        Title = title;
        Message = message;
    }

    public string Title { get; }

    public string Message { get; }
}