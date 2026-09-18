using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using FileMerger.Wpf.Features.Files.ViewModels;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Preview.ViewModels;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Session.ViewModels;
using FileMerger.Wpf.Features.Settings;
using FileMerger.Wpf.Features.Settings.Dialogs;
using FileMerger.Wpf.Features.Sources.ViewModels;
using FileMerger.Wpf.Features.Updates.Dialogs;
using FileMerger.Wpf.Features.Validation.ViewModels;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.Configuration.Dialogs;
using FileMerger.Wpf.Features.Workspace.Recent;
using FileMerger.Wpf.Features.Workspace.State;
using FileMerger.Wpf.Features.Workspace.Tabs;
using FileMerger.Wpf.Features.Workspace.Tabs.Dialogs;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shared.Integration;
using FileMerger.Wpf.Shared.Status;
using FileMerger.Wpf.Shared.ViewModels;
using FileMerger.Wpf.Shell.Help;

namespace FileMerger.Wpf.Shell.Main;

public sealed class MainViewModel : ViewModelBase, ICurrentSessionProfileHost, IAsyncCloseGuard
{
    private readonly IApplicationPreferencesStore _applicationPreferencesStore;
    private readonly IClipboardService _clipboardService;
    private readonly IKeyboardShortcutsDialogService _keyboardShortcutsDialogService;
    private readonly IPreferencesDialogService _preferencesDialogService;
    private readonly IProfileManagerWindowService _profileManagerWindowService;
    private readonly IRecentWorkspacesService _recentWorkspacesService;
    private readonly IUpdateCheckDialogService _updateCheckDialogService;
    private readonly IUserPromptService _userPromptService;
    private readonly IWorkspaceConfigurationDialogService _workspaceConfigurationDialogService;
    private readonly IWorkspaceDocumentDirtyStateService _workspaceDocumentDirtyStateService;
    private readonly IWorkspaceDocumentLifecycleService _workspaceDocumentLifecycleService;
    private readonly IWorkspaceDocumentOutputService _workspaceDocumentOutputService;
    private readonly IWorkspaceDocumentPreviewService _workspaceDocumentPreviewService;
    private readonly IWorkspaceTabRenameDialogService _workspaceTabRenameDialogService;

    private WorkspaceDocumentViewModel? _subscribedDocument;

    public MainViewModel(
        IWorkspaceDocumentPreviewService workspaceDocumentPreviewService,
        IWorkspaceDocumentOutputService workspaceDocumentOutputService,
        IWorkspaceDocumentLifecycleService workspaceDocumentLifecycleService,
        IWorkspaceDocumentDirtyStateService workspaceDocumentDirtyStateService,
        IProfileManagerWindowService profileManagerWindowService,
        IPreferencesDialogService preferencesDialogService,
        IWorkspaceConfigurationDialogService workspaceConfigurationDialogService,
        IWorkspaceTabRenameDialogService workspaceTabRenameDialogService,
        IClipboardService clipboardService,
        IKeyboardShortcutsDialogService keyboardShortcutsDialogService,
        IUpdateCheckDialogService updateCheckDialogService,
        IUserPromptService userPromptService,
        IRecentWorkspacesService recentWorkspacesService,
        IApplicationPreferencesStore applicationPreferencesStore,
        WorkspaceTabManagerViewModel workspaceTabs)
    {
        ArgumentNullException.ThrowIfNull(workspaceDocumentPreviewService);
        ArgumentNullException.ThrowIfNull(workspaceDocumentOutputService);
        ArgumentNullException.ThrowIfNull(workspaceDocumentLifecycleService);
        ArgumentNullException.ThrowIfNull(workspaceDocumentDirtyStateService);
        ArgumentNullException.ThrowIfNull(profileManagerWindowService);
        ArgumentNullException.ThrowIfNull(preferencesDialogService);
        ArgumentNullException.ThrowIfNull(workspaceConfigurationDialogService);
        ArgumentNullException.ThrowIfNull(workspaceTabRenameDialogService);
        ArgumentNullException.ThrowIfNull(clipboardService);
        ArgumentNullException.ThrowIfNull(keyboardShortcutsDialogService);
        ArgumentNullException.ThrowIfNull(updateCheckDialogService);
        ArgumentNullException.ThrowIfNull(userPromptService);
        ArgumentNullException.ThrowIfNull(recentWorkspacesService);
        ArgumentNullException.ThrowIfNull(applicationPreferencesStore);
        ArgumentNullException.ThrowIfNull(workspaceTabs);

        _workspaceDocumentPreviewService = workspaceDocumentPreviewService;
        _workspaceDocumentOutputService = workspaceDocumentOutputService;
        _workspaceDocumentLifecycleService = workspaceDocumentLifecycleService;
        _workspaceDocumentDirtyStateService = workspaceDocumentDirtyStateService;
        _profileManagerWindowService = profileManagerWindowService;
        _preferencesDialogService = preferencesDialogService;
        _workspaceConfigurationDialogService = workspaceConfigurationDialogService;
        _workspaceTabRenameDialogService = workspaceTabRenameDialogService;
        _clipboardService = clipboardService;
        _keyboardShortcutsDialogService = keyboardShortcutsDialogService;
        _updateCheckDialogService = updateCheckDialogService;
        _userPromptService = userPromptService;
        _recentWorkspacesService = recentWorkspacesService;
        _applicationPreferencesStore = applicationPreferencesStore;

        WorkspaceTabs = workspaceTabs;
        WorkspaceTabs.PropertyChanged += WorkspaceTabs_PropertyChanged;
        ((INotifyCollectionChanged)WorkspaceTabs.Tabs).CollectionChanged += WorkspaceTabs_CollectionChanged;

        foreach (WorkspaceTabViewModel tab in WorkspaceTabs.Tabs)
            tab.PropertyChanged += WorkspaceTab_PropertyChanged;

        SubscribeToDocument(CurrentDocument);

        RecentWorkspaces = [];
        MissingRecentWorkspaces = [];

        BuildPreviewCommand = new AsyncRelayCommand(BuildPreviewAsync, () => !OperationStatus.IsBusy);
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CopyPreviewCommand = new RelayCommand(CopyPreview, CanCopyPreview);
        OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder, CanOpenOutputFolder);
        TogglePreviewSummaryCommand = new RelayCommand(TogglePreviewSummary);

