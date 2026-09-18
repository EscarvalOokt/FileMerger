using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using FileMerger.Wpf.Features.Profile.Models;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shared.Integration;
using FileMerger.Wpf.Shared.Status;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Profile.ViewModels;

public sealed class ProfileManagerViewModel : ViewModelBase, IAsyncCloseGuard
{
    private const string ProfileLibraryFileFilter =
        "Profile library files (*.filemerger.profile.json)|*.filemerger.profile.json|JSON files (*.json)|*.json|All files (*.*)|*.*";

    private readonly IClipboardService _clipboardService;
    private readonly ProfileManagerContext _context;
    private readonly ICurrentSessionProfileHost _currentSessionProfileHost;

    private readonly ProfileListFiltersViewModel _filters = new();
    private readonly IOpenFileDialogService _openFileDialogService;

    private readonly IProfileLibraryService _profileLibraryService;
    private readonly ISaveFileDialogService _saveFileDialogService;
    private readonly IUserPromptService _userPromptService;
    private string? _creationDraftReturnProfileId;
    private string _description = string.Empty;
    private bool _isBusy;
    private bool _isCreatingProfileDraft;
    private bool _isDirty;
    private DateTime? _loadedCreatedAtUtc;

    private string? _loadedEntryId;
    private string? _loadedFilePath;
    private string _profileCreationSourceLabel = string.Empty;
    private ProfileManagerPage _selectedPage = ProfileManagerPage.Overview;

    private ProfileLibraryListItemViewModel? _selectedProfile;
    private string _statusMessage = "Ready.";
    private StatusSeverity _statusSeverity = StatusSeverity.Info;
    private bool _suppressDirtyTracking;

