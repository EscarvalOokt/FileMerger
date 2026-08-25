using System.ComponentModel;
using System.IO;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Session.ViewModels;
using FileMerger.Wpf.Features.Workspace.State;
using FileMerger.Wpf.Shared.Status;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Workspace.Tabs;

public sealed class WorkspaceTabViewModel : ViewModelBase
{
    private const string UntitledWorkspaceTitle = "Untitled Workspace";
    private const string WorkspaceFileExtension = ".filemerger.workspace.json";
    private const string ReadyStatusMessage = "Ready.";

    private bool _isActive;

    public WorkspaceTabViewModel(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Id = Guid.NewGuid();
        Document = document;

        Document.PropertyChanged += Document_PropertyChanged;
        Document.SessionSettings.PropertyChanged += SessionSettings_PropertyChanged;
        Document.WorkspaceDirtyTracker.PropertyChanged += WorkspaceDirtyTracker_PropertyChanged;
        Document.PreviewDirtyTracker.PropertyChanged += PreviewDirtyTracker_PropertyChanged;
        Document.OperationStatus.PropertyChanged += OperationStatus_PropertyChanged;
    }

    public Guid Id { get; }

    public WorkspaceDocumentViewModel Document { get; }

    public string Title
    {
        get
        {
            string sessionName = Document.SessionSettings.SessionName;
            if (!string.IsNullOrWhiteSpace(sessionName))
                return sessionName.Trim();

            string? fileTitle = BuildTitleFromWorkspaceFilePath(Document.WorkspaceFilePath);
            return string.IsNullOrWhiteSpace(fileTitle)
                ? UntitledWorkspaceTitle
                : fileTitle;
        }
    }

    public bool IsWorkspaceDirty => Document.WorkspaceDirtyTracker.IsWorkspaceDirty;

    public bool IsPreviewDirty => Document.PreviewDirtyTracker.IsPreviewDirty;

    public bool HasOutput => Document.LastOutput is not null;

    public bool IsBusy => Document.OperationStatus.IsBusy;

    public bool CanRequestClose => !IsBusy;

    public bool HasActivityIndicator => IsBusy;

    public string ActivityIndicatorTooltip => OperationTooltip;

    public string OperationTooltip
    {
        get
        {
            if (!IsBusy)
                return string.Empty;

            string message = GetBestOperationMessage();

            return string.IsNullOrWhiteSpace(message)
                ? "Workspace operation is running."
                : $"Workspace operation is running: {message}";
        }
    }

    public string CloseTooltip => IsBusy
        ? "Workspace operation is running. This tab cannot be closed right now."
        : "Close workspace tab (Ctrl+W)";

    public bool HasDirtyIndicator => IsWorkspaceDirty;

    public string DirtyTooltip
    {
        get
        {
            if (IsWorkspaceDirty && IsPreviewDirty)
                return "Workspace has unsaved changes. Preview is outdated.";

            if (IsWorkspaceDirty)
                return "Workspace has unsaved changes.";

            if (IsPreviewDirty)
                return Document.PreviewDirtyTracker.PreviewDirtyTooltip;

            return "Workspace is saved.";
        }
    }

    public bool IsActive
    {
        get => _isActive;
        internal set => SetProperty(ref _isActive, value);
    }

    private string GetBestOperationMessage()
    {
        OperationStatusViewModel status = Document.OperationStatus;

        if (status.IsProgressVisible && IsUsefulOperationMessage(status.ProgressMessage))
            return status.ProgressMessage.Trim();

        if (IsUsefulOperationMessage(status.StatusMessage))
            return status.StatusMessage.Trim();

        return string.Empty;
    }

    private static bool IsUsefulOperationMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return !string.Equals(
            message.Trim(),
            ReadyStatusMessage,
            StringComparison.Ordinal);
    }

    private static string? BuildTitleFromWorkspaceFilePath(string? workspaceFilePath)
    {
        if (string.IsNullOrWhiteSpace(workspaceFilePath))
            return null;

        string fileName = Path.GetFileName(workspaceFilePath.Trim());
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        if (fileName.EndsWith(WorkspaceFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            string title = fileName[..^WorkspaceFileExtension.Length].Trim();
            return string.IsNullOrWhiteSpace(title)
                ? null
                : title;
        }

        string fallbackTitle = Path.GetFileNameWithoutExtension(fileName).Trim();
        return string.IsNullOrWhiteSpace(fallbackTitle)
            ? null
            : fallbackTitle;
    }

    private void SessionSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SessionSettingsViewModel.SessionName))
            OnPropertyChanged(nameof(Title));
    }

    private void Document_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.LastOutput))
            OnPropertyChanged(nameof(HasOutput));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.WorkspaceFilePath))
            OnPropertyChanged(nameof(Title));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.IsWorkspaceDirty))
            RaiseWorkspaceDirtyPropertiesChanged();
    }

    private void WorkspaceDirtyTracker_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkspaceDirtyStateTracker.IsWorkspaceDirty))
            RaiseWorkspaceDirtyPropertiesChanged();
    }

    private void PreviewDirtyTracker_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PreviewDirtyStateTracker.IsPreviewDirty) ||
            e.PropertyName == nameof(PreviewDirtyStateTracker.PreviewDirtyTooltip))
        {
            OnPropertyChanged(nameof(IsPreviewDirty));
            OnPropertyChanged(nameof(DirtyTooltip));
        }
    }

    private void OperationStatus_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OperationStatusViewModel.IsBusy))
        {
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(CanRequestClose));
            OnPropertyChanged(nameof(HasActivityIndicator));
            OnPropertyChanged(nameof(CloseTooltip));
            RaiseOperationTooltipPropertiesChanged();
            return;
        }

        if (e.PropertyName == nameof(OperationStatusViewModel.StatusMessage) ||
            e.PropertyName == nameof(OperationStatusViewModel.IsProgressVisible) ||
            e.PropertyName == nameof(OperationStatusViewModel.ProgressMessage))
        {
            RaiseOperationTooltipPropertiesChanged();
        }
    }

    private void RaiseOperationTooltipPropertiesChanged()
    {
        OnPropertyChanged(nameof(OperationTooltip));
        OnPropertyChanged(nameof(ActivityIndicatorTooltip));
    }

    private void RaiseWorkspaceDirtyPropertiesChanged()
    {
        OnPropertyChanged(nameof(IsWorkspaceDirty));
        OnPropertyChanged(nameof(HasDirtyIndicator));
        OnPropertyChanged(nameof(DirtyTooltip));
    }
}