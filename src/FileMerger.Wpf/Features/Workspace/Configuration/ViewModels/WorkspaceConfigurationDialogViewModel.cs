using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Workspace.Configuration.ViewModels;

public sealed class WorkspaceConfigurationDialogViewModel : ViewModelBase
{
    private readonly WorkspaceDocumentViewModel _targetDocument;
    private readonly IWorkspaceDocumentDirtyStateService _dirtyStateService;
    private readonly IWorkspaceDocumentOutputService _outputService;
    private readonly string? _initialProfileEntryId;
    private readonly string _initialProfileDisplayName;
    private readonly PreviewProfileStateSnapshot _initialProfileState;

    public WorkspaceConfigurationDialogViewModel(
        WorkspaceDocumentViewModel targetDocument,
        IWorkspaceDocumentFactory documentFactory,
        IWorkspaceDocumentDirtyStateService dirtyStateService,
        IWorkspaceDocumentOutputService outputService)
    {
        ArgumentNullException.ThrowIfNull(targetDocument);
        ArgumentNullException.ThrowIfNull(documentFactory);
        ArgumentNullException.ThrowIfNull(dirtyStateService);
        ArgumentNullException.ThrowIfNull(outputService);

        _targetDocument = targetDocument;
        _dirtyStateService = dirtyStateService;
        _outputService = outputService;

        WorkspaceConfigurationSnapshot configuration =
            WorkspaceConfigurationMapper.Capture(targetDocument);

        _initialProfileEntryId = configuration.ProfileEntryId;
        _initialProfileDisplayName = configuration.ProfileDisplayName;
        _initialProfileState = targetDocument.ProfileEditor.BuildPreviewProfileSnapshot();

        WorkingDocument = documentFactory.CreateDefaultDocument();
        WorkspaceConfigurationMapper.Apply(WorkingDocument, configuration);

        BrowseOutputCommand = new RelayCommand(BrowseOutput);
        OkCommand = new RelayCommand(ApplyAndClose);
        CancelCommand = new RelayCommand(Cancel);
    }

    public event EventHandler<bool?>? RequestClose;

    public WorkspaceDocumentViewModel WorkingDocument { get; }

    public RelayCommand BrowseOutputCommand { get; }

    public RelayCommand OkCommand { get; }

    public RelayCommand CancelCommand { get; }

    public string ProfileSourceText => ProfileSourceTextFormatter.Format(
        WorkingDocument.CurrentProfileEntryId,
        WorkingDocument.ProfileOriginEntryId,
        WorkingDocument.ProfileOriginDisplayName);

    private void BrowseOutput()
    {
        _outputService.BrowseOutputPath(WorkingDocument);
    }

    private void ApplyAndClose()
    {
        WorkspaceConfigurationSnapshot configuration =
            WorkspaceConfigurationMapper.Capture(WorkingDocument);

        if (_initialProfileEntryId is not null && HasLinkedProfileDiverged(configuration.ProfileDisplayName))
            configuration = configuration with { ProfileEntryId = null };

        WorkspaceConfigurationMapper.Apply(_targetDocument, configuration);
        _dirtyStateService.RefreshWorkspaceDirtyState(_targetDocument);
        _dirtyStateService.RefreshPreviewDirtyState(_targetDocument);

        RequestClose?.Invoke(this, true);
    }

    private bool HasLinkedProfileDiverged(string profileDisplayName)
    {
        return !string.Equals(
                   profileDisplayName,
                   _initialProfileDisplayName,
                   StringComparison.Ordinal) ||
               !Equals(
                   WorkingDocument.ProfileEditor.BuildPreviewProfileSnapshot(),
                   _initialProfileState);
    }

    private void Cancel()
    {
        RequestClose?.Invoke(this, false);
    }
}