        NewWorkspaceTabCommand = new RelayCommand(NewWorkspaceTab, () => !OperationStatus.IsBusy);
        SaveWorkspaceCommand = new AsyncRelayCommand(SaveWorkspaceAsync, () => !OperationStatus.IsBusy);
        SaveWorkspaceAsCommand = new AsyncRelayCommand(SaveWorkspaceAsAsync, () => !OperationStatus.IsBusy);
        LoadWorkspaceCommand = new AsyncRelayCommand(LoadWorkspaceAsync, () => !OperationStatus.IsBusy);

        RefreshRecentWorkspacesCommand = new AsyncRelayCommand(RefreshRecentWorkspacesAsync);

        OpenRecentWorkspaceCommand = new AsyncRelayCommand<RecentWorkspaceMenuItemViewModel>(
            OpenRecentWorkspaceAsync,
            CanOpenRecentWorkspace);

        RemoveMissingRecentWorkspaceCommand = new AsyncRelayCommand<RecentWorkspaceMenuItemViewModel>(
            RemoveMissingRecentWorkspaceAsync,
            CanRemoveMissingRecentWorkspace);

        ClearRecentWorkspacesCommand = new AsyncRelayCommand(
            ClearRecentWorkspacesAsync,
            () => !OperationStatus.IsBusy && HasRecentWorkspaces);

        DuplicateWorkspaceTabCommand = new RelayCommand(
            DuplicateWorkspaceTab,
            () => WorkspaceTabs.CanDuplicateTab(WorkspaceTabs.ActiveTab));

        CloseActiveWorkspaceTabCommand = new AsyncRelayCommand(
            CloseActiveWorkspaceTabAsync,
            () => WorkspaceTabs.CanCloseTab(WorkspaceTabs.ActiveTab));

        CloseOtherWorkspaceTabsCommand = new AsyncRelayCommand(
            CloseOtherWorkspaceTabsAsync,
            () => WorkspaceTabs.CanCloseOtherTabs());

        RenameWorkspaceTabCommand = new RelayCommand(
            RenameWorkspaceTab,
            () => WorkspaceTabs.CanRenameTab(WorkspaceTabs.ActiveTab));

        SelectNextWorkspaceTabCommand = new RelayCommand(
            SelectNextWorkspaceTab,
            () => WorkspaceTabs.CanSelectNextTab());

        SelectPreviousWorkspaceTabCommand = new RelayCommand(
            SelectPreviousWorkspaceTab,
            () => WorkspaceTabs.CanSelectPreviousTab());

        OpenProfilesCommand = new AsyncRelayCommand(OpenProfilesAsync, () => !OperationStatus.IsBusy);
        OpenWorkspaceConfigurationCommand = new AsyncRelayCommand(
            OpenWorkspaceConfigurationAsync,
            () => !CurrentDocument.OperationStatus.IsBusy);
        OpenPreferencesCommand = new AsyncRelayCommand(
            OpenPreferencesAsync,
            () => !CurrentDocument.OperationStatus.IsBusy);
        OpenKeyboardShortcutsCommand = new RelayCommand(OpenKeyboardShortcuts);
        OpenUpdateCheckCommand = new RelayCommand(OpenUpdateCheck);

        CurrentProfileCard = new CurrentProfileCardViewModel();
        RefreshCurrentProfileCard();
        RefreshFooterState();
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public WorkspaceTabManagerViewModel WorkspaceTabs { get; }

    public WorkspaceDocumentViewModel CurrentDocument => WorkspaceTabs.ActiveDocument;

    public bool HasMultipleWorkspaceTabs => WorkspaceTabs.Tabs.Count > 1;

    public ObservableCollection<RecentWorkspaceMenuItemViewModel> RecentWorkspaces { get; }

    public ObservableCollection<RecentWorkspaceMenuItemViewModel> MissingRecentWorkspaces { get; }

    public bool HasRecentWorkspaces => RecentWorkspaces.Count > 0;

    public bool HasNoRecentWorkspaces => !HasRecentWorkspaces;

    public bool HasMissingRecentWorkspaces => MissingRecentWorkspaces.Count > 0;

    public SessionSettingsViewModel SessionSettings => CurrentDocument.SessionSettings;
    public ProfileEditorViewModel ProfileEditor => CurrentDocument.ProfileEditor;
    public SourcesPaneViewModel SourcesPane => CurrentDocument.SourcesPane;
    public FilesPaneViewModel FilesPane => CurrentDocument.FilesPane;
    public OperationStatusViewModel OperationStatus => CurrentDocument.OperationStatus;
    public PreviewDirtyStateTracker PreviewDirtyTracker => CurrentDocument.PreviewDirtyTracker;
    public WorkspaceDirtyStateTracker WorkspaceDirtyTracker => CurrentDocument.WorkspaceDirtyTracker;
    public ValidationPaneViewModel ValidationPane => CurrentDocument.ValidationPane;
    public CurrentProfileCardViewModel CurrentProfileCard { get; }

    public string PreviewContent
    {
        get => CurrentDocument.PreviewContent;
        set => CurrentDocument.PreviewContent = value;
    }

    public string PreviewNotice
    {
        get => CurrentDocument.PreviewNotice;
        set => CurrentDocument.PreviewNotice = value;
    }

