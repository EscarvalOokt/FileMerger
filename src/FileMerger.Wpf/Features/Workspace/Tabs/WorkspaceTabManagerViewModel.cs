using System.Collections.ObjectModel;
using FileMerger.Wpf.Features.Workspace.State;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Workspace.Tabs;

public sealed class WorkspaceTabManagerViewModel : ViewModelBase
{
    private readonly IWorkspaceDocumentFactory _workspaceDocumentFactory;
    private readonly IWorkspaceDocumentLifecycleService? _workspaceDocumentLifecycleService;
    private readonly IUserPromptService? _userPromptService;
    private readonly IWorkspaceDocumentCloneService? _workspaceDocumentCloneService;
    private readonly ObservableCollection<WorkspaceTabViewModel> _tabs = [];

    private WorkspaceTabViewModel? _activeTab;

    public WorkspaceTabManagerViewModel(
        IWorkspaceDocumentFactory workspaceDocumentFactory,
        IWorkspaceDocumentLifecycleService? workspaceDocumentLifecycleService = null,
        IUserPromptService? userPromptService = null,
        IWorkspaceDocumentCloneService? workspaceDocumentCloneService = null)
    {
        ArgumentNullException.ThrowIfNull(workspaceDocumentFactory);

        _workspaceDocumentFactory = workspaceDocumentFactory;
        _workspaceDocumentLifecycleService = workspaceDocumentLifecycleService;
        _userPromptService = userPromptService;
        _workspaceDocumentCloneService = workspaceDocumentCloneService;

        Tabs = new ReadOnlyObservableCollection<WorkspaceTabViewModel>(_tabs);

        CreateNewTab();
    }

    public ReadOnlyObservableCollection<WorkspaceTabViewModel> Tabs { get; }

    public WorkspaceTabViewModel ActiveTab =>
        _activeTab ?? throw new InvalidOperationException("Workspace tab manager has no active tab.");

    public WorkspaceDocumentViewModel ActiveDocument => ActiveTab.Document;

    public WorkspaceTabViewModel CreateNewTab()
    {
        WorkspaceDocumentViewModel document = _workspaceDocumentFactory.CreateDefaultDocument();
        WorkspaceTabViewModel tab = new(document);

        _tabs.Add(tab);
        SelectTab(tab);

        return tab;
    }

    public WorkspaceTabViewModel DuplicateActiveTab()
    {
        return DuplicateTab(ActiveTab);
    }

    public WorkspaceTabViewModel DuplicateTab(WorkspaceTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        EnsureTabBelongsToManager(tab);

        if (IsTabBusy(tab))
            throw new InvalidOperationException("Busy workspace tab cannot be duplicated.");

        if (_workspaceDocumentCloneService is null)
            throw new InvalidOperationException("Duplicate workspace flow requires clone service.");

        int sourceIndex = _tabs.IndexOf(tab);
        string duplicateName = BuildUniqueDuplicateName(tab);

        WorkspaceDocumentViewModel duplicateDocument =
            _workspaceDocumentCloneService.CloneAsDuplicate(tab.Document, duplicateName);

        WorkspaceTabViewModel duplicateTab = new(duplicateDocument);

        _tabs.Insert(sourceIndex + 1, duplicateTab);
        SelectTab(duplicateTab);

        return duplicateTab;
    }

    public bool RenameActiveTab(string newName)
    {
        return RenameTab(ActiveTab, newName);
    }

    public bool RenameTab(
        WorkspaceTabViewModel tab,
        string newName)
    {
        ArgumentNullException.ThrowIfNull(tab);
        EnsureTabBelongsToManager(tab);

        if (IsTabBusy(tab))
            return false;

        if (string.IsNullOrWhiteSpace(newName))
            return false;

        tab.Document.SessionSettings.SessionName = newName.Trim();

        tab.Document.WorkspaceDirtyTracker.Refresh(
            WorkspaceDocumentStateSnapshotFactory.Capture(tab.Document));

        return true;
    }

    public void SelectTab(WorkspaceTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        EnsureTabBelongsToManager(tab);

        SetActiveTab(tab);
    }

