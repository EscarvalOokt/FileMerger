using System.ComponentModel;
using FileMerger.Domain.Entities;
using FileMerger.Wpf.Features.Files.ViewModels;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Preview.ViewModels;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Session.ViewModels;
using FileMerger.Wpf.Features.Sources.ViewModels;
using FileMerger.Wpf.Features.Validation.ViewModels;
using FileMerger.Wpf.Features.Workspace.State;
using FileMerger.Wpf.Shared.Status;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentViewModel : ViewModelBase
{
    private CancellationTokenSource? _buildPreviewCancellationTokenSource;
    private string? _currentProfileEntryId;
    private bool _isPreviewLineWrapEnabled;
    private bool _isPreviewSummaryExpanded = true;
    private MergeOutput? _lastOutput;
    private string _previewCharacterCountText = FormatPreviewCharacterCountText(0);
    private string _previewContent = string.Empty;
    private string _previewNotice = string.Empty;
    private PreviewGenerationSummaryViewModel _previewSummary = PreviewGenerationSummaryViewModel.Empty;
    private string? _profileOriginDisplayName;
    private string? _profileOriginEntryId;
    private string? _workspaceFilePath;

    public WorkspaceDocumentViewModel(
        SessionSettingsViewModel sessionSettings,
        ProfileEditorViewModel profileEditor,
        SourcesPaneViewModel sourcesPane,
        FilesPaneViewModel filesPane,
        ValidationPaneViewModel validationPane,
        PreviewDirtyStateTracker previewDirtyTracker,
        WorkspaceDirtyStateTracker workspaceDirtyTracker,
        IAppliedPreviewFileStateStore appliedPreviewFileStateStore)
    {
        ArgumentNullException.ThrowIfNull(sessionSettings);
        ArgumentNullException.ThrowIfNull(profileEditor);
        ArgumentNullException.ThrowIfNull(sourcesPane);
        ArgumentNullException.ThrowIfNull(filesPane);
        ArgumentNullException.ThrowIfNull(validationPane);
        ArgumentNullException.ThrowIfNull(previewDirtyTracker);
        ArgumentNullException.ThrowIfNull(workspaceDirtyTracker);
        ArgumentNullException.ThrowIfNull(appliedPreviewFileStateStore);

        SessionSettings = sessionSettings;
        ProfileEditor = profileEditor;
        SourcesPane = sourcesPane;
        FilesPane = filesPane;
        ValidationPane = validationPane;
        PreviewDirtyTracker = previewDirtyTracker;
        WorkspaceDirtyTracker = workspaceDirtyTracker;
        AppliedPreviewFileStateStore = appliedPreviewFileStateStore;

        WorkspaceDirtyTracker.PropertyChanged += WorkspaceDirtyTracker_PropertyChanged;

        OperationStatus = new OperationStatusViewModel(CancelBuildPreview);

        SourcesPane.PropertyChanged += EmptyStateSource_PropertyChanged;
        FilesPane.PropertyChanged += EmptyStateSource_PropertyChanged;
        ValidationPane.PropertyChanged += EmptyStateSource_PropertyChanged;
        PreviewDirtyTracker.PropertyChanged += EmptyStateSource_PropertyChanged;
        OperationStatus.PropertyChanged += EmptyStateSource_PropertyChanged;
    }

    public SessionSettingsViewModel SessionSettings { get; }
    public ProfileEditorViewModel ProfileEditor { get; }
    public SourcesPaneViewModel SourcesPane { get; }
    public FilesPaneViewModel FilesPane { get; }
    public ValidationPaneViewModel ValidationPane { get; }
    public PreviewDirtyStateTracker PreviewDirtyTracker { get; }
    public WorkspaceDirtyStateTracker WorkspaceDirtyTracker { get; }
    public IAppliedPreviewFileStateStore AppliedPreviewFileStateStore { get; }
    public OperationStatusViewModel OperationStatus { get; }

    public bool IsWorkspaceDirty => WorkspaceDirtyTracker.IsWorkspaceDirty;

    public bool HasSources => SourcesPane.HasSources;
    public bool HasNoSources => !HasSources;

    public bool ShowDiscoveredFilesEmptyState => FilesPane.HasNoFiles;
    public string DiscoveredFilesEmptyTitle => BuildDiscoveredFilesEmptyTitle();
    public string DiscoveredFilesEmptyDescription => BuildDiscoveredFilesEmptyDescription();

    public bool HasPreviewContent => !string.IsNullOrWhiteSpace(PreviewContent);
    public bool HasSuccessfulPreviewBuild => LastOutput is not null;

    public bool IsEmptyWorkspace =>
        !SourcesPane.HasSources && FilesPane.HasNoFiles && !HasPreviewContent && LastOutput is null;

    public bool ShowPreviewEmptyState => !HasPreviewContent;
    public string PreviewEmptyTitle => BuildPreviewEmptyTitle();
    public string PreviewEmptyDescription => BuildPreviewEmptyDescription();
    public bool ShowPreviewEmptyBuildAction => !OperationStatus.IsBusy;

    public MergeOutput? LastOutput
    {
        get => _lastOutput;
        private set
        {
            if (SetProperty(ref _lastOutput, value))
                RefreshEmptyStateProperties();
        }
    }

    public string PreviewContent
    {
        get => _previewContent;
        set
        {
            if (SetProperty(ref _previewContent, value))
                RefreshEmptyStateProperties();
        }
    }

    public string PreviewNotice
    {
        get => _previewNotice;
        set
        {
            if (!SetProperty(ref _previewNotice, value))
                return;

            OnPropertyChanged(nameof(HasPreviewNotice));
            RefreshEmptyStateProperties();
        }
    }

    public string PreviewCharacterCountText
    {
        get => _previewCharacterCountText;
        private set => SetProperty(ref _previewCharacterCountText, value);
    }

    public bool HasPreviewNotice => !string.IsNullOrWhiteSpace(PreviewNotice);

    public PreviewGenerationSummaryViewModel PreviewSummary
    {
        get => _previewSummary;
        private set
        {
            if (!SetProperty(ref _previewSummary, value))
                return;

            OnPropertyChanged(nameof(HasPreviewSummary));
        }
    }

    public bool HasPreviewSummary => PreviewSummary.HasSummary;

    public bool IsPreviewSummaryExpanded
    {
        get => _isPreviewSummaryExpanded;
        set
        {
            if (!SetProperty(ref _isPreviewSummaryExpanded, value))
                return;

            OnPropertyChanged(nameof(IsPreviewSummaryCollapsed));
        }
    }

    public bool IsPreviewSummaryCollapsed => !IsPreviewSummaryExpanded;

    public bool IsPreviewLineWrapEnabled
    {
        get => _isPreviewLineWrapEnabled;
        set => SetProperty(ref _isPreviewLineWrapEnabled, value);
    }

    public string CurrentProfileName => ProfileEditor.WorkingProfileName;

    public string? CurrentProfileEntryId
    {
        get => _currentProfileEntryId;
        set
        {
            string? normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

            SetProperty(ref _currentProfileEntryId, normalized);
        }
    }

    public string? ProfileOriginEntryId
    {
        get => _profileOriginEntryId;
        set
        {
            string? normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

            SetProperty(ref _profileOriginEntryId, normalized);
        }
    }

    public string? ProfileOriginDisplayName
    {
        get => _profileOriginDisplayName;
        set
        {
            string? normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

            SetProperty(ref _profileOriginDisplayName, normalized);
        }
    }

    public string? WorkspaceFilePath
    {
        get => _workspaceFilePath;
        set
        {
            string? normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

            SetProperty(ref _workspaceFilePath, normalized);
        }
    }

    public CancellationToken BeginPreviewBuild()
    {
        _buildPreviewCancellationTokenSource?.Dispose();
        _buildPreviewCancellationTokenSource = new CancellationTokenSource();
        return _buildPreviewCancellationTokenSource.Token;
    }

    public void CancelBuildPreview()
    {
        _buildPreviewCancellationTokenSource?.Cancel();
    }

    public void CompletePreviewBuild()
    {
        _buildPreviewCancellationTokenSource?.Dispose();
        _buildPreviewCancellationTokenSource = null;
    }

    public void SetLastOutput(MergeOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);
        LastOutput = output;
    }

    public void ClearLastOutput()
    {
        LastOutput = null;
    }

    public void SetPreviewCharacterCount(int totalCharacters)
    {
        PreviewCharacterCountText = FormatPreviewCharacterCountText(totalCharacters);
    }

    public void ResetPreviewCharacterCount()
    {
        SetPreviewCharacterCount(0);
    }

    public void SetPreviewSummary(PreviewGenerationSummaryViewModel summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        PreviewSummary = summary;
    }

    public void ResetPreviewSummary()
    {
        PreviewSummary = PreviewGenerationSummaryViewModel.Empty;
    }

    public void TogglePreviewSummary()
    {
        IsPreviewSummaryExpanded = !IsPreviewSummaryExpanded;
    }

    public void ResetRuntimeState()
    {
        ClearLastOutput();
        AppliedPreviewFileStateStore.Clear();

        PreviewContent = string.Empty;
        PreviewNotice = string.Empty;
        ResetPreviewCharacterCount();
        ResetPreviewSummary();

        ValidationPane.Clear();
        PreviewDirtyTracker.Reset();
    }

    private void WorkspaceDirtyTracker_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkspaceDirtyStateTracker.IsWorkspaceDirty))
            OnPropertyChanged(nameof(IsWorkspaceDirty));
    }

    private void EmptyStateSource_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        bool affectsEmptyState = sender == SourcesPane && e.PropertyName == nameof(SourcesPaneViewModel.HasSources) ||
                                 sender == FilesPane && e.PropertyName == nameof(FilesPaneViewModel.HasNoFiles) ||
                                 sender == ValidationPane &&
                                 e.PropertyName == nameof(ValidationPaneViewModel.HasErrors) ||
                                 sender == PreviewDirtyTracker &&
                                 e.PropertyName == nameof(PreviewDirtyStateTracker.HasAppliedPreview) ||
                                 sender == OperationStatus && e.PropertyName == nameof(OperationStatusViewModel.IsBusy);

        if (affectsEmptyState)
            RefreshEmptyStateProperties();
    }

    private string BuildDiscoveredFilesEmptyTitle()
    {
        if (ValidationPane.HasErrors && FilesPane.HasNoFiles)
            return "Preview could not discover files";

        if (!HasSources || !PreviewDirtyTracker.HasAppliedPreview)
            return "No files discovered yet";

        return "No files were discovered";
    }

    private string BuildDiscoveredFilesEmptyDescription()
    {
        if (ValidationPane.HasErrors && FilesPane.HasNoFiles)
            return "Fix validation errors, then build preview again.";

        if (!HasSources)
            return "Add a folder or individual file source, then build preview to discover files.";

        if (!PreviewDirtyTracker.HasAppliedPreview)
            return "Build preview to discover files from the configured sources.";

        return
            "Check source paths, enabled file types, profile filters, source exclusions, and unsupported text fallback settings.";
    }

    private string BuildPreviewEmptyTitle()
    {
        if (!HasSources)
            return "Preview will appear here";

        if (ValidationPane.HasErrors && LastOutput is null)
            return "Preview build is blocked by validation errors";

        if (LastOutput is not null && !HasPreviewContent)
            return "Preview is empty";

        return "Preview will appear here after build";
    }

    private string BuildPreviewEmptyDescription()
    {
        if (!HasSources)
            return "Add a folder or file source, then build preview.";

        if (ValidationPane.HasErrors && LastOutput is null)
            return "Fix validation errors, then build preview again.";

        if (LastOutput is not null && !HasPreviewContent)
        {
            return "Preview was built successfully, but no file content was included in the output. " +
                   "Check discovered files, profile filters, and manual inclusion overrides.";
        }

        if (!PreviewDirtyTracker.HasAppliedPreview)
            return "Build preview to review the merged output before saving.";

        return "Build preview to generate merged output.";
    }

    private void RefreshEmptyStateProperties()
    {
        OnPropertyChanged(nameof(HasSources));
        OnPropertyChanged(nameof(HasNoSources));

        OnPropertyChanged(nameof(ShowDiscoveredFilesEmptyState));
        OnPropertyChanged(nameof(DiscoveredFilesEmptyTitle));
        OnPropertyChanged(nameof(DiscoveredFilesEmptyDescription));

        OnPropertyChanged(nameof(HasPreviewContent));
        OnPropertyChanged(nameof(HasSuccessfulPreviewBuild));
        OnPropertyChanged(nameof(IsEmptyWorkspace));
        OnPropertyChanged(nameof(ShowPreviewEmptyState));
        OnPropertyChanged(nameof(PreviewEmptyTitle));
        OnPropertyChanged(nameof(PreviewEmptyDescription));
        OnPropertyChanged(nameof(ShowPreviewEmptyBuildAction));
    }

    private static string FormatPreviewCharacterCountText(int totalCharacters)
    {
        return $"Characters: {totalCharacters:N0}";
    }
}