    public string PreviewCharacterCountText => CurrentDocument.PreviewCharacterCountText;

    public bool HasPreviewNotice => CurrentDocument.HasPreviewNotice;

    public PreviewGenerationSummaryViewModel PreviewSummary => CurrentDocument.PreviewSummary;

    public bool HasPreviewSummary => CurrentDocument.HasPreviewSummary;

    public bool IsPreviewSummaryExpanded => CurrentDocument.IsPreviewSummaryExpanded;

    public bool IsPreviewSummaryCollapsed => CurrentDocument.IsPreviewSummaryCollapsed;

    public string SaveOutputActionText => HasStaleBuiltOutput ? "Save Last Build" : "Save";

    public string SaveOutputActionTooltip =>
        HasStaleBuiltOutput
            ? "Save the last successfully built output. Changes made since that build are not included in its content; run Build Preview to rebuild it first."
            : "Save the latest built output.";

    public bool ShowDiscoveredFilesEmptyState => CurrentDocument.ShowDiscoveredFilesEmptyState;
    public string DiscoveredFilesEmptyTitle => CurrentDocument.DiscoveredFilesEmptyTitle;
    public string DiscoveredFilesEmptyDescription => CurrentDocument.DiscoveredFilesEmptyDescription;

    public bool ShowPreviewEmptyState => CurrentDocument.ShowPreviewEmptyState;
    public string PreviewEmptyTitle => CurrentDocument.PreviewEmptyTitle;
    public string PreviewEmptyDescription => CurrentDocument.PreviewEmptyDescription;
    public bool ShowPreviewEmptyBuildAction => CurrentDocument.ShowPreviewEmptyBuildAction;

    public bool ShowFirstWorkspaceGuide => CurrentDocument.IsEmptyWorkspace;

    public bool ShowRegularPreviewEmptyState => ShowPreviewEmptyState && !ShowFirstWorkspaceGuide;

    public static string FirstWorkspaceGuideTitle => "Start your first workspace";

    public static string FirstWorkspaceGuideDescription =>
        "Add sources, open an existing workspace, or choose a profile to begin.";

    public string FirstWorkspaceProfileHint =>
        string.IsNullOrWhiteSpace(CurrentProfileName)
            ? "Current profile: Default"
            : $"Current profile: {CurrentProfileName}";

    public RecentWorkspaceMenuItemViewModel? FirstRecentWorkspace => RecentWorkspaces.FirstOrDefault(x => x.Exists);

    public bool HasFirstRecentWorkspace => FirstRecentWorkspace is not null;

    public string FirstRecentWorkspaceText =>
        FirstRecentWorkspace is null ? string.Empty : $"Open recent: {FirstRecentWorkspace.DisplayName}";

    public bool IsPreviewLineWrapEnabled
    {
        get => CurrentDocument.IsPreviewLineWrapEnabled;
        set
        {
            if (CurrentDocument.IsPreviewLineWrapEnabled == value)
                return;

            CurrentDocument.IsPreviewLineWrapEnabled = value;
            _ = PersistPreviewLineWrapPreferenceAsync(value);
        }
    }

    public StatusSeverity FooterSeverity
    {
        get
        {
            StatusSeverity operation = OperationStatus.StatusSeverity;
            StatusSeverity validation = ValidationPane.HighestStatusSeverity;

            if (OperationStatus.IsBusy)
                return operation;

            return (StatusSeverity)Math.Max((int)operation, (int)validation);
        }
    }

    public string FooterStatusMessage
    {
        get
        {
            if (!ValidationPane.HasIssues)
                return OperationStatus.StatusMessage;

            if (string.IsNullOrWhiteSpace(OperationStatus.StatusMessage))
                return ValidationPane.SummaryText;

            return $"{OperationStatus.StatusMessage} ({ValidationPane.SummaryText})";
        }
    }

    public AsyncRelayCommand BuildPreviewCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand CopyPreviewCommand { get; }
    public RelayCommand OpenOutputFolderCommand { get; }
    public RelayCommand TogglePreviewSummaryCommand { get; }

    public RelayCommand NewWorkspaceTabCommand { get; }
    public AsyncRelayCommand SaveWorkspaceCommand { get; }
    public AsyncRelayCommand SaveWorkspaceAsCommand { get; }
    public AsyncRelayCommand LoadWorkspaceCommand { get; }
    public RelayCommand DuplicateWorkspaceTabCommand { get; }
    public AsyncRelayCommand CloseActiveWorkspaceTabCommand { get; }
    public RelayCommand RenameWorkspaceTabCommand { get; }
    public RelayCommand SelectNextWorkspaceTabCommand { get; }
    public RelayCommand SelectPreviousWorkspaceTabCommand { get; }
    public AsyncRelayCommand CloseOtherWorkspaceTabsCommand { get; }

    // ReSharper disable once MemberCanBePrivate.Global
    public AsyncRelayCommand OpenProfilesCommand { get; }
    public AsyncRelayCommand OpenWorkspaceConfigurationCommand { get; }
    public AsyncRelayCommand OpenPreferencesCommand { get; }
    public RelayCommand OpenKeyboardShortcutsCommand { get; }
    public RelayCommand OpenUpdateCheckCommand { get; }
    public AsyncRelayCommand RefreshRecentWorkspacesCommand { get; }

    // ReSharper disable once MemberCanBePrivate.Global
    public AsyncRelayCommand<RecentWorkspaceMenuItemViewModel> OpenRecentWorkspaceCommand { get; }
    public AsyncRelayCommand<RecentWorkspaceMenuItemViewModel> RemoveMissingRecentWorkspaceCommand { get; }
    public AsyncRelayCommand ClearRecentWorkspacesCommand { get; }

    private bool HasStaleBuiltOutput =>
        CurrentDocument.LastOutput is not null &&
        CurrentDocument.PreviewDirtyTracker is { HasAppliedPreview: true, IsPreviewDirty: true };

