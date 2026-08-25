using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.Recent;
using FileMerger.Wpf.Features.Workspace.Tabs;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shell.Main;

namespace FileMerger.Tests.Wpf.Shell.Main;

public sealed class MainViewModelWorkspaceTabWorkflowSmokeTests
{
    [Fact]
    public async Task WorkspaceTabsSmoke_Should_Create_Switch_Duplicate_Rename_And_Close_Tabs()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;

        context.ViewModel.NewWorkspaceTabCommand.Execute(null);

        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.ActiveTab;

        Assert.Equal(2, context.WorkspaceTabs.Tabs.Count);
        Assert.Same(secondTab.Document, context.ViewModel.CurrentDocument);
        Assert.True(context.ViewModel.HasMultipleWorkspaceTabs);

        context.RenameDialogService.NextName = "Renamed Workspace";

        context.ViewModel.RenameWorkspaceTabCommand.Execute(null);

        Assert.Equal("Renamed Workspace", secondTab.Title);
        Assert.True(secondTab.Document.IsWorkspaceDirty);

        context.ViewModel.DuplicateWorkspaceTabCommand.Execute(null);

        WorkspaceTabViewModel duplicateTab = context.WorkspaceTabs.ActiveTab;

        Assert.Equal(3, context.WorkspaceTabs.Tabs.Count);
        Assert.NotSame(secondTab.Document, duplicateTab.Document);
        Assert.Equal("Renamed Workspace Copy", duplicateTab.Title);
        Assert.Null(duplicateTab.Document.LastOutput);
        Assert.Null(duplicateTab.Document.WorkspaceFilePath);

        context.ViewModel.SelectPreviousWorkspaceTabCommand.Execute(null);

        Assert.Same(secondTab, context.WorkspaceTabs.ActiveTab);
        Assert.Same(secondTab.Document, context.ViewModel.CurrentDocument);

        context.ViewModel.SelectNextWorkspaceTabCommand.Execute(null);

        Assert.Same(duplicateTab, context.WorkspaceTabs.ActiveTab);
        Assert.Same(duplicateTab.Document, context.ViewModel.CurrentDocument);

        MarkDocumentSaved(duplicateTab.Document);

        bool closed = await context.ViewModel.CloseWorkspaceTabAsync(duplicateTab);