    public ProfileManagerViewModel(
        IProfileLibraryService profileLibraryService,
        IProfileEditorFactory profileEditorFactory,
        ICurrentSessionProfileHost currentSessionProfileHost,
        IUserPromptService userPromptService,
        IOpenFileDialogService openFileDialogService,
        ISaveFileDialogService saveFileDialogService,
        IClipboardService clipboardService,
        ProfileManagerContext context = ProfileManagerContext.CurrentSession)
    {
        ArgumentNullException.ThrowIfNull(profileLibraryService);
        ArgumentNullException.ThrowIfNull(profileEditorFactory);
        ArgumentNullException.ThrowIfNull(currentSessionProfileHost);
        ArgumentNullException.ThrowIfNull(userPromptService);
        ArgumentNullException.ThrowIfNull(openFileDialogService);
        ArgumentNullException.ThrowIfNull(saveFileDialogService);
        ArgumentNullException.ThrowIfNull(clipboardService);

        if (!Enum.IsDefined(context))
            throw new ArgumentOutOfRangeException(nameof(context));

        _profileLibraryService = profileLibraryService;
        _currentSessionProfileHost = currentSessionProfileHost;
        _userPromptService = userPromptService;
        _openFileDialogService = openFileDialogService;
        _saveFileDialogService = saveFileDialogService;
        _clipboardService = clipboardService;
        _context = context;

        _filters.PropertyChanged += Filters_PropertyChanged;

        ProfileKindFilterOptions =
        [
            new EnumOptionViewModel<ProfileListKindFilterMode>(ProfileListKindFilterMode.All, "All"),
            new EnumOptionViewModel<ProfileListKindFilterMode>(ProfileListKindFilterMode.User, "User"),
            new EnumOptionViewModel<ProfileListKindFilterMode>(ProfileListKindFilterMode.BuiltIn, "Built-in")
        ];

        Profiles = [];
        Profiles.CollectionChanged += Profiles_CollectionChanged;

        ProfileTemplates = [];

        ProfilesView = CollectionViewSource.GetDefaultView(Profiles);
        ProfilesView.Filter = FilterProfile;

        Editor = profileEditorFactory.Create();
        Editor.PropertyChanged += Editor_PropertyChanged;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        NewCommand = new AsyncRelayCommand(StartBlankProfileDraftAsync, () => !IsBusy);
        StartBlankProfileDraftCommand = new AsyncRelayCommand(StartBlankProfileDraftAsync, () => !IsBusy);
        StartProfileTemplateDraftCommand = new AsyncRelayCommand<ProfileTemplateItemViewModel>(
            StartProfileTemplateDraftAsync,
            template => !IsBusy && template is not null);
        CreateProfileDraftCommand = new AsyncRelayCommand(CreateProfileDraftAsync, () => CanCreateProfileDraft);
        CancelProfileDraftCommand = new AsyncRelayCommand(
            CancelProfileDraftAsync,
            () => IsCreatingProfileDraft && !IsBusy);

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy && CanSave);
        SaveAsCommand = new AsyncRelayCommand(SaveAsAsync, () => !IsBusy && CanSaveAs);
        DuplicateCommand = new AsyncRelayCommand(DuplicateSelectedProfileAsync, () => !IsBusy && CanDuplicate);
        ApplyToCurrentSessionCommand = new RelayCommand(ApplyToCurrentSession, () => !IsBusy && CanApply);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => !IsBusy && CanDelete);
        ImportCommand = new AsyncRelayCommand(ImportAsync, () => !IsBusy);
        ExportCommand = new AsyncRelayCommand(ExportAsync, () => !IsBusy && CanExport);
        ClearSearchCommand = new RelayCommand(ClearFilters, () => !IsBusy && Filters.HasActiveFilters);
        CopyStoragePathCommand = new RelayCommand(CopyStoragePath, () => !IsBusy && CanCopyStoragePath);
        ShowOverviewPageCommand = new RelayCommand(ShowOverviewPage, () => !IsBusy);
        ShowEditorPageCommand = new RelayCommand(ShowEditorPage, () => !IsBusy);

        NewProfileCore();
    }

    public ObservableCollection<ProfileLibraryListItemViewModel> Profiles { get; }
    public ObservableCollection<ProfileTemplateItemViewModel> ProfileTemplates { get; }

    public ICollectionView ProfilesView { get; }

    public ProfileListFiltersViewModel Filters => _filters;

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IReadOnlyCollection<EnumOptionViewModel<ProfileListKindFilterMode>> ProfileKindFilterOptions { get; }

    public ProfileEditorViewModel Editor { get; }

    public ProfileSummaryViewModel Summary { get; } = new();

    public ProfileManagerPage SelectedPage
    {
        get => _selectedPage;
        private set
        {
            if (!SetProperty(ref _selectedPage, value))
                return;

            OnPropertyChanged(nameof(IsOverviewPageSelected));
            OnPropertyChanged(nameof(IsEditorPageSelected));
        }
    }

    public bool IsOverviewPageSelected => SelectedPage == ProfileManagerPage.Overview;
    public bool IsEditorPageSelected => SelectedPage == ProfileManagerPage.Editor;

    public ProfileLibraryListItemViewModel? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (!SetProperty(ref _selectedProfile, value))
                return;

            RefreshProfileDirtyMarkers();
            RaiseCommandStates();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
                MarkDirty();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public StatusSeverity StatusSeverity
    {
        get => _statusSeverity;
        private set => SetProperty(ref _statusSeverity, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                RaiseCommandStates();
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (!SetProperty(ref _isDirty, value))
                return;

            RefreshProfileDirtyMarkers();
            RaiseCommandStates();
        }
    }

    public string ProfilesDirectory => _profileLibraryService.GetPrimaryProfilesDirectory();

    public bool HasSelection => SelectedProfile is not null;
    public bool HasSearchText => Filters.HasActiveFilters;

    public bool IsEditingDraft => SelectedProfile is null;
    public bool IsCreatingProfileDraft => _isCreatingProfileDraft;
    public bool IsNotCreatingProfileDraft => !IsCreatingProfileDraft;

    public bool SelectedProfileIsBuiltIn => SelectedProfile?.IsBuiltIn == true;
    public bool SelectedProfileIsUserDefined => SelectedProfile?.IsUserDefined == true;
    public bool SelectedProfileIsReadOnly => SelectedProfile?.IsReadOnly == true;
    public bool SelectedProfileIsUsedByCurrentSession => SelectedProfile?.IsUsedByCurrentSession == true;

    public string UsedProfileLabel => _context == ProfileManagerContext.WorkspaceConfiguration ? "Selected" : "Active";

    public bool SelectedProfileHasStorageLocation =>
        SelectedProfile?.IsUserDefined == true && !string.IsNullOrWhiteSpace(SelectedProfile.Entry.FilePath);

    public bool HasProfileTemplates => ProfileTemplates.Count > 0;

    public string SelectedProfileKindLabel => SelectedProfile?.KindLabel ?? string.Empty;

    public string SelectedProfileStorageLabel
    {
        get
        {
            if (!SelectedProfileHasStorageLocation)
                return string.Empty;

            return SelectedProfile!.Entry.FilePath ?? string.Empty;
        }
    }

    public string ProfileCreationSourceLabel
    {
        get => _profileCreationSourceLabel;
        private set
        {
            if (SetProperty(ref _profileCreationSourceLabel, value))
                OnPropertyChanged(nameof(HasProfileCreationSourceLabel));
        }
    }

    public bool HasProfileCreationSourceLabel => !string.IsNullOrWhiteSpace(ProfileCreationSourceLabel);

    private bool IsEditorContentValidForSave =>
        !string.IsNullOrWhiteSpace(Editor.WorkingProfileName) && Editor.CanUseProfile;

    public bool CanSave => !IsCreatingProfileDraft && IsEditorContentValidForSave;

    public bool CanSaveAs => CanSave;

    public bool CanDuplicate => !IsCreatingProfileDraft && HasSelection;

    public bool CanApply => !IsCreatingProfileDraft && CanSave;

    public bool CanDelete => !IsCreatingProfileDraft && SelectedProfile is { IsReadOnly: false };

    public bool CanExport => CanSave;

    public bool CanCreateProfileDraft => IsCreatingProfileDraft && !IsBusy && IsEditorContentValidForSave;

    public bool CanCopyStoragePath => !string.IsNullOrWhiteSpace(ProfilesDirectory);

    public int VisibleProfileCount => ProfilesView.Cast<object>().Count();

    public int TotalProfileCount => Profiles.Count;

    public string ProfilesSummary => $"{VisibleProfileCount} of {TotalProfileCount} profile(s) shown";

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand NewCommand { get; }
    public AsyncRelayCommand StartBlankProfileDraftCommand { get; }
    public AsyncRelayCommand<ProfileTemplateItemViewModel> StartProfileTemplateDraftCommand { get; }
    public AsyncRelayCommand CreateProfileDraftCommand { get; }
    public AsyncRelayCommand CancelProfileDraftCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand SaveAsCommand { get; }
    public AsyncRelayCommand DuplicateCommand { get; }
    public RelayCommand ApplyToCurrentSessionCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public AsyncRelayCommand ImportCommand { get; }
    public AsyncRelayCommand ExportCommand { get; }
    public RelayCommand ClearSearchCommand { get; }
    public RelayCommand CopyStoragePathCommand { get; }
    public RelayCommand ShowOverviewPageCommand { get; }
    public RelayCommand ShowEditorPageCommand { get; }

    public Task<bool> CanCloseAsync()
    {
        return EnsureEditorCanBeReplacedAsync("closing the profile library");
    }

    public async Task InitializeAsync()
    {
        await RefreshAsync();

        if (SelectedProfile is null && Profiles.Count > 0)
        {
            ProfileLibraryListItemViewModel first = Profiles[0];
            SetSelectedProfileSilently(first);
            await LoadSelectedProfileAsync(first);
        }
        else
        {
            RefreshSummary();
            RefreshCurrentSessionMarkers();
        }
    }

    public async Task RequestSelectProfileAsync(ProfileLibraryListItemViewModel? candidate)
    {
        if (IsBusy)
            return;

        if (ReferenceEquals(candidate, SelectedProfile))
            return;

        if (!await EnsureEditorCanBeReplacedAsync("loading another profile"))
            return;

        ClearProfileCreationDraftState();

        SetSelectedProfileSilently(candidate);

        if (candidate is not null)
            await LoadSelectedProfileAsync(candidate);
    }

    public Task<bool> TryCloseAsync()
    {
        return CanCloseAsync();
    }

    private async Task RefreshAsync()
    {
        string? selectedId = SelectedProfile?.Id;

        try
        {
            IsBusy = true;
            SetStatus("Loading profile library.", StatusSeverity.Info);

            await ReloadProfilesAsync(selectedId);

            SetStatus($"Loaded {Profiles.Count} profile(s).", StatusSeverity.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to load profile library: {ex.Message}", StatusSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NewProfileCore()
    {
        ResetEditorToBlankCore("New profile draft reset.");
    }

    private async Task StartBlankProfileDraftAsync()
    {
        if (IsBusy)
            return;

        if (!await EnsureEditorCanBeReplacedAsync("creating a new profile"))
            return;

        BeginProfileCreationDraftCore(profile: null, sourceLabel: "Blank profile", description: string.Empty);

        ShowEditorPage();

        SetStatus("Creating a blank profile draft.", StatusSeverity.Info);
    }

    private async Task StartProfileTemplateDraftAsync(ProfileTemplateItemViewModel? template)
    {
        if (template is null || IsBusy)
            return;

        if (!await EnsureEditorCanBeReplacedAsync($"creating a profile from '{template.DisplayName}'"))
            return;

        BeginProfileCreationDraftCore(
            profile: template.Entry.Profile,
            sourceLabel: template.SourceLabel,
            description: template.Description);

        ShowOverviewPage();

        SetStatus($"Creating a profile draft from '{template.DisplayName}'.", StatusSeverity.Info);
    }

    private async Task CreateProfileDraftAsync()
    {
        if (!CanCreateProfileDraft)
            return;

        string profileName = NormalizeProfileName(Editor.WorkingProfileName);

        bool saved = await SaveCurrentAsync(saveAsNew: true);
        if (!saved)
            return;

        ClearProfileCreationDraftState();

        SetStatus($"Profile '{profileName}' created.", StatusSeverity.Success);
        RaiseCommandStates();
    }

    private async Task CancelProfileDraftAsync()
    {
        if (!IsCreatingProfileDraft)
            return;

        if (IsDirty && !_userPromptService.Confirm("Discard profile draft", "Discard the current profile draft?"))
        {
            return;
        }

        string? returnProfileId = _creationDraftReturnProfileId;

        ClearProfileCreationDraftState();

        ProfileLibraryListItemViewModel? target = null;

        if (!string.IsNullOrWhiteSpace(returnProfileId))
        {
            target = Profiles.FirstOrDefault(x => string.Equals(
                x.Id,
                returnProfileId,
                StringComparison.OrdinalIgnoreCase));
        }

        target ??= Profiles.FirstOrDefault();

        if (target is not null)
        {
            SetSelectedProfileSilently(target);
            await LoadSelectedProfileAsync(target);
        }
        else
        {
            ResetEditorToBlankCore("Profile creation canceled.");
        }

        SetStatus("Profile creation canceled.", StatusSeverity.Info);
        RaiseCommandStates();
    }

    private void BeginProfileCreationDraftCore(WorkspaceProfileDto? profile, string sourceLabel, string? description)
    {
        _suppressDirtyTracking = true;
        try
        {
            _creationDraftReturnProfileId = SelectedProfile?.Id;

            _loadedEntryId = null;
            _loadedFilePath = null;
            _loadedCreatedAtUtc = null;

            SetSelectedProfileSilently(null);

            if (profile is null)
                Editor.LoadDefaults();
            else
                Editor.ApplyProfile(profile);

            Editor.WorkingProfileName = BuildNextUntitledName();
            Description = description ?? string.Empty;

            SetProfileCreationDraftState(isCreatingProfileDraft: true, sourceLabel: sourceLabel);

            IsDirty = false;

            RefreshSummary();
            RefreshCurrentSessionMarkers();
        }
        finally
        {
            _suppressDirtyTracking = false;
            RaiseCommandStates();
        }
    }

    private void ResetEditorToBlankCore(string statusMessage)
    {
        _suppressDirtyTracking = true;
        try
        {
            _loadedEntryId = null;
            _loadedFilePath = null;
            _loadedCreatedAtUtc = null;

            SetSelectedProfileSilently(null);

            Editor.LoadDefaults();
            Editor.WorkingProfileName = "New Profile";
            Description = string.Empty;

            ClearProfileCreationDraftState();

            IsDirty = false;

            RefreshSummary();
            RefreshCurrentSessionMarkers();

            SetStatus(statusMessage, StatusSeverity.Info);
        }
        finally
        {
            _suppressDirtyTracking = false;
            RaiseCommandStates();
        }
    }

    private void SetProfileCreationDraftState(bool isCreatingProfileDraft, string sourceLabel)
    {
        if (SetProperty(ref _isCreatingProfileDraft, isCreatingProfileDraft, nameof(IsCreatingProfileDraft)))
            OnPropertyChanged(nameof(IsNotCreatingProfileDraft));

        ProfileCreationSourceLabel = sourceLabel;
    }

    private void ClearProfileCreationDraftState()
    {
        _creationDraftReturnProfileId = null;

        SetProfileCreationDraftState(isCreatingProfileDraft: false, sourceLabel: string.Empty);
    }

    private string BuildNextUntitledName()
    {
        var usedNames = Profiles.Select(x => x.DisplayName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        int index = 1;

        while (true)
        {
            string candidate = $"Untitled {index}";
            if (!usedNames.Contains(candidate))
                return candidate;

            index++;
        }
    }

    private async Task DuplicateSelectedProfileAsync()
    {
        if (SelectedProfile is null)
            return;

        if (!await EnsureEditorCanBeReplacedAsync("duplicating the selected profile"))
            return;

        ProfileLibraryEntry source = SelectedProfile.Entry;

        _suppressDirtyTracking = true;
        try
        {
            _loadedEntryId = null;
            _loadedFilePath = null;
            _loadedCreatedAtUtc = null;

            SetSelectedProfileSilently(null);

            Editor.ApplyProfile(source.Profile);
            Editor.WorkingProfileName = BuildDuplicateName(source.DisplayName);
            Description = source.Description;

            IsDirty = true;

            RefreshSummary();
            RefreshCurrentSessionMarkers();

            ShowEditorPage();

            SetStatus(
                $"Profile '{source.DisplayName}' duplicated in editor. Save it to persist the copy.",
                StatusSeverity.Success);
        }
        finally
        {
            _suppressDirtyTracking = false;
            RaiseCommandStates();
        }
    }

    private async Task LoadSelectedProfileAsync(ProfileLibraryListItemViewModel? selected)
    {
        if (selected is null)
            return;

        try
        {
            IsBusy = true;
            SetStatus($"Loading '{selected.DisplayName}'.", StatusSeverity.Info);

            ProfileLibraryEntry entry = await _profileLibraryService.LoadAsync(selected.Id);
            ApplyLoadedEntry(entry, selected);

            SetStatus($"Loaded '{entry.DisplayName}'.", StatusSeverity.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to load profile: {ex.Message}", StatusSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task<bool> SaveAsync()
    {
        return SaveCurrentAsync(ShouldSaveAsNew());
    }

    private Task<bool> SaveAsAsync()
    {
        return SaveCurrentAsync(saveAsNew: true);
    }

    private async Task<bool> SaveCurrentAsync(bool saveAsNew)
    {
        if (IsBusy)
            return false;

        if (!IsEditorContentValidForSave)
        {
            SetStatus(
                string.IsNullOrWhiteSpace(Editor.WorkingProfileName)
                    ? "Profile was not saved. Profile name cannot be empty."
                    : $"Profile was not saved. {Editor.DraftValidationMessage}",
                StatusSeverity.Error);

            return false;
        }

        try
        {
            IsBusy = true;
            SetStatus(saveAsNew ? "Saving profile as new." : "Saving profile.", StatusSeverity.Info);

            ProfileLibraryEntry entry = BuildEntry(saveAsNew);
            ProfileLibraryEntry saved = await _profileLibraryService.SaveAsync(entry, saveAsNew);

            await AfterSaveAsync(saved);

            SetStatus(
                saveAsNew ? $"Profile '{saved.DisplayName}' saved as new." : $"Profile '{saved.DisplayName}' saved.",
                StatusSeverity.Success);

            return true;
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to save profile: {ex.Message}", StatusSeverity.Error);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool ShouldSaveAsNew()
    {
        return SelectedProfile?.IsReadOnly == true || string.IsNullOrWhiteSpace(_loadedEntryId);
    }

    private void ApplyLoadedEntry(ProfileLibraryEntry entry, ProfileLibraryListItemViewModel? selected)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _suppressDirtyTracking = true;
        try
        {
            _loadedEntryId = entry.Id;
            _loadedFilePath = entry.FilePath;
            _loadedCreatedAtUtc = entry.CreatedAtUtc;

            ClearProfileCreationDraftState();
            SetSelectedProfileSilently(selected);

            Editor.WorkingProfileName = entry.DisplayName;
            Description = entry.Description;
            Editor.ApplyProfile(entry.Profile);

            IsDirty = false;
        }
        finally
        {
            _suppressDirtyTracking = false;
        }

        ShowOverviewPage();

        RefreshSummary();
        RefreshCurrentSessionMarkers();
        RaiseCommandStates();
    }

    private ProfileLibraryEntry BuildEntry(bool saveAsNew)
    {
        string profileName = NormalizeProfileName(Editor.WorkingProfileName);

        ProfileMetadataDto metadata = new(
            Id: saveAsNew || string.IsNullOrWhiteSpace(_loadedEntryId) ? Guid.NewGuid().ToString("N") : _loadedEntryId!,
            Name: profileName,
            Description: NormalizeDescription(Description),
            CreatedAtUtc: saveAsNew ? null : _loadedCreatedAtUtc,
            UpdatedAtUtc: DateTime.UtcNow,
            IsBuiltIn: false,
            IsReadOnly: false);

        return new ProfileLibraryEntry(
            Metadata: metadata,
            Profile: Editor.CaptureProfile(),
            FilePath: saveAsNew ? null : _loadedFilePath,
            Kind: ProfileEntryKind.User);
    }

    private ProfileLibraryEntry BuildExportEntry()
    {
        string profileName = NormalizeProfileName(Editor.WorkingProfileName);

        ProfileMetadataDto metadata = new(
            Id: string.IsNullOrWhiteSpace(_loadedEntryId) ? Guid.NewGuid().ToString("N") : _loadedEntryId!,
            Name: profileName,
            Description: NormalizeDescription(Description),
            CreatedAtUtc: _loadedCreatedAtUtc ?? DateTime.UtcNow,
            UpdatedAtUtc: DateTime.UtcNow,
            IsBuiltIn: false,
            IsReadOnly: false);

        return new ProfileLibraryEntry(
            Metadata: metadata,
            Profile: Editor.CaptureProfile(),
            FilePath: null,
            Kind: ProfileEntryKind.User);
    }

    private async Task AfterSaveAsync(ProfileLibraryEntry saved)
    {
        MarkCurrentStateAsSaved(saved);

        await ReloadProfilesAsync(saved.Id);

        ProfileLibraryListItemViewModel? selected = Profiles.FirstOrDefault(x =>
            string.Equals(x.Id, saved.Id, StringComparison.OrdinalIgnoreCase));

        ApplyLoadedEntry(saved, selected);
    }

    private void ApplyToCurrentSession()
    {
        if (IsBusy)
            return;

        if (!CanApply)
        {
            if (!Editor.CanUseProfile)
                SetStatus($"Profile was not applied. {Editor.DraftValidationMessage}", StatusSeverity.Error);

            return;
        }

        WorkspaceProfileDto profile = Editor.CaptureProfile();

        string? appliedEntryId = !IsDirty && !string.IsNullOrWhiteSpace(_loadedEntryId) ? _loadedEntryId : null;

        _currentSessionProfileHost.ApplyProfileToCurrentSession(Editor.WorkingProfileName, profile, appliedEntryId);

        RefreshCurrentSessionMarkers();

        string statusMessage = _context == ProfileManagerContext.WorkspaceConfiguration
            ? "Profile selected for workspace configuration. Confirm with OK to apply it."
            : "Profile applied to current session.";

        SetStatus(statusMessage, StatusSeverity.Success);
    }

    private async Task DeleteAsync()
    {
        if (SelectedProfile is null || SelectedProfile.IsReadOnly)
            return;

        if (!_userPromptService.Confirm(
                "Delete profile",
                $"Delete profile '{SelectedProfile.DisplayName}'?",
                confirmButtonText: "Delete",
                cancelButtonText: "Cancel"))
        {
            return;
        }

        string displayName = SelectedProfile.DisplayName;
        string deletedId = SelectedProfile.Id;

        try
        {
            IsBusy = true;
            SetStatus($"Deleting '{displayName}'.", StatusSeverity.Info);

            await _profileLibraryService.DeleteAsync(deletedId);
            await ReloadProfilesAsync(preferredSelectedId: null);

            if (string.Equals(_loadedEntryId, deletedId, StringComparison.OrdinalIgnoreCase))
                NewProfileCore();
            else
                RefreshCurrentSessionMarkers();

            SetStatus($"Profile '{displayName}' deleted.", StatusSeverity.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to delete profile: {ex.Message}", StatusSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ImportAsync()
    {
        if (!await EnsureEditorCanBeReplacedAsync("importing profile files"))
            return;

        ClearProfileCreationDraftState();

        string initialPath = Path.Combine(
            _profileLibraryService.GetPrimaryProfilesDirectory(),
            "ImportedProfile.filemerger.profile.json");

        IReadOnlyList<string> filePaths = _openFileDialogService.SelectFiles(initialPath, ProfileLibraryFileFilter);

        if (filePaths.Count == 0)
            return;

        try
        {
            IsBusy = true;
            SetStatus("Importing profiles.", StatusSeverity.Info);

            IReadOnlyCollection<ProfileLibraryEntry> imported = await _profileLibraryService.ImportAsync(filePaths);

            ProfileLibraryEntry? lastImported = imported.LastOrDefault();

            await ReloadProfilesAsync(lastImported?.Id);

            if (lastImported is not null)
            {
                ProfileLibraryListItemViewModel? selected = Profiles.FirstOrDefault(x =>
                    string.Equals(x.Id, lastImported.Id, StringComparison.OrdinalIgnoreCase));

                if (selected is not null)
                {
                    SetSelectedProfileSilently(selected);
                    await LoadSelectedProfileAsync(selected);
                }
            }

            SetStatus(
                imported.Count == 0 ? "No valid profiles were imported." : $"Imported {imported.Count} profile(s).",
                imported.Count == 0 ? StatusSeverity.Warning : StatusSeverity.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to import profiles: {ex.Message}", StatusSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportAsync()
    {
        try
        {
            IsBusy = true;
            SetStatus("Exporting profile.", StatusSeverity.Info);

            string defaultPath = BuildDefaultExportPath();
            string? targetPath = _saveFileDialogService.SelectSaveFilePath(defaultPath, ProfileLibraryFileFilter);

            if (string.IsNullOrWhiteSpace(targetPath))
                return;

            ProfileLibraryEntry exportEntry = BuildExportEntry();

            await _profileLibraryService.ExportAsync(exportEntry, targetPath);

            SetStatus($"Profile exported to '{Path.GetFileName(targetPath)}'.", StatusSeverity.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to export profile: {ex.Message}", StatusSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CopyStoragePath()
    {
        if (!CanCopyStoragePath)
            return;

        try
        {
            _clipboardService.SetText(ProfilesDirectory);
            SetStatus("Storage location copied to clipboard.", StatusSeverity.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to copy storage location: {ex.Message}", StatusSeverity.Error);
        }
    }

    private void ShowOverviewPage()
    {
        SelectedPage = ProfileManagerPage.Overview;
    }

    private void ShowEditorPage()
    {
        SelectedPage = ProfileManagerPage.Editor;
    }

    private async Task ReloadProfilesAsync(string? preferredSelectedId)
    {
        IReadOnlyCollection<ProfileLibraryEntry> entries = await _profileLibraryService.GetAllAsync();

        RefreshProfileTemplates(entries);

        Profiles.Clear();

        foreach (ProfileLibraryEntry entry in entries)
            Profiles.Add(new ProfileLibraryListItemViewModel(entry, UsedProfileLabel));

        if (!string.IsNullOrWhiteSpace(preferredSelectedId))
        {
            ProfileLibraryListItemViewModel? preferred = Profiles.FirstOrDefault(x => string.Equals(
                x.Id,
                preferredSelectedId,
                StringComparison.OrdinalIgnoreCase));

            SetSelectedProfileSilently(preferred);
        }
        else if (SelectedProfile is not null && !Profiles.Contains(SelectedProfile))
        {
            SetSelectedProfileSilently(null);
        }

        RefreshCurrentSessionMarkers();
        RefreshSummary();
        RefreshProfilesView();
    }

    private void RefreshProfileTemplates(IEnumerable<ProfileLibraryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        ProfileTemplates.Clear();

        foreach (ProfileLibraryEntry entry in entries.Where(x => x.IsBuiltIn))
            ProfileTemplates.Add(new ProfileTemplateItemViewModel(entry));

        OnPropertyChanged(nameof(HasProfileTemplates));
        StartProfileTemplateDraftCommand.RaiseCanExecuteChanged();
    }

    private async Task<bool> EnsureEditorCanBeReplacedAsync(string actionLabel)
    {
        if (IsCreatingProfileDraft)
        {
            if (!IsDirty)
                return true;

            return _userPromptService.Confirm(
                "Discard profile draft",
                $"Discard the current profile draft before {actionLabel}?");
        }

        if (!IsDirty)
            return true;

        UnsavedChangesDecision decision = _userPromptService.ConfirmUnsavedChanges(
            "Unsaved profile changes",
            $"The current profile has unsaved changes. Save them before {actionLabel}?");

        return decision switch
        {
            UnsavedChangesDecision.Save => await SaveCurrentAsync(ShouldSaveAsNew()),
            UnsavedChangesDecision.Discard => true,
            _ => false
        };
    }

    private void RefreshCurrentSessionMarkers()
    {
        string? currentEntryId = _currentSessionProfileHost.CurrentProfileEntryId;

        foreach (ProfileLibraryListItemViewModel item in Profiles)
        {
            bool used = !string.IsNullOrWhiteSpace(currentEntryId) &&
                        string.Equals(item.Id, currentEntryId, StringComparison.OrdinalIgnoreCase);

            item.SetUsedByCurrentSession(used);
        }

        RefreshProfilesView();
        RaiseCommandStates();
    }

    private void RefreshProfileDirtyMarkers()
    {
        foreach (ProfileLibraryListItemViewModel item in Profiles)
        {
            bool isDirtyProfile = !IsCreatingProfileDraft && IsDirty && ReferenceEquals(item, SelectedProfile);

            item.SetDirty(isDirtyProfile);
        }
    }

    private void RefreshSummary()
    {
        Summary.Apply(Editor.CaptureProfile());
        OnPropertyChanged(nameof(ProfilesSummary));
    }

    private void Editor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (IsEditorPresentationProperty(e.PropertyName))
            return;

        RefreshSummary();
        MarkDirty();
        RaiseCommandStates();
    }

    private static bool IsEditorPresentationProperty(string? propertyName)
    {
        return propertyName is nameof(ProfileEditorViewModel.SelectedSection)
            or nameof(ProfileEditorViewModel.IsGeneralSectionSelected)
            or nameof(ProfileEditorViewModel.IsFormattingSectionSelected)
            or nameof(ProfileEditorViewModel.IsOutputMetadataSectionSelected)
            or nameof(ProfileEditorViewModel.IsInputEncodingSectionSelected)
            or nameof(ProfileEditorViewModel.IsUnsupportedTextFallbackSectionSelected)
            or nameof(ProfileEditorViewModel.IsFileTypesSectionSelected)
            or nameof(ProfileEditorViewModel.IsFilterRulesSectionSelected)
            or nameof(ProfileEditorViewModel.DraftValidationMessage)
            or

            // File type search/filter UI state.
            nameof(ProfileEditorViewModel.FileTypeFilters)
            or nameof(ProfileEditorViewModel.FileTypeGroups)
            or nameof(ProfileEditorViewModel.HasVisibleFileTypeGroups)
            or nameof(ProfileEditorViewModel.HasFileTypeFilterEmptyState)
            or nameof(ProfileEditorViewModel.FileTypeFilterSummary)
            or

            // Filter rule search/filter UI state.
            nameof(ProfileEditorViewModel.FilterRuleFilters)
            or nameof(ProfileEditorViewModel.VisibleFilterRules)
            or nameof(ProfileEditorViewModel.HasVisibleFilterRules)
            or nameof(ProfileEditorViewModel.HasFilterRuleFilterEmptyState)
            or nameof(ProfileEditorViewModel.FilterRuleFilterSummary)
            or

            // Selection and command-state changes do not modify profile data.
            nameof(ProfileEditorViewModel.SelectedFilterRule)
            or nameof(ProfileEditorViewModel.HasSelectedFilterRule)
            or nameof(ProfileEditorViewModel.CanDuplicateFilterRule)
            or nameof(ProfileEditorViewModel.CanRemoveFilterRule)
            or nameof(ProfileEditorViewModel.CanMoveFilterRuleUp)
            or nameof(ProfileEditorViewModel.CanMoveFilterRuleDown);
    }

    private void Profiles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshProfilesView();
    }

    private void Filters_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshProfilesView();
        ClearSearchCommand.RaiseCanExecuteChanged();
    }

    private void MarkDirty()
    {
        if (_suppressDirtyTracking)
            return;

        IsDirty = true;
    }

    private void SetSelectedProfileSilently(ProfileLibraryListItemViewModel? value)
    {
        if (!SetProperty(ref _selectedProfile, value))
            return;

        RefreshProfileDirtyMarkers();
        RaiseCommandStates();
    }

    private void ClearFilters()
    {
        Filters.Reset();
        RefreshProfilesView();
    }

    private bool FilterProfile(object obj)
    {
        return obj is ProfileLibraryListItemViewModel item && Filters.Matches(item);
    }

    private void RefreshProfilesView()
    {
        ProfilesView.Refresh();

        ClearSearchCommand.RaiseCanExecuteChanged();

        OnPropertyChanged(nameof(HasSearchText));
        OnPropertyChanged(nameof(VisibleProfileCount));
        OnPropertyChanged(nameof(TotalProfileCount));
        OnPropertyChanged(nameof(ProfilesSummary));
    }

    private void SetStatus(string message, StatusSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(message);

        StatusMessage = message;
        StatusSeverity = severity;
    }

    private void RaiseCommandStates()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        NewCommand.RaiseCanExecuteChanged();
        StartBlankProfileDraftCommand.RaiseCanExecuteChanged();
        StartProfileTemplateDraftCommand.RaiseCanExecuteChanged();
        CreateProfileDraftCommand.RaiseCanExecuteChanged();
        CancelProfileDraftCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
        SaveAsCommand.RaiseCanExecuteChanged();
        DuplicateCommand.RaiseCanExecuteChanged();
        ApplyToCurrentSessionCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
        ImportCommand.RaiseCanExecuteChanged();
        ExportCommand.RaiseCanExecuteChanged();
        ClearSearchCommand.RaiseCanExecuteChanged();
        CopyStoragePathCommand.RaiseCanExecuteChanged();
        ShowOverviewPageCommand.RaiseCanExecuteChanged();
        ShowEditorPageCommand.RaiseCanExecuteChanged();

        OnPropertyChanged(nameof(IsOverviewPageSelected));
        OnPropertyChanged(nameof(IsEditorPageSelected));

        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasSearchText));

        OnPropertyChanged(nameof(IsEditingDraft));
        OnPropertyChanged(nameof(IsCreatingProfileDraft));
        OnPropertyChanged(nameof(IsNotCreatingProfileDraft));
        OnPropertyChanged(nameof(SelectedProfileIsBuiltIn));
        OnPropertyChanged(nameof(SelectedProfileIsUserDefined));
        OnPropertyChanged(nameof(SelectedProfileIsReadOnly));
        OnPropertyChanged(nameof(SelectedProfileIsUsedByCurrentSession));
        OnPropertyChanged(nameof(SelectedProfileHasStorageLocation));
        OnPropertyChanged(nameof(SelectedProfileKindLabel));
        OnPropertyChanged(nameof(SelectedProfileStorageLabel));
        OnPropertyChanged(nameof(ProfileCreationSourceLabel));
        OnPropertyChanged(nameof(HasProfileCreationSourceLabel));
        OnPropertyChanged(nameof(HasProfileTemplates));

        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanSaveAs));
        OnPropertyChanged(nameof(CanDuplicate));
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanExport));
        OnPropertyChanged(nameof(CanCreateProfileDraft));
        OnPropertyChanged(nameof(CanCopyStoragePath));

        OnPropertyChanged(nameof(VisibleProfileCount));
        OnPropertyChanged(nameof(TotalProfileCount));
        OnPropertyChanged(nameof(ProfilesSummary));
    }

    private void MarkCurrentStateAsSaved(ProfileLibraryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _suppressDirtyTracking = true;
        try
        {
            _loadedEntryId = entry.Id;
            _loadedFilePath = entry.FilePath;
            _loadedCreatedAtUtc = entry.CreatedAtUtc;

            Editor.WorkingProfileName = entry.DisplayName;
            Description = entry.Description;

            IsDirty = false;
        }
        finally
        {
            _suppressDirtyTracking = false;
        }

        RefreshProfileDirtyMarkers();
        RefreshCurrentSessionMarkers();
        RefreshSummary();
        RefreshProfilesView();
    }

    private string BuildDefaultExportPath()
    {
        string safeName = MakeSafeFileName(Editor.WorkingProfileName, "Profile");
        return Path.Combine(
            _profileLibraryService.GetPrimaryProfilesDirectory(),
            $"{safeName}.filemerger.profile.json");
    }

    private static string BuildDuplicateName(string displayName)
    {
        string baseName = string.IsNullOrWhiteSpace(displayName) ? "New Profile" : displayName.Trim();

        return $"{baseName} Copy";
    }

    private static string NormalizeProfileName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Profile" : value.Trim();
    }

    private static string? NormalizeDescription(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string MakeSafeFileName(string? value, string fallback)
    {
        string candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            candidate = candidate.Replace(invalidChar, '_');

        return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
    }
}