    public Task<bool> CanCloseAsync()
    {
        return WorkspaceTabs.TryPrepareForApplicationShutdownAsync();
    }

    public string CurrentProfileName => CurrentDocument.CurrentProfileName;

    public string? CurrentProfileEntryId => CurrentDocument.CurrentProfileEntryId;

    public WorkspaceProfileDto CaptureCurrentProfile()
    {
        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        return document.ProfileEditor.CaptureProfile();
    }

    public void ApplyProfileToCurrentSession(string profileName, WorkspaceProfileDto profile, string? profileEntryId)
    {
        ArgumentNullException.ThrowIfNull(profile);

        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        document.ProfileEditor.WorkingProfileName = string.IsNullOrWhiteSpace(profileName)
            ? "Profile"
            : profileName.Trim();

        document.ProfileEditor.ApplyProfile(profile);
        document.CurrentProfileEntryId = profileEntryId;

        if (document.CurrentProfileEntryId is not null)
        {
            document.ProfileOriginEntryId = document.CurrentProfileEntryId;
            document.ProfileOriginDisplayName = document.CurrentProfileName;
        }
        else
        {
            document.ProfileOriginEntryId = null;
            document.ProfileOriginDisplayName = null;
        }

        document.ResetRuntimeState();
        document.OperationStatus.SetStatus("Profile applied to current session.", StatusSeverity.Success);

        RefreshDocumentDirtyState(document);
        RefreshAfterDocumentCommand(document);
    }

    public Task InitializeAsync()
    {
        return RefreshRecentWorkspacesAsync();
    }

    public void ReplaceSelectedFiles(IEnumerable<InputFileItemViewModel> selectedItems)
    {
        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        document.FilesPane.ReplaceSelectedFiles(selectedItems);
    }

    public void ReplaceSelectedSources(IEnumerable<MergeSourceItemViewModel> selectedItems)
    {
        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        document.SourcesPane.ReplaceSelectedSources(selectedItems);
    }

    private async Task BuildPreviewAsync()
    {
        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        await _workspaceDocumentPreviewService.BuildPreviewAsync(document, RaiseTopLevelCommandsCanExecuteChanged);

        RefreshAfterDocumentCommand(document);
    }

    private async Task SaveAsync()
    {
        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        await _workspaceDocumentOutputService.SaveOutputAsync(document, RaiseTopLevelCommandsCanExecuteChanged);

        RefreshAfterDocumentCommand(document);
    }

    private void CopyPreview()
    {
        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        string? content = document.LastOutput?.Content;
        if (string.IsNullOrWhiteSpace(content))
            return;

        _clipboardService.SetText(content);
        document.OperationStatus.SetStatus("Preview copied to clipboard.", StatusSeverity.Success);

        RefreshAfterDocumentCommand(document);
    }

    private void OpenOutputFolder()
    {
        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        _workspaceDocumentOutputService.OpenOutputFolder(document);

        RefreshAfterDocumentCommand(document);
    }

    private void TogglePreviewSummary()
    {
        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        document.TogglePreviewSummary();

        if (IsCurrentDocument(document))
        {
            OnPropertyChanged(nameof(IsPreviewSummaryExpanded));
            OnPropertyChanged(nameof(IsPreviewSummaryCollapsed));
        }
    }

    private bool CanSave()
    {
        return !OperationStatus.IsBusy &&
               CurrentDocument.LastOutput is not null &&
               !string.IsNullOrWhiteSpace(CurrentDocument.SessionSettings.OutputPath);
    }

    private bool CanCopyPreview()
    {
        return !OperationStatus.IsBusy && !string.IsNullOrWhiteSpace(CurrentDocument.LastOutput?.Content);
    }

    private bool CanOpenOutputFolder()
    {
        return !OperationStatus.IsBusy && _workspaceDocumentOutputService.CanOpenOutputFolder(CurrentDocument);
    }

    private async Task SaveWorkspaceAsync()
    {
        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        await _workspaceDocumentLifecycleService.SaveWorkspaceAsync(document, RaiseTopLevelCommandsCanExecuteChanged);

        RefreshAfterDocumentCommand(document);
        await RefreshRecentWorkspacesAsync();
    }

    private async Task SaveWorkspaceAsAsync()
    {
        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        await _workspaceDocumentLifecycleService.SaveWorkspaceAsAsync(document, RaiseTopLevelCommandsCanExecuteChanged);

        RefreshAfterDocumentCommand(document);
        await RefreshRecentWorkspacesAsync();
    }

    private async Task LoadWorkspaceAsync()
    {
        WorkspaceDocumentViewModel beforeOpenDocument = CurrentDocument;

        await WorkspaceTabs.OpenWorkspaceAsync(RaiseTopLevelCommandsCanExecuteChanged);

        WorkspaceDocumentViewModel afterOpenDocument = CurrentDocument;

        if (ReferenceEquals(beforeOpenDocument, afterOpenDocument))
            RefreshAfterDocumentCommand(afterOpenDocument);
        else
            RefreshCurrentDocumentBindings();

        await RefreshRecentWorkspacesAsync();
    }

    private async Task RefreshRecentWorkspacesAsync()
    {
        try
        {
            IReadOnlyCollection<RecentWorkspaceEntry> entries =
                await _recentWorkspacesService.GetRecentWorkspacesAsync();

            RecentWorkspaceMenuItemViewModel[] items =
            [
                .. entries.Select(x => new RecentWorkspaceMenuItemViewModel(x))
            ];

            RecentWorkspaces.Clear();
            foreach (RecentWorkspaceMenuItemViewModel item in items)
                RecentWorkspaces.Add(item);

            MissingRecentWorkspaces.Clear();
            foreach (RecentWorkspaceMenuItemViewModel item in items.Where(x => x.IsMissing))
                MissingRecentWorkspaces.Add(item);

            RaiseRecentWorkspacesChanged();
        }
        catch (Exception ex)
        {
            CurrentDocument.OperationStatus.SetStatus(
                $"Failed to load recent workspaces: {ex.Message}",
                StatusSeverity.Error);

            RefreshAfterDocumentCommand(CurrentDocument);
        }
    }