        Assert.True(closed);
        Assert.Equal(2, context.WorkspaceTabs.Tabs.Count);
        Assert.DoesNotContain(duplicateTab, context.WorkspaceTabs.Tabs);
        Assert.Contains(firstTab, context.WorkspaceTabs.Tabs);
        Assert.Contains(secondTab, context.WorkspaceTabs.Tabs);
    }

    [Theory]
    [InlineData(UnsavedChangesDecision.Cancel, false, false, 2)]
    [InlineData(UnsavedChangesDecision.Discard, true, false, 1)]
    [InlineData(UnsavedChangesDecision.Save, true, true, 1)]
    public async Task CloseDirtyTabSmoke_Should_Follow_Unsaved_Decision(
        UnsavedChangesDecision decision,
        bool expectedClosed,
        bool expectedSaveCall,
        int expectedTabCount)
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel cleanTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel dirtyTab = context.WorkspaceTabs.CreateNewTab();

        dirtyTab.Document.SessionSettings.SessionName = $"Dirty {decision}";
        MarkWorkspaceDirty(dirtyTab.Document);

        context.WorkspaceTabs.SelectTab(cleanTab);

        context.PromptService.EnqueueUnsavedChangesDecision(decision);
        context.LifecycleService.OnSave = MarkDocumentSaved;

        bool closed = await context.ViewModel.CloseWorkspaceTabAsync(dirtyTab);

        Assert.Equal(expectedClosed, closed);
        Assert.Equal(expectedTabCount, context.WorkspaceTabs.Tabs.Count);
        Assert.Equal(expectedSaveCall ? 1 : 0, context.LifecycleService.SaveWorkspaceCalls);
        Assert.Equal(1, context.PromptService.ConfirmUnsavedChangesCalls);

        if (expectedClosed)
            Assert.DoesNotContain(dirtyTab, context.WorkspaceTabs.Tabs);
        else
            Assert.Contains(dirtyTab, context.WorkspaceTabs.Tabs);

        Assert.Same(cleanTab, context.WorkspaceTabs.ActiveTab);
    }

    [Fact]
    public void CloseOtherTabsSmoke_Should_Close_Inactive_Tabs_And_Respect_Dirty_Decisions()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.ActiveTab;
        activeTab.Document.SessionSettings.SessionName = "Active Workspace";

        WorkspaceTabViewModel cleanInactiveTab = context.WorkspaceTabs.CreateNewTab();
        cleanInactiveTab.Document.SessionSettings.SessionName = "Clean Inactive Workspace";
        MarkDocumentSaved(cleanInactiveTab.Document);

        WorkspaceTabViewModel dirtySaveTab = context.WorkspaceTabs.CreateNewTab();
        dirtySaveTab.Document.SessionSettings.SessionName = "Dirty Save Workspace";
        MarkWorkspaceDirty(dirtySaveTab.Document);

        WorkspaceTabViewModel dirtyDiscardTab = context.WorkspaceTabs.CreateNewTab();
        dirtyDiscardTab.Document.SessionSettings.SessionName = "Dirty Discard Workspace";
        MarkWorkspaceDirty(dirtyDiscardTab.Document);

        context.WorkspaceTabs.SelectTab(activeTab);

        context.PromptService.EnqueueUnsavedChangesDecision(UnsavedChangesDecision.Save);
        context.PromptService.EnqueueUnsavedChangesDecision(UnsavedChangesDecision.Discard);
        context.LifecycleService.OnSave = MarkDocumentSaved;

        context.ViewModel.CloseOtherWorkspaceTabsCommand.Execute(null);

        WorkspaceTabViewModel remainingTab = Assert.Single(context.WorkspaceTabs.Tabs);

        Assert.Same(activeTab, remainingTab);
        Assert.Same(activeTab, context.WorkspaceTabs.ActiveTab);
        Assert.Same(activeTab.Document, context.ViewModel.CurrentDocument);
        Assert.DoesNotContain(cleanInactiveTab, context.WorkspaceTabs.Tabs);
        Assert.DoesNotContain(dirtySaveTab, context.WorkspaceTabs.Tabs);
        Assert.DoesNotContain(dirtyDiscardTab, context.WorkspaceTabs.Tabs);
        Assert.Equal(2, context.PromptService.ConfirmUnsavedChangesCalls);
        Assert.Equal(1, context.LifecycleService.SaveWorkspaceCalls);
    }

    [Fact]
    public void OpenWorkspaceSmoke_Should_Reuse_Empty_Tab_And_Create_New_Tab_For_NonEmpty_Tab()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel initialDocument = context.ViewModel.CurrentDocument;

        context.LifecycleService.OnLoad = document =>
            LoadWorkspaceInto(
                document,
                sessionName: "Reused Workspace",
                profileName: "Reused Profile",
                profileEntryId: "profile-reused",
                workspaceFilePath: @"D:\Workspaces\reused.filemerger.workspace.json");

        context.ViewModel.LoadWorkspaceCommand.Execute(null);

        Assert.Single(context.WorkspaceTabs.Tabs);
        Assert.Same(initialDocument, context.ViewModel.CurrentDocument);
        Assert.Same(initialDocument, context.LifecycleService.LastLoadWorkspaceDocument);
        Assert.Equal("Reused Workspace", context.ViewModel.CurrentDocument.SessionSettings.SessionName);
        Assert.Equal("Reused Profile", context.ViewModel.CurrentProfileCard.ProfileName);
        Assert.Equal("profile-reused", context.ViewModel.CurrentProfileCard.ProfileEntryId);

        MakeWorkspaceNonEmpty(initialDocument);

        context.LifecycleService.OnLoad = document =>
            LoadWorkspaceInto(
                document,
                sessionName: "Loaded Workspace",
                profileName: "Loaded Profile",
                profileEntryId: "profile-loaded",
                workspaceFilePath: @"D:\Workspaces\loaded.filemerger.workspace.json");

        context.ViewModel.LoadWorkspaceCommand.Execute(null);

        Assert.Equal(2, context.WorkspaceTabs.Tabs.Count);
        Assert.NotSame(initialDocument, context.ViewModel.CurrentDocument);
        Assert.Same(context.ViewModel.CurrentDocument, context.LifecycleService.LastLoadWorkspaceDocument);
        Assert.Equal("Loaded Workspace", context.ViewModel.CurrentDocument.SessionSettings.SessionName);
        Assert.Equal(@"D:\Workspaces\loaded.filemerger.workspace.json", context.ViewModel.CurrentDocument.WorkspaceFilePath);
        Assert.Equal("Loaded Profile", context.ViewModel.CurrentProfileCard.ProfileName);
        Assert.Equal("profile-loaded", context.ViewModel.CurrentProfileCard.ProfileEntryId);
    }

    [Fact]
    public void ActiveTabWorkflowSmoke_Should_Run_Document_Commands_On_Active_Tab()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel firstDocument = context.ViewModel.CurrentDocument;
        firstDocument.SessionSettings.SessionName = "First Workspace";
        firstDocument.SetLastOutput(CreateOutput(@"D:\Output\first.txt"));
        firstDocument.SessionSettings.OutputPath = @"D:\Output\first.txt";

        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel secondDocument = secondTab.Document;
        secondDocument.SessionSettings.SessionName = "Second Workspace";
        secondDocument.SetLastOutput(CreateOutput(@"D:\Output\second.txt"));
        secondDocument.SessionSettings.OutputPath = @"D:\Output\second.txt";

        context.WorkspaceTabs.SelectTab(secondTab);

        context.ViewModel.BuildPreviewCommand.Execute(null);
        context.ViewModel.SaveCommand.Execute(null);
        context.ViewModel.CopyPreviewCommand.Execute(null);
        context.ViewModel.OpenOutputFolderCommand.Execute(null);
        context.ViewModel.SaveWorkspaceCommand.Execute(null);
        context.ViewModel.SaveWorkspaceAsCommand.Execute(null);

        Assert.Same(secondDocument, context.PreviewService.LastBuildPreviewDocument);
        Assert.Same(secondDocument, context.OutputService.LastSaveOutputDocument);
        Assert.Same(secondDocument, context.LifecycleService.LastSaveWorkspaceDocument);
        Assert.Same(secondDocument, context.LifecycleService.LastSaveWorkspaceAsDocument);
        Assert.Equal("merged", context.ClipboardService.LastText);
        Assert.Same(secondDocument, context.OutputService.LastOpenOutputFolderDocument);

        context.WorkspaceTabs.SelectTab(context.WorkspaceTabs.Tabs[0]);

        context.ViewModel.BuildPreviewCommand.Execute(null);
        context.ViewModel.SaveCommand.Execute(null);
        context.ViewModel.CopyPreviewCommand.Execute(null);
        context.ViewModel.OpenOutputFolderCommand.Execute(null);
        context.ViewModel.SaveWorkspaceCommand.Execute(null);
        context.ViewModel.SaveWorkspaceAsCommand.Execute(null);

        Assert.Same(firstDocument, context.PreviewService.LastBuildPreviewDocument);
        Assert.Same(firstDocument, context.OutputService.LastSaveOutputDocument);
        Assert.Same(firstDocument, context.LifecycleService.LastSaveWorkspaceDocument);
        Assert.Same(firstDocument, context.LifecycleService.LastSaveWorkspaceAsDocument);
        Assert.Equal("merged", context.ClipboardService.LastText);
        Assert.Same(firstDocument, context.OutputService.LastOpenOutputFolderDocument);
    }

    [Fact]
    public void ProfileCardSmoke_Should_Follow_Loaded_Workspace_And_Tab_Switches()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel profileATab = context.WorkspaceTabs.ActiveTab;

        context.ViewModel.ApplyProfileToCurrentSession(
            profileName: "Profile A",
            profile: CreateProfile(includeHeaderComment: true),
            profileEntryId: "profile-a");

        Assert.Equal("Profile A", context.ViewModel.CurrentProfileCard.ProfileName);
        Assert.Equal("profile-a", context.ViewModel.CurrentProfileCard.ProfileEntryId);
        Assert.True(context.ViewModel.CaptureCurrentProfile().IncludeHeaderComment);

        WorkspaceTabViewModel profileBTab = context.WorkspaceTabs.CreateNewTab();

        context.LifecycleService.OnLoad = document =>
            LoadWorkspaceInto(
                document,
                sessionName: "Loaded Workspace",
                profileName: "Loaded Profile",
                profileEntryId: "profile-loaded",
                workspaceFilePath: @"D:\Workspaces\loaded.filemerger.workspace.json",
                includeHeaderComment: false);

        context.ViewModel.LoadWorkspaceCommand.Execute(null);

        Assert.Same(profileBTab.Document, context.ViewModel.CurrentDocument);
        Assert.Equal("Loaded Profile", context.ViewModel.CurrentProfileCard.ProfileName);
        Assert.Equal("profile-loaded", context.ViewModel.CurrentProfileCard.ProfileEntryId);
        Assert.False(context.ViewModel.CaptureCurrentProfile().IncludeHeaderComment);

        context.WorkspaceTabs.SelectTab(profileATab);

        Assert.Equal("Profile A", context.ViewModel.CurrentProfileCard.ProfileName);
        Assert.Equal("profile-a", context.ViewModel.CurrentProfileCard.ProfileEntryId);
        Assert.True(context.ViewModel.CaptureCurrentProfile().IncludeHeaderComment);

        context.WorkspaceTabs.SelectTab(profileBTab);

        Assert.Equal("Loaded Profile", context.ViewModel.CurrentProfileCard.ProfileName);
        Assert.Equal("profile-loaded", context.ViewModel.CurrentProfileCard.ProfileEntryId);
        Assert.False(context.ViewModel.CaptureCurrentProfile().IncludeHeaderComment);
    }

    [Fact]
    public void BusyTabSmoke_Should_Block_Tab_Actions_Without_Losing_State()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel inactiveTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceTabViewModel busyInactiveTab = context.WorkspaceTabs.CreateNewTab();

        context.WorkspaceTabs.SelectTab(activeTab);

        int tabCount = context.WorkspaceTabs.Tabs.Count;

        activeTab.Document.OperationStatus.IsBusy = true;

        Assert.False(context.ViewModel.DuplicateWorkspaceTabCommand.CanExecute(null));
        Assert.False(context.ViewModel.RenameWorkspaceTabCommand.CanExecute(null));
        Assert.False(context.ViewModel.CloseActiveWorkspaceTabCommand.CanExecute(null));

        context.ViewModel.CloseActiveWorkspaceTabCommand.Execute(null);

        Assert.Equal(tabCount, context.WorkspaceTabs.Tabs.Count);
        Assert.Same(activeTab, context.WorkspaceTabs.ActiveTab);
        Assert.Contains(inactiveTab, context.WorkspaceTabs.Tabs);
        Assert.Contains(busyInactiveTab, context.WorkspaceTabs.Tabs);

        activeTab.Document.OperationStatus.IsBusy = false;
        busyInactiveTab.Document.OperationStatus.IsBusy = true;

        Assert.False(context.ViewModel.CloseOtherWorkspaceTabsCommand.CanExecute(null));

        context.ViewModel.CloseOtherWorkspaceTabsCommand.Execute(null);

        Assert.Equal(tabCount, context.WorkspaceTabs.Tabs.Count);
        Assert.Same(activeTab, context.WorkspaceTabs.ActiveTab);
        Assert.Contains(inactiveTab, context.WorkspaceTabs.Tabs);
        Assert.Contains(busyInactiveTab, context.WorkspaceTabs.Tabs);
    }

    [Fact]
    public void FirstWorkspaceGuide_Should_Show_For_New_Empty_Workspace()
    {
        TestContext context = CreateContext();

        Assert.True(context.ViewModel.ShowFirstWorkspaceGuide);
        Assert.True(context.ViewModel.ShowPreviewEmptyState);
        Assert.False(context.ViewModel.ShowRegularPreviewEmptyState);
        Assert.Contains("Default", context.ViewModel.FirstWorkspaceProfileHint);
    }

    [Fact]
    public void FirstWorkspaceGuide_Should_Hide_When_Source_Is_Added()
    {
        TestContext context = CreateContext();

        context.ViewModel.CurrentDocument.SourcesPane.LoadSources(
        [
            new MergeSource(
                @"D:\Project",
                MergeSourceType.Directory,
                isRecursive: true,
                isEnabled: true)
        ]);

        Assert.False(context.ViewModel.ShowFirstWorkspaceGuide);
        Assert.True(context.ViewModel.ShowRegularPreviewEmptyState);
    }

    [Fact]
    public void FirstWorkspaceGuide_Should_Hide_When_PreviewContent_Exists()
    {
        TestContext context = CreateContext();

        context.ViewModel.CurrentDocument.PreviewContent = "merged";

        Assert.False(context.ViewModel.ShowFirstWorkspaceGuide);
        Assert.False(context.ViewModel.ShowPreviewEmptyState);
        Assert.False(context.ViewModel.ShowRegularPreviewEmptyState);
    }

    [Fact]
    public void FirstWorkspaceGuide_Should_Show_Current_Profile_Name()
    {
        TestContext context = CreateContext();

        context.ViewModel.ApplyProfileToCurrentSession(
            profileName: "Docs and Config",
            profile: CreateProfile(includeHeaderComment: true),
            profileEntryId: "built-in-docs-config");

        Assert.Contains("Docs and Config", context.ViewModel.FirstWorkspaceProfileHint);
    }

    [Fact]
    public void FirstWorkspaceGuide_Should_Show_Recent_Action_When_Usable_Recent_Workspace_Exists()
    {
        TestContext context = CreateContext();

        context.RecentWorkspacesService.Entries.Add(new RecentWorkspaceEntry(
            FilePath: @"D:\Workspaces\one.filemerger.workspace.json",
            LastUsedAtUtc: DateTime.UtcNow,
            Exists: true));

        context.ViewModel.RefreshRecentWorkspacesCommand.Execute(null);

        Assert.True(context.ViewModel.HasFirstRecentWorkspace);
        Assert.NotNull(context.ViewModel.FirstRecentWorkspace);
        Assert.Contains("one", context.ViewModel.FirstRecentWorkspaceText);
    }

    [Fact]
    public void FirstWorkspaceGuide_Should_Ignore_Missing_Recent_Workspace()
    {
        TestContext context = CreateContext();

        context.RecentWorkspacesService.Entries.Add(new RecentWorkspaceEntry(
            FilePath: @"D:\Workspaces\missing.filemerger.workspace.json",
            LastUsedAtUtc: DateTime.UtcNow,
            Exists: false));

        context.ViewModel.RefreshRecentWorkspacesCommand.Execute(null);

        Assert.False(context.ViewModel.HasFirstRecentWorkspace);
        Assert.Null(context.ViewModel.FirstRecentWorkspace);
        Assert.Equal(string.Empty, context.ViewModel.FirstRecentWorkspaceText);
    }

    [Fact]
    public void FirstWorkspaceGuide_Should_Update_When_Active_Tab_Changes()
    {
        TestContext context = CreateContext();
        WorkspaceTabViewModel emptyTab = context.WorkspaceTabs.ActiveTab;

        Assert.True(context.ViewModel.ShowFirstWorkspaceGuide);

        WorkspaceTabViewModel configuredTab = context.WorkspaceTabs.CreateNewTab();
        configuredTab.Document.SourcesPane.LoadSources(
        [
            new MergeSource(
                @"D:\Project",
                MergeSourceType.Directory,
                isRecursive: true,
                isEnabled: true)
        ]);

        Assert.False(context.ViewModel.ShowFirstWorkspaceGuide);

        context.WorkspaceTabs.SelectTab(emptyTab);

        Assert.True(context.ViewModel.ShowFirstWorkspaceGuide);

        context.WorkspaceTabs.SelectTab(configuredTab);

        Assert.False(context.ViewModel.ShowFirstWorkspaceGuide);
    }

    [Fact]
    public void RefreshRecentWorkspacesCommand_Should_Load_Recent_Workspaces()
    {
        TestContext context = CreateContext();

        context.RecentWorkspacesService.Entries.Add(new RecentWorkspaceEntry(
            FilePath: @"D:\Workspaces\one.filemerger.workspace.json",
            LastUsedAtUtc: new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            Exists: true));

        context.ViewModel.RefreshRecentWorkspacesCommand.Execute(null);

        Assert.Single(context.ViewModel.RecentWorkspaces);
        Assert.Equal("one", context.ViewModel.RecentWorkspaces.Single().DisplayName);
    }

    [Fact]
    public void RefreshRecentWorkspacesCommand_Should_Split_Missing_Workspaces()
    {
        TestContext context = CreateContext();

        context.RecentWorkspacesService.Entries.Add(new RecentWorkspaceEntry(
            FilePath: @"D:\Workspaces\missing.filemerger.workspace.json",
            LastUsedAtUtc: new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            Exists: false));

        context.ViewModel.RefreshRecentWorkspacesCommand.Execute(null);

        Assert.Single(context.ViewModel.RecentWorkspaces);
        Assert.Single(context.ViewModel.MissingRecentWorkspaces);
        Assert.True(context.ViewModel.HasMissingRecentWorkspaces);
    }

    [Fact]
    public void ClearRecentWorkspacesCommand_Should_Clear_When_Confirmed()
    {
        TestContext context = CreateContext();

        context.PromptService.ConfirmResult = true;

        context.RecentWorkspacesService.Entries.Add(new RecentWorkspaceEntry(
            FilePath: @"D:\Workspaces\one.filemerger.workspace.json",
            LastUsedAtUtc: DateTime.UtcNow,
            Exists: true));

        context.ViewModel.RefreshRecentWorkspacesCommand.Execute(null);
        context.ViewModel.ClearRecentWorkspacesCommand.Execute(null);

        Assert.Equal(1, context.PromptService.ConfirmCalls);
        Assert.Equal(1, context.RecentWorkspacesService.ClearCalls);
        Assert.Empty(context.ViewModel.RecentWorkspaces);
    }

    [Fact]
    public void RemoveMissingRecentWorkspaceCommand_Should_Remove_Entry()
    {
        TestContext context = CreateContext();

        RecentWorkspaceEntry entry = new(
            FilePath: @"D:\Workspaces\missing.filemerger.workspace.json",
            LastUsedAtUtc: DateTime.UtcNow,
            Exists: false);

        context.RecentWorkspacesService.Entries.Add(entry);
        context.ViewModel.RefreshRecentWorkspacesCommand.Execute(null);

        RecentWorkspaceMenuItemViewModel item =
            Assert.Single(context.ViewModel.MissingRecentWorkspaces);

        context.ViewModel.RemoveMissingRecentWorkspaceCommand.Execute(item);

        Assert.Equal(@"D:\Workspaces\missing.filemerger.workspace.json", Assert.Single(context.RecentWorkspacesService.RemovedPaths));
        Assert.Empty(context.ViewModel.RecentWorkspaces);
    }

    [Fact]
    public void RefreshRecentWorkspacesCommand_Should_Show_Normalized_Missing_Workspace_Name()
    {
        TestContext context = CreateContext();

        context.RecentWorkspacesService.Entries.Add(new RecentWorkspaceEntry(
            FilePath: @"D:\Workspaces\missing.filemerger.workspace.json",
            LastUsedAtUtc: DateTime.UtcNow,
            Exists: false));

        context.ViewModel.RefreshRecentWorkspacesCommand.Execute(null);

        RecentWorkspaceMenuItemViewModel item =
            Assert.Single(context.ViewModel.MissingRecentWorkspaces);

        Assert.Equal("missing", item.DisplayName);
        Assert.Equal("missing (missing)", item.MenuHeader);
    }

    [Fact]
    public async Task InitializeAsync_Should_Load_Recent_Workspaces_For_First_Workspace_Guide()
    {
        TestContext context = CreateContext();

        context.RecentWorkspacesService.Entries.Add(new RecentWorkspaceEntry(
            FilePath: @"D:\Workspaces\one.filemerger.workspace.json",
            LastUsedAtUtc: new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            Exists: true));

        await context.ViewModel.InitializeAsync();

        Assert.Equal(1, context.RecentWorkspacesService.GetCalls);
        Assert.Single(context.ViewModel.RecentWorkspaces);
        Assert.True(context.ViewModel.HasFirstRecentWorkspace);
        Assert.NotNull(context.ViewModel.FirstRecentWorkspace);
        Assert.Equal("one", context.ViewModel.FirstRecentWorkspace.DisplayName);
        Assert.Contains("one", context.ViewModel.FirstRecentWorkspaceText);
    }

    [Fact]
    public async Task InitializeAsync_Should_Not_Show_First_Recent_When_Only_Missing_Entries_Exist()
    {
        TestContext context = CreateContext();

        context.RecentWorkspacesService.Entries.Add(new RecentWorkspaceEntry(
            FilePath: @"D:\Workspaces\missing.filemerger.workspace.json",
            LastUsedAtUtc: new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            Exists: false));

        await context.ViewModel.InitializeAsync();

        Assert.Equal(1, context.RecentWorkspacesService.GetCalls);
        Assert.Single(context.ViewModel.RecentWorkspaces);
        Assert.Single(context.ViewModel.MissingRecentWorkspaces);
        Assert.False(context.ViewModel.HasFirstRecentWorkspace);
        Assert.Null(context.ViewModel.FirstRecentWorkspace);
        Assert.Equal(string.Empty, context.ViewModel.FirstRecentWorkspaceText);
    }

    private static TestContext CreateContext()
    {
        FakeWorkspaceDocumentFactory documentFactory = new();
        FakeWorkspaceDocumentLifecycleService lifecycleService = new();
        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        FakeUserPromptService promptService = new();

        WorkspaceDocumentCloneService cloneService = new(
            documentFactory,
            dirtyStateService);

        WorkspaceTabManagerViewModel workspaceTabs = new(
            documentFactory,
            lifecycleService,
            promptService,
            cloneService);

        FakeWorkspaceDocumentPreviewService previewService = new();
        FakeWorkspaceDocumentOutputService outputService = new();
        FakeProfileManagerWindowService profileManagerWindowService = new();
        FakePreferencesDialogService preferencesDialogService = new();
        FakeWorkspaceConfigurationDialogService workspaceConfigurationDialogService = new();
        FakeWorkspaceTabRenameDialogService renameDialogService = new();
        FakeClipboardService clipboardService = new();
        FakeKeyboardShortcutsDialogService keyboardShortcutsDialogService = new();
        FakeRecentWorkspacesService recentWorkspacesService = new();
        FakeApplicationPreferencesStore applicationPreferencesStore = new();

        MainViewModel viewModel = new(
            previewService,
            outputService,
            lifecycleService,
            dirtyStateService,
            profileManagerWindowService,
            preferencesDialogService,
            workspaceConfigurationDialogService,
            renameDialogService,
            clipboardService,
            keyboardShortcutsDialogService,
            promptService,
            recentWorkspacesService,
            applicationPreferencesStore,
            workspaceTabs);

        return new TestContext(
            viewModel,
            workspaceTabs,
            previewService,
            outputService,
            lifecycleService,
            promptService,
            renameDialogService,
            clipboardService,
            recentWorkspacesService);
    }

    private static WorkspaceProfileDto CreateProfile(bool includeHeaderComment)
    {
        return new WorkspaceProfileDto(
            IncludeHeaderComment: includeHeaderComment,
            IncludeFileSeparators: true,
            IncludeRelativePathInSeparator: true,
            TrimTrailingEmptyLines: true,
            RemoveUsingDirectives: false,
            FileTypes: [],
            LineEndingMode: LineEndingMode.Preserve,
            SortMode: SortMode.ByRelativePathAscending,
            InputEncodingMode: InputEncodingMode.Auto,
            PreferredInputEncodingName: null,
            FallbackInputEncodingName: "windows-1251");
    }

    private static MergeOutput CreateOutput(string outputPath = @"D:\Output\merged.txt")
    {
        return new MergeOutput(
            content: "merged",
            sections: [],
            statistics: new MergeStatistics(
                filesScanned: 1,
                filesIncluded: 1,
                filesSkipped: 0,
                totalCharacters: 6,
                duration: TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: new OutputTarget(outputPath));
    }

    private static void LoadWorkspaceInto(
        WorkspaceDocumentViewModel document,
        string sessionName,
        string profileName,
        string profileEntryId,
        string workspaceFilePath,
        bool includeHeaderComment = true)
    {
        document.SessionSettings.SessionName = sessionName;
        document.WorkspaceFilePath = workspaceFilePath;
        document.ProfileEditor.WorkingProfileName = profileName;
        document.ProfileEditor.ApplyProfile(CreateProfile(includeHeaderComment));
        document.CurrentProfileEntryId = profileEntryId;

        MarkDocumentSaved(document);
    }

    private static void MakeWorkspaceNonEmpty(WorkspaceDocumentViewModel document)
    {
        document.WorkspaceFilePath = @"D:\Workspaces\non-empty.filemerger.workspace.json";
        document.SessionSettings.SessionName = "Non-empty Workspace";

        MarkDocumentSaved(document);
    }

    private static void MarkWorkspaceDirty(WorkspaceDocumentViewModel document)
    {
        document.SessionSettings.OutputPath = $@"D:\Output\{Guid.NewGuid():N}.txt";
        WorkspaceDocumentTestFactory.RefreshWorkspaceDirtyState(document);
    }

    private static void MarkDocumentSaved(WorkspaceDocumentViewModel document)
    {
        WorkspaceDocumentTestFactory.MarkWorkspaceSaved(document);
    }

    private sealed record TestContext(
        MainViewModel ViewModel,
        WorkspaceTabManagerViewModel WorkspaceTabs,
        FakeWorkspaceDocumentPreviewService PreviewService,
        FakeWorkspaceDocumentOutputService OutputService,
        FakeWorkspaceDocumentLifecycleService LifecycleService,
        FakeUserPromptService PromptService,
        FakeWorkspaceTabRenameDialogService RenameDialogService,
        FakeClipboardService ClipboardService,
        FakeRecentWorkspacesService RecentWorkspacesService);

    private sealed class FakeWorkspaceDocumentLifecycleService : IWorkspaceDocumentLifecycleService
    {
        public WorkspaceDocumentViewModel? LastSaveWorkspaceDocument { get; private set; }

        public WorkspaceDocumentViewModel? LastSaveWorkspaceAsDocument { get; private set; }

        public WorkspaceDocumentViewModel? LastLoadWorkspaceDocument { get; private set; }

        public int SaveWorkspaceCalls { get; private set; }

        public int SaveWorkspaceAsCalls { get; private set; }

        public int LoadWorkspaceCalls { get; private set; }

        public int LoadWorkspaceFromPathCalls { get; private set; }

        public bool SaveResult { get; set; } = true;

        public bool SaveAsResult { get; set; } = true;

        public bool LoadResult { get; set; } = true;

        public Action<WorkspaceDocumentViewModel>? OnSave { get; set; }

        public Action<WorkspaceDocumentViewModel>? OnLoad { get; set; }

        public Task<bool> SaveWorkspaceAsync(
            WorkspaceDocumentViewModel document,
            Action? stateChanged = null,
            CancellationToken cancellationToken = default)
        {
            LastSaveWorkspaceDocument = document;
            SaveWorkspaceCalls++;

            if (SaveResult)
            {
                OnSave?.Invoke(document);
                stateChanged?.Invoke();
            }

            return Task.FromResult(SaveResult);
        }

        public Task<bool> SaveWorkspaceAsAsync(
            WorkspaceDocumentViewModel document,
            Action? stateChanged = null,
            CancellationToken cancellationToken = default)
        {
            LastSaveWorkspaceAsDocument = document;
            SaveWorkspaceAsCalls++;

            if (SaveAsResult)
                stateChanged?.Invoke();

            return Task.FromResult(SaveAsResult);
        }

        public Task<bool> LoadWorkspaceAsync(
            WorkspaceDocumentViewModel document,
            Action? stateChanged = null,
            CancellationToken cancellationToken = default)
        {
            LastLoadWorkspaceDocument = document;
            LoadWorkspaceCalls++;

            if (LoadResult)
            {
                OnLoad?.Invoke(document);
                stateChanged?.Invoke();
            }

            return Task.FromResult(LoadResult);
        }

        public Task<bool> LoadWorkspaceFromPathAsync(
            WorkspaceDocumentViewModel document,
            string workspaceFilePath,
            Action? stateChanged = null,
            CancellationToken cancellationToken = default)
        {
            LastLoadWorkspaceDocument = document;
            LoadWorkspaceFromPathCalls++;

            if (LoadResult)
            {
                OnLoad?.Invoke(document);
                stateChanged?.Invoke();
            }

            return Task.FromResult(LoadResult);
        }
    }

    private sealed class FakeWorkspaceDocumentDirtyStateService : IWorkspaceDocumentDirtyStateService
    {
        public void RefreshPreviewDirtyState(WorkspaceDocumentViewModel document)
        {
        }

        public void MarkPreviewApplied(WorkspaceDocumentViewModel document)
        {
        }

        public void RefreshWorkspaceDirtyState(WorkspaceDocumentViewModel document)
        {
        }

        public void MarkWorkspaceSaved(WorkspaceDocumentViewModel document)
        {
            WorkspaceDocumentTestFactory.MarkWorkspaceSaved(document);
        }
    }
}