    public bool CanSelectNextTab()
    {
        return _tabs.Count > 1;
    }

    public bool CanSelectPreviousTab()
    {
        return _tabs.Count > 1;
    }

    public bool SelectNextTab()
    {
        return SelectRelativeTab(offset: 1);
    }

    public bool SelectPreviousTab()
    {
        return SelectRelativeTab(offset: -1);
    }

    public bool CanDuplicateTab(WorkspaceTabViewModel? tab)
    {
        return tab is not null &&
               _tabs.Contains(tab) &&
               !IsTabBusy(tab) &&
               _workspaceDocumentCloneService is not null;
    }

    public bool CanRenameTab(WorkspaceTabViewModel? tab)
    {
        return tab is not null &&
               _tabs.Contains(tab) &&
               !IsTabBusy(tab);
    }

    public bool CanCloseTab(WorkspaceTabViewModel? tab)
    {
        return tab is not null &&
               _tabs.Contains(tab) &&
               _tabs.Count > 1 &&
               !IsTabBusy(tab);
    }

    public bool CanCloseOtherTabs()
    {
        if (_tabs.Count <= 1)
            return false;

        if (IsTabBusy(ActiveTab))
            return false;

        return _tabs
            .Where(tab => !ReferenceEquals(tab, ActiveTab))
            .All(tab => !IsTabBusy(tab));
    }

    public async Task<bool> TryCloseOtherTabsAsync()
    {
        if (!CanCloseOtherTabs())
            return false;

        WorkspaceTabViewModel activeTab = ActiveTab;

        WorkspaceTabViewModel[] tabsToClose =
        [
            .. _tabs.Where(tab => !ReferenceEquals(tab, activeTab))
        ];

        foreach (WorkspaceTabViewModel tab in tabsToClose)
        {
            if (!_tabs.Contains(tab))
                continue;

            bool closed = await TryCloseTabAsync(tab);

            if (!closed)
            {
                if (_tabs.Contains(activeTab))
                    SelectTab(activeTab);

                return false;
            }
        }

        if (_tabs.Contains(activeTab))
            SelectTab(activeTab);

        return true;
    }

    public bool CloseTab(WorkspaceTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        EnsureTabBelongsToManager(tab);

        if (!CanCloseTab(tab))
            return false;

        int closingIndex = _tabs.IndexOf(tab);
        bool wasActive = ReferenceEquals(tab, _activeTab);

        _tabs.RemoveAt(closingIndex);
        tab.IsActive = false;

        if (wasActive)
        {
            int nextIndex = Math.Min(closingIndex, _tabs.Count - 1);
            SetActiveTab(_tabs[nextIndex]);
        }

        return true;
    }

    public Task<bool> OpenWorkspaceAsync(
        Action? stateChanged = null,
        CancellationToken cancellationToken = default)
    {
        if (_workspaceDocumentLifecycleService is null)
            throw new InvalidOperationException("Open workspace flow requires lifecycle service.");

        return OpenWorkspaceCoreAsync(
            (document, token) => _workspaceDocumentLifecycleService.LoadWorkspaceAsync(
                document,
                stateChanged,
                token),
            cancellationToken);
    }

    public Task<bool> OpenWorkspaceFromPathAsync(
        string workspaceFilePath,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default)
    {
        if (_workspaceDocumentLifecycleService is null)
            throw new InvalidOperationException("Open workspace flow requires lifecycle service.");

        if (string.IsNullOrWhiteSpace(workspaceFilePath))
            throw new ArgumentException("Workspace file path cannot be empty.", nameof(workspaceFilePath));

        return OpenWorkspaceCoreAsync(
            (document, token) => _workspaceDocumentLifecycleService.LoadWorkspaceFromPathAsync(
                document,
                workspaceFilePath,
                stateChanged,
                token),
            cancellationToken);
    }