    private bool CanOpenRecentWorkspace(RecentWorkspaceMenuItemViewModel? item)
    {
        return item is not null && !OperationStatus.IsBusy;
    }

    private async Task OpenRecentWorkspaceAsync(RecentWorkspaceMenuItemViewModel? item)
    {
        if (item is null)
            return;

        if (OperationStatus.IsBusy)
            return;

        if (!File.Exists(item.FilePath))
        {
            await HandleMissingRecentWorkspaceAsync(item);
            return;
        }

        WorkspaceDocumentViewModel beforeOpenDocument = CurrentDocument;

        await WorkspaceTabs.OpenWorkspaceFromPathAsync(item.FilePath, RaiseTopLevelCommandsCanExecuteChanged);

        WorkspaceDocumentViewModel afterOpenDocument = CurrentDocument;

        if (ReferenceEquals(beforeOpenDocument, afterOpenDocument))
            RefreshAfterDocumentCommand(afterOpenDocument);
        else
            RefreshCurrentDocumentBindings();

        await RefreshRecentWorkspacesAsync();
    }

    private async Task HandleMissingRecentWorkspaceAsync(RecentWorkspaceMenuItemViewModel item)
    {
        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        document.OperationStatus.SetStatus("Recent workspace file was not found.", StatusSeverity.Warning);

        bool remove = _userPromptService.Confirm(
            title: "Recent workspace not found",
            message: $"The recent workspace file was not found:{Environment.NewLine}" +
                     item.FilePath +
                     Environment.NewLine +
                     Environment.NewLine +
                     "Remove it from recent workspaces?");

        if (remove)
        {
            await _recentWorkspacesService.RemoveAsync(item.FilePath);

            document.OperationStatus.SetStatus("Removed missing recent workspace.", StatusSeverity.Success);
        }

        RefreshAfterDocumentCommand(document);
        await RefreshRecentWorkspacesAsync();
    }

    private bool CanRemoveMissingRecentWorkspace(RecentWorkspaceMenuItemViewModel? item)
    {
        return item is not null && item.IsMissing && !OperationStatus.IsBusy;
    }

    private async Task RemoveMissingRecentWorkspaceAsync(RecentWorkspaceMenuItemViewModel? item)
    {
        if (item is null)
            return;

        if (!item.IsMissing)
            return;

        await _recentWorkspacesService.RemoveAsync(item.FilePath);

        CurrentDocument.OperationStatus.SetStatus("Removed missing recent workspace.", StatusSeverity.Success);

        RefreshAfterDocumentCommand(CurrentDocument);
        await RefreshRecentWorkspacesAsync();
    }

    private async Task ClearRecentWorkspacesAsync()
    {
        if (!HasRecentWorkspaces)
            return;

        bool confirmed = _userPromptService.Confirm(
            title: "Clear recent workspaces",
            message: "Clear all recent workspaces from the list?");

        if (!confirmed)
            return;

        await _recentWorkspacesService.ClearAsync();

        CurrentDocument.OperationStatus.SetStatus("Recent workspaces cleared.", StatusSeverity.Success);

        RefreshAfterDocumentCommand(CurrentDocument);
        await RefreshRecentWorkspacesAsync();
    }

    private void RaiseRecentWorkspacesChanged()
    {
        OnPropertyChanged(nameof(HasRecentWorkspaces));
        OnPropertyChanged(nameof(HasNoRecentWorkspaces));
        OnPropertyChanged(nameof(HasMissingRecentWorkspaces));
        RefreshFirstWorkspaceGuideBindings();

        RefreshRecentWorkspacesCommand.RaiseCanExecuteChanged();
        OpenRecentWorkspaceCommand.RaiseCanExecuteChanged();
        RemoveMissingRecentWorkspaceCommand.RaiseCanExecuteChanged();
        ClearRecentWorkspacesCommand.RaiseCanExecuteChanged();
    }

    private void OpenKeyboardShortcuts()
    {
        _keyboardShortcutsDialogService.ShowDialog();
    }

    private void OpenUpdateCheck()
    {
        _updateCheckDialogService.ShowDialog();
    }

    private async Task PersistPreviewLineWrapPreferenceAsync(bool isEnabled)
    {
        try
        {
            await _applicationPreferencesStore.UpdateAsync(current => new ApplicationPreferences(
                isPreviewLineWrapEnabledByDefault: isEnabled,
                previewDisplayCharacterLimit: current.PreviewDisplayCharacterLimit,
                crashLogRetentionLimit: current.CrashLogRetentionLimit));
        }
        catch (Exception ex)
        {
            CurrentDocument.OperationStatus.SetStatus(
                $"Failed to save preview display preferences: {ex.Message}",
                StatusSeverity.Error);
        }
    }

    private async Task OpenProfilesAsync()
    {
        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        try
        {
            document.OperationStatus.IsBusy = true;
            document.OperationStatus.ShowIndeterminateProgress("Opening profile library.");
            RefreshAfterDocumentCommand(document);

            await _profileManagerWindowService.ShowDialogAsync();
        }
        catch (Exception ex)
        {
            document.OperationStatus.SetStatus($"Failed to open profile library: {ex.Message}", StatusSeverity.Error);
        }
        finally
        {
            document.OperationStatus.HideProgress();
            document.OperationStatus.IsBusy = false;
            RefreshAfterDocumentCommand(document);
        }
    }

