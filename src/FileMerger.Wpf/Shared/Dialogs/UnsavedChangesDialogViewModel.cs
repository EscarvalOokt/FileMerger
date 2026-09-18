using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Shared.Dialogs;

public sealed class UnsavedChangesDialogViewModel : ViewModelBase
{
    private UnsavedChangesDecision _decision = UnsavedChangesDecision.Cancel;

    public UnsavedChangesDialogViewModel(string title, string message)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);

        Title = title;
        Message = message;
    }

    public string Title { get; }

    public string Message { get; }

    public UnsavedChangesDecision Decision
    {
        get => _decision;
        private set => SetProperty(ref _decision, value);
    }

    public void Choose(UnsavedChangesDecision decision)
    {
        Decision = decision;
    }
}