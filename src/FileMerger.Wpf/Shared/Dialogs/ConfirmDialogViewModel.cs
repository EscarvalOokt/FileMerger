namespace FileMerger.Wpf.Shared.Dialogs;

public sealed class ConfirmDialogViewModel
{
    public ConfirmDialogViewModel(string title, string message, string confirmButtonText, string cancelButtonText)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(confirmButtonText);
        ArgumentNullException.ThrowIfNull(cancelButtonText);

        Title = title;
        Message = message;
        ConfirmButtonText = confirmButtonText;
        CancelButtonText = cancelButtonText;
    }

    public string Title { get; }

    public string Message { get; }

    public string ConfirmButtonText { get; }

    public string CancelButtonText { get; }
}