    private async Task OpenWorkspaceConfigurationAsync()
    {
        WorkspaceDocumentViewModel document = GetCommandTargetDocument();

        bool applied = await _workspaceConfigurationDialogService.ShowDialogAsync(document);

        if (applied)
            RefreshAfterDocumentCommand(document);
        else
            RaiseTopLevelCommandsCanExecuteChanged();
    }

    private async Task OpenPreferencesAsync()
    {
        bool saved = await _preferencesDialogService.ShowDialogAsync();

        if (saved)
        {
            CurrentDocument.OperationStatus.SetStatus("Preferences saved.", StatusSeverity.Success);
        }

        RaiseTopLevelCommandsCanExecuteChanged();
    }

    private void NewWorkspaceTab()
    {
        WorkspaceTabs.CreateNewTab();
        RefreshCurrentDocumentBindings();
    }

    private void DuplicateWorkspaceTab()
    {
        if (!WorkspaceTabs.CanDuplicateTab(WorkspaceTabs.ActiveTab))
            return;

        WorkspaceTabs.DuplicateActiveTab();
        RefreshCurrentDocumentBindings();
    }

    private async Task CloseActiveWorkspaceTabAsync()
    {
        await CloseWorkspaceTabAsync(WorkspaceTabs.ActiveTab);
    }

    private async Task CloseOtherWorkspaceTabsAsync()
    {
        if (!WorkspaceTabs.CanCloseOtherTabs())
        {
            RaiseTopLevelCommandsCanExecuteChanged();
            return;
        }

        int tabCountBeforeClose = WorkspaceTabs.Tabs.Count;

        bool closedAll = await WorkspaceTabs.TryCloseOtherTabsAsync();
        bool changedTabCollection = WorkspaceTabs.Tabs.Count != tabCountBeforeClose;

        if (closedAll || changedTabCollection)
            RefreshCurrentDocumentBindings();
        else
            RaiseTopLevelCommandsCanExecuteChanged();
    }

    private void RenameWorkspaceTab()
    {
        WorkspaceTabViewModel tab = WorkspaceTabs.ActiveTab;

        if (!WorkspaceTabs.CanRenameTab(tab))
            return;

        string? requestedName = _workspaceTabRenameDialogService.RequestRename(tab);
        if (string.IsNullOrWhiteSpace(requestedName))
            return;

        string normalizedName = requestedName.Trim();

        if (string.Equals(normalizedName, tab.Title, StringComparison.Ordinal))
            return;

        RenameCurrentWorkspaceTab(normalizedName);
    }

    public bool RenameCurrentWorkspaceTab(string newName)
    {
        if (!WorkspaceTabs.CanRenameTab(WorkspaceTabs.ActiveTab))
            return false;

        bool renamed = WorkspaceTabs.RenameActiveTab(newName);
        if (!renamed)
            return false;

        RefreshCurrentDocumentBindings();

        return true;
    }

    private void SelectNextWorkspaceTab()
    {
        if (!WorkspaceTabs.SelectNextTab())
            RaiseTopLevelCommandsCanExecuteChanged();
    }

    private void SelectPreviousWorkspaceTab()
    {
        if (!WorkspaceTabs.SelectPreviousTab())
            RaiseTopLevelCommandsCanExecuteChanged();
    }

    public void SelectWorkspaceTab(WorkspaceTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);

        if (ReferenceEquals(WorkspaceTabs.ActiveTab, tab))
            return;

        if (!WorkspaceTabs.Tabs.Contains(tab))
            return;

