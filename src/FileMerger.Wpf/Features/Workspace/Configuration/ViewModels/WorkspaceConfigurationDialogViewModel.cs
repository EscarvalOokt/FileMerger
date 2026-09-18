using System.ComponentModel;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Workspace.Configuration.Dialogs;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Workspace.Configuration.ViewModels;

public sealed class WorkspaceConfigurationDialogViewModel : ViewModelBase, ICurrentSessionProfileHost
{
    private readonly IWorkspaceDocumentDirtyStateService _dirtyStateService;
    private readonly IWorkspaceDocumentOutputService _outputService;
    private readonly IProfileManagerWindowService _profileManagerWindowService;
    private readonly WorkspaceDocumentViewModel _targetDocument;
    private readonly IWorkspaceLocalProfileDialogService _workspaceLocalProfileDialogService;

    private string? _linkedProfileDisplayName;
    private string? _linkedProfileEntryId;
    private PreviewProfileStateSnapshot? _linkedProfileState;

    public WorkspaceConfigurationDialogViewModel(
        WorkspaceDocumentViewModel targetDocument,
        IWorkspaceDocumentFactory documentFactory,
        IWorkspaceDocumentDirtyStateService dirtyStateService,
        IWorkspaceDocumentOutputService outputService,
        IWorkspaceLocalProfileDialogService workspaceLocalProfileDialogService,
        IProfileManagerWindowService profileManagerWindowService)
    {
        ArgumentNullException.ThrowIfNull(targetDocument);
        ArgumentNullException.ThrowIfNull(documentFactory);
        ArgumentNullException.ThrowIfNull(dirtyStateService);
        ArgumentNullException.ThrowIfNull(outputService);
        ArgumentNullException.ThrowIfNull(workspaceLocalProfileDialogService);
        ArgumentNullException.ThrowIfNull(profileManagerWindowService);

        _targetDocument = targetDocument;
        _dirtyStateService = dirtyStateService;
        _outputService = outputService;
        _workspaceLocalProfileDialogService = workspaceLocalProfileDialogService;
        _profileManagerWindowService = profileManagerWindowService;

        WorkspaceConfigurationSnapshot configuration = WorkspaceConfigurationMapper.Capture(targetDocument);

        WorkingDocument = documentFactory.CreateDefaultDocument();
        WorkspaceConfigurationMapper.Apply(WorkingDocument, configuration);

        CurrentProfileCard = new CurrentProfileCardViewModel();

        if (WorkingDocument.CurrentProfileEntryId is not null)
            SetLinkedProfileBaselineFromWorkingDocument();

        RefreshCurrentProfileCard();

        BrowseOutputCommand = new RelayCommand(BrowseOutput);
        EditLocalProfileCommand = new AsyncRelayCommand(EditLocalProfileAsync);
        ChooseProfileCommand = new AsyncRelayCommand(ChooseProfileAsync);
        OkCommand = new RelayCommand(ApplyAndClose, CanApplyConfiguration);
        CancelCommand = new RelayCommand(Cancel);

        WorkingDocument.ProfileEditor.PropertyChanged += ProfileEditor_PropertyChanged;
    }

    public WorkspaceDocumentViewModel WorkingDocument { get; }

    public CurrentProfileCardViewModel CurrentProfileCard { get; }

    public RelayCommand BrowseOutputCommand { get; }

    public AsyncRelayCommand EditLocalProfileCommand { get; }

    public AsyncRelayCommand ChooseProfileCommand { get; }

    public RelayCommand OkCommand { get; }

    public RelayCommand CancelCommand { get; }

    public string CurrentProfileName => WorkingDocument.CurrentProfileName;

    public string? CurrentProfileEntryId => WorkingDocument.CurrentProfileEntryId;

    public WorkspaceProfileDto CaptureCurrentProfile()
    {
        return WorkingDocument.ProfileEditor.CaptureProfile();
    }