    private async Task<bool> OpenWorkspaceCoreAsync(
        Func<WorkspaceDocumentViewModel, CancellationToken, Task<bool>> loadAsync,
        CancellationToken cancellationToken)
    {
        WorkspaceTabViewModel previousActiveTab = ActiveTab;

        bool replaceActiveTab = CanReplaceWithOpenedWorkspace(previousActiveTab);
        WorkspaceTabViewModel targetTab = replaceActiveTab
            ? previousActiveTab
            : CreateNewTab();

        bool loaded = false;

        try
        {
            loaded = await loadAsync(targetTab.Document, cancellationToken);

            return loaded;
        }
        finally
        {
            if (!loaded && !replaceActiveTab && _tabs.Contains(targetTab))
            {
                CloseTab(targetTab);

                if (_tabs.Contains(previousActiveTab))
                    SelectTab(previousActiveTab);
            }
        }
    }

    public async Task<bool> TryCloseTabAsync(WorkspaceTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        EnsureTabBelongsToManager(tab);

        if (!CanCloseTab(tab))
            return false;

        if (!tab.IsWorkspaceDirty)
            return CloseTab(tab);

        if (_workspaceDocumentLifecycleService is null || _userPromptService is null)
            throw new InvalidOperationException("Dirty workspace close flow requires lifecycle and prompt services.");

        UnsavedChangesDecision decision = _userPromptService.ConfirmUnsavedChanges(
            title: "Unsaved workspace",
            message:
            $"Workspace tab \"{tab.Title}\" has unsaved changes.{Environment.NewLine}" +
            "Save changes before closing this tab?");

        if (decision == UnsavedChangesDecision.Cancel)
            return false;

        if (decision == UnsavedChangesDecision.Discard)
            return CloseTab(tab);

        bool saved = await _workspaceDocumentLifecycleService.SaveWorkspaceAsync(tab.Document);
        if (!saved)
            return false;

        if (tab.IsWorkspaceDirty)
            return false;

        return CloseTab(tab);
    }

    private bool SelectRelativeTab(int offset)
    {
        if (_tabs.Count <= 1)
            return false;

        int activeIndex = _tabs.IndexOf(ActiveTab);
        if (activeIndex < 0)
            return false;

        int nextIndex = (activeIndex + offset + _tabs.Count) % _tabs.Count;

        SetActiveTab(_tabs[nextIndex]);

        return true;
    }

    private static bool IsTabBusy(WorkspaceTabViewModel tab)
    {
        return tab.Document.OperationStatus.IsBusy;
    }

    private static bool CanReplaceWithOpenedWorkspace(WorkspaceTabViewModel tab)
    {
        WorkspaceDocumentViewModel document = tab.Document;

        return !document.IsWorkspaceDirty &&
               string.IsNullOrWhiteSpace(document.WorkspaceFilePath) &&
               document.SourcesPane.Sources.Count == 0 &&
               document.FilesPane.Files.Count == 0 &&
               document.LastOutput is null &&
               !document.PreviewDirtyTracker.HasAppliedPreview;
    }

    private string BuildUniqueDuplicateName(WorkspaceTabViewModel sourceTab)
    {
        string sourceTitle = NormalizeTabTitle(sourceTab.Title);
        string baseName = $"{sourceTitle} Copy";

        if (!TabTitleExists(baseName))
            return baseName;

        for (int index = 2;; index++)
        {
            string candidate = $"{baseName} {index}";
            if (!TabTitleExists(candidate))
                return candidate;
        }
    }

    private bool TabTitleExists(string title)
    {
        return _tabs.Any(tab =>
            string.Equals(
                NormalizeTabTitle(tab.Title),
                NormalizeTabTitle(title),
                StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeTabTitle(string? title)
    {
        return string.IsNullOrWhiteSpace(title)
            ? "Untitled Workspace"
            : title.Trim();
    }

    private void SetActiveTab(WorkspaceTabViewModel tab)
    {
        if (ReferenceEquals(_activeTab, tab))
            return;

        _activeTab?.IsActive = false;

        _activeTab = tab;
        _activeTab.IsActive = true;

        OnPropertyChanged(nameof(ActiveTab));
        OnPropertyChanged(nameof(ActiveDocument));
    }

    private void EnsureTabBelongsToManager(WorkspaceTabViewModel tab)
    {
        if (!_tabs.Contains(tab))
            throw new InvalidOperationException("Tab does not belong to this manager.");
    }
}