        WorkspaceTabs.SelectTab(tab);
    }

    public async Task<bool> CloseWorkspaceTabAsync(WorkspaceTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);

        if (!WorkspaceTabs.CanCloseTab(tab))
        {
            RaiseTopLevelCommandsCanExecuteChanged();
            return false;
        }

        bool closed = await WorkspaceTabs.TryCloseTabAsync(tab);

        if (closed)
            RefreshCurrentDocumentBindings();
        else
            RaiseTopLevelCommandsCanExecuteChanged();

        return closed;
    }

    private void RaiseTopLevelCommandsCanExecuteChanged()
    {
        BuildPreviewCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
        CopyPreviewCommand.RaiseCanExecuteChanged();
        OpenOutputFolderCommand.RaiseCanExecuteChanged();

        NewWorkspaceTabCommand.RaiseCanExecuteChanged();
        SaveWorkspaceCommand.RaiseCanExecuteChanged();
        SaveWorkspaceAsCommand.RaiseCanExecuteChanged();
        LoadWorkspaceCommand.RaiseCanExecuteChanged();
        DuplicateWorkspaceTabCommand.RaiseCanExecuteChanged();
        CloseActiveWorkspaceTabCommand.RaiseCanExecuteChanged();
        RenameWorkspaceTabCommand.RaiseCanExecuteChanged();
        SelectNextWorkspaceTabCommand.RaiseCanExecuteChanged();
        SelectPreviousWorkspaceTabCommand.RaiseCanExecuteChanged();
        CloseOtherWorkspaceTabsCommand.RaiseCanExecuteChanged();
        OpenProfilesCommand.RaiseCanExecuteChanged();
        OpenWorkspaceConfigurationCommand.RaiseCanExecuteChanged();
        OpenPreferencesCommand.RaiseCanExecuteChanged();

        RefreshRecentWorkspacesCommand.RaiseCanExecuteChanged();
        OpenRecentWorkspaceCommand.RaiseCanExecuteChanged();
        RemoveMissingRecentWorkspaceCommand.RaiseCanExecuteChanged();
        ClearRecentWorkspacesCommand.RaiseCanExecuteChanged();
    }

    private void EditorState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        WorkspaceDocumentViewModel document = CurrentDocument;

        RefreshDocumentDirtyState(document);

        OnPropertyChanged(nameof(CurrentProfileName));
        RefreshFirstWorkspaceGuideBindings();
        RefreshCurrentProfileCard();
        RaiseTopLevelCommandsCanExecuteChanged();
    }

    private void SourcesPane_SourcesChanged(object? sender, EventArgs e)
    {
        WorkspaceDocumentViewModel document = CurrentDocument;

        RefreshDocumentDirtyState(document);
        RefreshEmptyStateBindings();
    }

    private void FilesPane_FileOverridesChanged(object? sender, EventArgs e)
    {
        WorkspaceDocumentViewModel document = CurrentDocument;

        RefreshDocumentDirtyState(document);
        RefreshEmptyStateBindings();
    }

    private void ValidationPane_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshEmptyStateBindings();
        RefreshFooterState();
    }

    private void OperationStatus_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OperationStatusViewModel.IsBusy))
        {
            RaiseTopLevelCommandsCanExecuteChanged();
            RefreshEmptyStateBindings();
        }

        RefreshFooterState();
    }

    private void PreviewDirtyTracker_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PreviewDirtyStateTracker.IsPreviewDirty)
            or nameof(PreviewDirtyStateTracker.HasAppliedPreview))
        {
            RefreshSaveOutputActionBindings();
        }
    }

    private void CurrentDocument_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.PreviewContent))
            OnPropertyChanged(nameof(PreviewContent));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.PreviewNotice))
            OnPropertyChanged(nameof(PreviewNotice));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.HasPreviewNotice))
            OnPropertyChanged(nameof(HasPreviewNotice));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.PreviewCharacterCountText))
            OnPropertyChanged(nameof(PreviewCharacterCountText));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.PreviewSummary))
            OnPropertyChanged(nameof(PreviewSummary));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.HasPreviewSummary))
            OnPropertyChanged(nameof(HasPreviewSummary));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.IsPreviewSummaryExpanded))
            OnPropertyChanged(nameof(IsPreviewSummaryExpanded));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.IsPreviewSummaryCollapsed))
            OnPropertyChanged(nameof(IsPreviewSummaryCollapsed));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.IsPreviewLineWrapEnabled))
            OnPropertyChanged(nameof(IsPreviewLineWrapEnabled));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.CurrentProfileEntryId))
        {
            OnPropertyChanged(nameof(CurrentProfileEntryId));
            RefreshCurrentProfileCard();
        }

        if (e.PropertyName is nameof(WorkspaceDocumentViewModel.ProfileOriginEntryId)
            or nameof(WorkspaceDocumentViewModel.ProfileOriginDisplayName))
        {
            RefreshCurrentProfileCard();
        }

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.IsWorkspaceDirty))
            OnPropertyChanged(nameof(WorkspaceDirtyTracker));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.LastOutput))
        {
            RaiseTopLevelCommandsCanExecuteChanged();
            RefreshSaveOutputActionBindings();
        }

        if (e.PropertyName is nameof(WorkspaceDocumentViewModel.PreviewContent)
            or nameof(WorkspaceDocumentViewModel.LastOutput)
            or nameof(WorkspaceDocumentViewModel.PreviewNotice)
            or nameof(WorkspaceDocumentViewModel.HasPreviewNotice)
            or nameof(WorkspaceDocumentViewModel.PreviewSummary)
            or nameof(WorkspaceDocumentViewModel.HasPreviewSummary)
            or nameof(WorkspaceDocumentViewModel.PreviewCharacterCountText))
        {
            RefreshEmptyStateBindings();
        }

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.ShowDiscoveredFilesEmptyState))
            OnPropertyChanged(nameof(ShowDiscoveredFilesEmptyState));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.DiscoveredFilesEmptyTitle))
            OnPropertyChanged(nameof(DiscoveredFilesEmptyTitle));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.DiscoveredFilesEmptyDescription))
            OnPropertyChanged(nameof(DiscoveredFilesEmptyDescription));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.ShowPreviewEmptyState))
            OnPropertyChanged(nameof(ShowPreviewEmptyState));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.PreviewEmptyTitle))
            OnPropertyChanged(nameof(PreviewEmptyTitle));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.PreviewEmptyDescription))
            OnPropertyChanged(nameof(PreviewEmptyDescription));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.ShowPreviewEmptyBuildAction))
            OnPropertyChanged(nameof(ShowPreviewEmptyBuildAction));

        if (e.PropertyName == nameof(WorkspaceDocumentViewModel.IsEmptyWorkspace))
            RefreshFirstWorkspaceGuideBindings();
    }

    private void WorkspaceTabs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (WorkspaceTabViewModel tab in e.OldItems)
                tab.PropertyChanged -= WorkspaceTab_PropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (WorkspaceTabViewModel tab in e.NewItems)
                tab.PropertyChanged += WorkspaceTab_PropertyChanged;
        }

        OnPropertyChanged(nameof(HasMultipleWorkspaceTabs));
        RaiseTopLevelCommandsCanExecuteChanged();
    }

    private void WorkspaceTab_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WorkspaceTabViewModel.IsBusy))
            return;

        RaiseTopLevelCommandsCanExecuteChanged();
    }

    private void WorkspaceTabs_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WorkspaceTabManagerViewModel.ActiveDocument))
            return;

        SubscribeToDocument(CurrentDocument);
        RefreshCurrentDocumentBindings();
    }

    private void RefreshFooterState()
    {
        OnPropertyChanged(nameof(FooterSeverity));
        OnPropertyChanged(nameof(FooterStatusMessage));
    }

    private void RefreshCurrentProfileCard()
    {
        CurrentProfileCard.Apply(
            profileName: CurrentProfileName,
            profileEntryId: CurrentProfileEntryId,
            profileOriginEntryId: CurrentDocument.ProfileOriginEntryId,
            profileOriginDisplayName: CurrentDocument.ProfileOriginDisplayName,
            profile: CaptureCurrentProfile());
    }

    private WorkspaceDocumentViewModel GetCommandTargetDocument()
    {
        return CurrentDocument;
    }

    private bool IsCurrentDocument(WorkspaceDocumentViewModel document)
    {
        return ReferenceEquals(CurrentDocument, document);
    }

    private void RefreshAfterDocumentCommand(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!IsCurrentDocument(document))
        {
            RaiseTopLevelCommandsCanExecuteChanged();
            return;
        }

        RefreshCurrentProfileCard();
        RefreshFooterState();
        RefreshEmptyStateBindings();
        RaiseTopLevelCommandsCanExecuteChanged();
    }

    private void RefreshDocumentDirtyState(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _workspaceDocumentDirtyStateService.RefreshWorkspaceDirtyState(document);
        _workspaceDocumentDirtyStateService.RefreshPreviewDirtyState(document);
    }

    private void RefreshCurrentDocumentBindings()
    {
        OnPropertyChanged(nameof(CurrentDocument));
        OnPropertyChanged(nameof(HasMultipleWorkspaceTabs));
        OnPropertyChanged(nameof(SessionSettings));
        OnPropertyChanged(nameof(ProfileEditor));
        OnPropertyChanged(nameof(SourcesPane));
        OnPropertyChanged(nameof(FilesPane));
        OnPropertyChanged(nameof(OperationStatus));
        OnPropertyChanged(nameof(PreviewDirtyTracker));
        OnPropertyChanged(nameof(WorkspaceDirtyTracker));
        OnPropertyChanged(nameof(ValidationPane));

        OnPropertyChanged(nameof(PreviewContent));
        OnPropertyChanged(nameof(PreviewNotice));
        OnPropertyChanged(nameof(PreviewCharacterCountText));
        OnPropertyChanged(nameof(HasPreviewNotice));
        OnPropertyChanged(nameof(IsPreviewLineWrapEnabled));
        OnPropertyChanged(nameof(PreviewSummary));
        OnPropertyChanged(nameof(HasPreviewSummary));
        OnPropertyChanged(nameof(IsPreviewSummaryExpanded));
        OnPropertyChanged(nameof(IsPreviewSummaryCollapsed));
        RefreshSaveOutputActionBindings();

        OnPropertyChanged(nameof(CurrentProfileName));
        OnPropertyChanged(nameof(CurrentProfileEntryId));

        RefreshEmptyStateBindings();
        RefreshCurrentProfileCard();
        RefreshFooterState();
        RaiseTopLevelCommandsCanExecuteChanged();
    }

    private void RefreshSaveOutputActionBindings()
    {
        OnPropertyChanged(nameof(SaveOutputActionText));
        OnPropertyChanged(nameof(SaveOutputActionTooltip));
    }

    private void RefreshEmptyStateBindings()
    {
        OnPropertyChanged(nameof(ShowDiscoveredFilesEmptyState));
        OnPropertyChanged(nameof(DiscoveredFilesEmptyTitle));
        OnPropertyChanged(nameof(DiscoveredFilesEmptyDescription));

        OnPropertyChanged(nameof(ShowPreviewEmptyState));
        OnPropertyChanged(nameof(PreviewEmptyTitle));
        OnPropertyChanged(nameof(PreviewEmptyDescription));
        OnPropertyChanged(nameof(ShowPreviewEmptyBuildAction));

        RefreshFirstWorkspaceGuideBindings();
    }

    private void RefreshFirstWorkspaceGuideBindings()
    {
        OnPropertyChanged(nameof(ShowFirstWorkspaceGuide));
        OnPropertyChanged(nameof(ShowRegularPreviewEmptyState));
        OnPropertyChanged(nameof(FirstWorkspaceGuideTitle));
        OnPropertyChanged(nameof(FirstWorkspaceGuideDescription));
        OnPropertyChanged(nameof(FirstWorkspaceProfileHint));
        OnPropertyChanged(nameof(FirstRecentWorkspace));
        OnPropertyChanged(nameof(HasFirstRecentWorkspace));
        OnPropertyChanged(nameof(FirstRecentWorkspaceText));
    }

    private void SubscribeToDocument(WorkspaceDocumentViewModel document)
    {
        if (ReferenceEquals(_subscribedDocument, document))
            return;

        if (_subscribedDocument is not null)
            UnsubscribeFromDocument(_subscribedDocument);

        document.PropertyChanged += CurrentDocument_PropertyChanged;

        document.SessionSettings.PropertyChanged += EditorState_PropertyChanged;
        document.ProfileEditor.PropertyChanged += EditorState_PropertyChanged;
        document.SourcesPane.SourcesChanged += SourcesPane_SourcesChanged;
        document.FilesPane.FileOverridesChanged += FilesPane_FileOverridesChanged;
        document.ValidationPane.PropertyChanged += ValidationPane_PropertyChanged;
        document.OperationStatus.PropertyChanged += OperationStatus_PropertyChanged;
        document.PreviewDirtyTracker.PropertyChanged += PreviewDirtyTracker_PropertyChanged;

        _subscribedDocument = document;
    }

    private void UnsubscribeFromDocument(WorkspaceDocumentViewModel document)
    {
        document.PropertyChanged -= CurrentDocument_PropertyChanged;

        document.SessionSettings.PropertyChanged -= EditorState_PropertyChanged;
        document.ProfileEditor.PropertyChanged -= EditorState_PropertyChanged;
        document.SourcesPane.SourcesChanged -= SourcesPane_SourcesChanged;
        document.FilesPane.FileOverridesChanged -= FilesPane_FileOverridesChanged;
        document.ValidationPane.PropertyChanged -= ValidationPane_PropertyChanged;
        document.OperationStatus.PropertyChanged -= OperationStatus_PropertyChanged;
        document.PreviewDirtyTracker.PropertyChanged -= PreviewDirtyTracker_PropertyChanged;
    }
}