    public void ApplyProfileToCurrentSession(string profileName, WorkspaceProfileDto profile, string? profileEntryId)
    {
        ArgumentNullException.ThrowIfNull(profile);

        WorkingDocument.ProfileEditor.WorkingProfileName = NormalizeProfileName(profileName);
        WorkingDocument.ProfileEditor.ApplyProfile(profile);
        WorkingDocument.CurrentProfileEntryId = profileEntryId;

        if (WorkingDocument.CurrentProfileEntryId is not null)
        {
            WorkingDocument.ProfileOriginEntryId = WorkingDocument.CurrentProfileEntryId;
            WorkingDocument.ProfileOriginDisplayName = WorkingDocument.CurrentProfileName;
            SetLinkedProfileBaselineFromWorkingDocument();
        }
        else
        {
            WorkingDocument.ProfileOriginEntryId = null;
            WorkingDocument.ProfileOriginDisplayName = null;
            ClearLinkedProfileBaseline();
        }

        RefreshCurrentProfileCard();
        OkCommand.RaiseCanExecuteChanged();
    }

    public event EventHandler<bool?>? RequestClose;

    private void BrowseOutput()
    {
        _outputService.BrowseOutputPath(WorkingDocument);
    }

    private async Task EditLocalProfileAsync()
    {
        WorkspaceLocalProfileEditResult? result = await _workspaceLocalProfileDialogService.ShowDialogAsync(
            WorkingDocument.CurrentProfileName,
            WorkingDocument.ProfileEditor.CaptureProfile());

        if (result is null)
            return;

        WorkingDocument.ProfileEditor.WorkingProfileName = NormalizeProfileName(result.ProfileName);
        WorkingDocument.ProfileEditor.ApplyProfile(result.Profile);

        DetachLinkedProfileIfDiverged();
        RefreshCurrentProfileCard();
        OkCommand.RaiseCanExecuteChanged();
    }

    private async Task ChooseProfileAsync()
    {
        await _profileManagerWindowService.ShowDialogAsync(this, ProfileManagerContext.WorkspaceConfiguration);
    }

    private bool CanApplyConfiguration()
    {
        return WorkingDocument.ProfileEditor.CanUseProfile;
    }

    private void ProfileEditor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProfileEditorViewModel.CanUseProfile))
            OkCommand.RaiseCanExecuteChanged();
    }

    private void ApplyAndClose()
    {
        if (!CanApplyConfiguration())
            return;

        DetachLinkedProfileIfDiverged();

        WorkspaceConfigurationSnapshot configuration = WorkspaceConfigurationMapper.Capture(WorkingDocument);

        WorkspaceConfigurationMapper.Apply(_targetDocument, configuration);
        _dirtyStateService.RefreshWorkspaceDirtyState(_targetDocument);
        _dirtyStateService.RefreshPreviewDirtyState(_targetDocument);

        RequestClose?.Invoke(this, true);
    }

    private void DetachLinkedProfileIfDiverged()
    {
        if (WorkingDocument.CurrentProfileEntryId is null ||
            _linkedProfileEntryId is null ||
            _linkedProfileDisplayName is null ||
            _linkedProfileState is null)
        {
            return;
        }

        if (!string.Equals(
                WorkingDocument.CurrentProfileEntryId,
                _linkedProfileEntryId,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        bool hasDiverged =
            !string.Equals(WorkingDocument.CurrentProfileName, _linkedProfileDisplayName, StringComparison.Ordinal) ||
            !Equals(WorkingDocument.ProfileEditor.BuildPreviewProfileSnapshot(), _linkedProfileState);

        if (!hasDiverged)
            return;

        WorkingDocument.CurrentProfileEntryId = null;
        ClearLinkedProfileBaseline();
        RefreshCurrentProfileCard();
    }

    private void SetLinkedProfileBaselineFromWorkingDocument()
    {
        _linkedProfileEntryId = WorkingDocument.CurrentProfileEntryId;
        _linkedProfileDisplayName = WorkingDocument.CurrentProfileName;
        _linkedProfileState = WorkingDocument.ProfileEditor.BuildPreviewProfileSnapshot();
    }

    private void ClearLinkedProfileBaseline()
    {
        _linkedProfileEntryId = null;
        _linkedProfileDisplayName = null;
        _linkedProfileState = null;
    }

    private void RefreshCurrentProfileCard()
    {
        CurrentProfileCard.Apply(
            WorkingDocument.CurrentProfileName,
            WorkingDocument.CurrentProfileEntryId,
            WorkingDocument.ProfileOriginEntryId,
            WorkingDocument.ProfileOriginDisplayName,
            WorkingDocument.ProfileEditor.CaptureProfile());
    }

    private static string NormalizeProfileName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Profile" : value.Trim();
    }

    private void Cancel()
    {
        RequestClose?.Invoke(this, false);
    }
}