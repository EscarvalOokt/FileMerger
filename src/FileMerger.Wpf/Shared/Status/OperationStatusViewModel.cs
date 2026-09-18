using FileMerger.Application.UseCases.BuildPreview;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Shared.Status;

public sealed class OperationStatusViewModel : ViewModelBase
{
    private readonly Action _cancelAction;

    private bool _isBusy;
    private bool _isCancelable;
    private bool _isProgressIndeterminate = true;
    private bool _isProgressVisible;
    private string _progressMessage = string.Empty;
    private double _progressValue;
    private string _statusMessage = "Ready.";
    private StatusSeverity _statusSeverity = StatusSeverity.Info;

    public OperationStatusViewModel(Action cancelAction)
    {
        ArgumentNullException.ThrowIfNull(cancelAction);

        _cancelAction = cancelAction;

        CancelCommand = new RelayCommand(execute: () => _cancelAction(), canExecute: () => IsCancelable);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public StatusSeverity StatusSeverity
    {
        get => _statusSeverity;
        set => SetProperty(ref _statusSeverity, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public bool IsCancelable
    {
        get => _isCancelable;
        set
        {
            if (SetProperty(ref _isCancelable, value))
                CancelCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsProgressVisible
    {
        get => _isProgressVisible;
        set => SetProperty(ref _isProgressVisible, value);
    }

    public bool IsProgressIndeterminate
    {
        get => _isProgressIndeterminate;
        set => SetProperty(ref _isProgressIndeterminate, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public string ProgressMessage
    {
        get => _progressMessage;
        set => SetProperty(ref _progressMessage, value);
    }

    public RelayCommand CancelCommand { get; }

    public void SetStatus(string message, StatusSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(message);

        StatusMessage = message;
        StatusSeverity = severity;
    }

    public void ShowIndeterminateProgress(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        IsProgressVisible = true;
        IsProgressIndeterminate = true;
        ProgressValue = 0;
        ProgressMessage = message;
    }

    public void ShowDeterminateProgress(string message, double progressValue)
    {
        ArgumentNullException.ThrowIfNull(message);

        IsProgressVisible = true;
        IsProgressIndeterminate = false;
        ProgressValue = Math.Clamp(progressValue, 0d, 100d);
        ProgressMessage = message;
    }

    public void HideProgress()
    {
        IsProgressVisible = false;
        IsProgressIndeterminate = true;
        ProgressValue = 0;
        ProgressMessage = string.Empty;
    }

    public void Apply(BuildMergePreviewProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (progress.Total <= 0)
        {
            ShowIndeterminateProgress(progress.Message);
            return;
        }

        double value = progress.Current * 100d / progress.Total;
        ShowDeterminateProgress(progress.Message, value);
    }
}