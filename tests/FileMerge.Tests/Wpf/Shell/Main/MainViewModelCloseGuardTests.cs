using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.Tabs;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shared.ViewModels;
using FileMerger.Wpf.Shell.Main;

namespace FileMerger.Tests.Wpf.Shell.Main;

public sealed class MainViewModelCloseGuardTests
{
    [Fact]
    public void MainViewModel_Should_Implement_Async_Close_Guard()
    {
        TestContext context = CreateContext();

        Assert.IsAssignableFrom<IAsyncCloseGuard>(context.ViewModel);
    }

    [Fact]
    public async Task CanCloseAsync_Should_Accept_Clean_Workspaces_Without_Prompt()
    {
        TestContext context = CreateContext();
        context.WorkspaceTabs.CreateNewTab();

        bool result = await context.ViewModel.CanCloseAsync();

        Assert.True(result);
        Assert.Equal(0, context.PromptService.ConfirmUnsavedChangesCalls);
        Assert.Null(context.LifecycleService.LastSaveWorkspaceDocument);
    }

    [Fact]
    public async Task CanCloseAsync_Should_Save_Dirty_Workspace_And_Allow_Close()
    {
        TestContext context = CreateContext();
        WorkspaceDocumentViewModel document = context.WorkspaceTabs.ActiveDocument;
        MarkWorkspaceDirty(document);

        context.PromptService.UnsavedChangesDecision = UnsavedChangesDecision.Save;
        context.LifecycleService.OnSave = WorkspaceDocumentTestFactory.MarkWorkspaceSaved;

        bool result = await context.ViewModel.CanCloseAsync();

        Assert.True(result);
        Assert.False(document.IsWorkspaceDirty);
        Assert.Same(document, context.LifecycleService.LastSaveWorkspaceDocument);
        Assert.Equal(1, context.PromptService.ConfirmUnsavedChangesCalls);
        Assert.Contains("closing FileMerger", context.PromptService.LastUnsavedChangesMessage);
        Assert.DoesNotContain("installing the update", context.PromptService.LastUnsavedChangesMessage);
    }

    [Fact]
    public async Task CanCloseAsync_Should_Accept_Discard_Without_Saving_Or_Clearing_Dirty_State()
    {
        TestContext context = CreateContext();
        WorkspaceDocumentViewModel document = context.WorkspaceTabs.ActiveDocument;
        MarkWorkspaceDirty(document);
        context.PromptService.UnsavedChangesDecision = UnsavedChangesDecision.Discard;

        bool result = await context.ViewModel.CanCloseAsync();

        Assert.True(result);
        Assert.True(document.IsWorkspaceDirty);
        Assert.Null(context.LifecycleService.LastSaveWorkspaceDocument);
        Assert.Equal(1, context.PromptService.ConfirmUnsavedChangesCalls);
    }

    [Fact]
    public async Task CanCloseAsync_Should_Reject_When_User_Cancels()
    {
        TestContext context = CreateContext();
        WorkspaceDocumentViewModel document = context.WorkspaceTabs.ActiveDocument;
        MarkWorkspaceDirty(document);
        context.PromptService.UnsavedChangesDecision = UnsavedChangesDecision.Cancel;

        bool result = await context.ViewModel.CanCloseAsync();

        Assert.False(result);
        Assert.True(document.IsWorkspaceDirty);
        Assert.Null(context.LifecycleService.LastSaveWorkspaceDocument);
        Assert.Equal(1, context.PromptService.ConfirmUnsavedChangesCalls);
    }

    [Fact]
    public async Task CanCloseAsync_Should_Reject_When_Save_Fails()
    {
        TestContext context = CreateContext();
        WorkspaceDocumentViewModel document = context.WorkspaceTabs.ActiveDocument;
        MarkWorkspaceDirty(document);
        context.PromptService.UnsavedChangesDecision = UnsavedChangesDecision.Save;
        context.LifecycleService.SaveResult = false;

        bool result = await context.ViewModel.CanCloseAsync();

        Assert.False(result);
        Assert.True(document.IsWorkspaceDirty);
        Assert.Same(document, context.LifecycleService.LastSaveWorkspaceDocument);
        Assert.Equal(1, context.PromptService.ConfirmUnsavedChangesCalls);
    }

    [Fact]
    public async Task CanCloseAsync_Should_Guard_Inactive_Dirty_Workspace()
    {
        TestContext context = CreateContext();
        WorkspaceTabViewModel inactiveTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.CreateNewTab();
        MarkWorkspaceDirty(inactiveTab.Document);
        context.WorkspaceTabs.SelectTab(activeTab);
        context.PromptService.UnsavedChangesDecision = UnsavedChangesDecision.Cancel;

        bool result = await context.ViewModel.CanCloseAsync();

        Assert.False(result);
        Assert.Same(activeTab, context.WorkspaceTabs.ActiveTab);
        Assert.True(inactiveTab.IsWorkspaceDirty);
        Assert.Equal(1, context.PromptService.ConfirmUnsavedChangesCalls);
        Assert.Contains(inactiveTab.Title, context.PromptService.LastUnsavedChangesMessage);
    }

    [Fact]
    public async Task CanCloseAsync_Should_Process_Multiple_Dirty_Workspaces_Sequentially()
    {
        TestContext context = CreateContext();
        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        MarkWorkspaceDirty(firstTab.Document);
        MarkWorkspaceDirty(secondTab.Document);

        context.LifecycleService.OnSave = WorkspaceDocumentTestFactory.MarkWorkspaceSaved;
        context.PromptService.EnqueueUnsavedChangesDecision(UnsavedChangesDecision.Save);
        context.PromptService.EnqueueUnsavedChangesDecision(UnsavedChangesDecision.Discard);

        bool result = await context.ViewModel.CanCloseAsync();

        Assert.True(result);
        Assert.False(firstTab.IsWorkspaceDirty);
        Assert.True(secondTab.IsWorkspaceDirty);
        Assert.Equal(2, context.PromptService.ConfirmUnsavedChangesCalls);
        Assert.Same(firstTab.Document, context.LifecycleService.LastSaveWorkspaceDocument);
    }

    [Fact]
    public async Task CanCloseAsync_Should_Reject_When_Any_Workspace_Is_Busy()
    {
        TestContext context = CreateContext();
        WorkspaceTabViewModel busyInactiveTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.CreateNewTab();
        busyInactiveTab.Document.OperationStatus.IsBusy = true;
        context.WorkspaceTabs.SelectTab(activeTab);

        bool result = await context.ViewModel.CanCloseAsync();

        Assert.False(result);
        Assert.Equal(0, context.PromptService.ConfirmUnsavedChangesCalls);
    }

    private static TestContext CreateContext()
    {
        FakeWorkspaceDocumentFactory documentFactory = new();
        FakeWorkspaceDocumentLifecycleService lifecycleService = new();
        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        FakeUserPromptService promptService = new();
        WorkspaceDocumentCloneService cloneService = new(documentFactory, dirtyStateService);
        WorkspaceTabManagerViewModel workspaceTabs = new(
            documentFactory,
            lifecycleService,
            promptService,
            cloneService);

        MainViewModel viewModel = new(
            new FakeWorkspaceDocumentPreviewService(),
            new FakeWorkspaceDocumentOutputService(),
            lifecycleService,
            dirtyStateService,
            new FakeProfileManagerWindowService(),
            new FakePreferencesDialogService(),
            new FakeWorkspaceConfigurationDialogService(),
            new FakeWorkspaceTabRenameDialogService(),
            new FakeClipboardService(),
            new FakeKeyboardShortcutsDialogService(),
            new FakeUpdateCheckDialogService(),
            promptService,
            new FakeRecentWorkspacesService(),
            new FakeApplicationPreferencesStore(),
            workspaceTabs);

        return new TestContext(viewModel, workspaceTabs, lifecycleService, promptService);
    }

    private static void MarkWorkspaceDirty(WorkspaceDocumentViewModel document)
    {
        document.SessionSettings.OutputPath = $@"D:\Output\{Guid.NewGuid():N}.txt";
        WorkspaceDocumentTestFactory.RefreshWorkspaceDirtyState(document);
        Assert.True(document.IsWorkspaceDirty);
    }

    private sealed record TestContext(
        MainViewModel ViewModel,
        WorkspaceTabManagerViewModel WorkspaceTabs,
        FakeWorkspaceDocumentLifecycleService LifecycleService,
        FakeUserPromptService PromptService);

    private sealed class FakeWorkspaceDocumentLifecycleService : IWorkspaceDocumentLifecycleService
    {
        public WorkspaceDocumentViewModel? LastSaveWorkspaceDocument { get; private set; }

        public bool SaveResult { get; set; } = true;

        public Action<WorkspaceDocumentViewModel>? OnSave { get; set; }

        public Task<bool> SaveWorkspaceAsync(
            WorkspaceDocumentViewModel document,
            Action? stateChanged = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastSaveWorkspaceDocument = document;

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
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(false);
        }

        public Task<bool> LoadWorkspaceAsync(
            WorkspaceDocumentViewModel document,
            Action? stateChanged = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(false);
        }

        public Task<bool> LoadWorkspaceFromPathAsync(
            WorkspaceDocumentViewModel document,
            string workspaceFilePath,
            Action? stateChanged = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(false);
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
            WorkspaceDocumentTestFactory.RefreshWorkspaceDirtyState(document);
        }

        public void MarkWorkspaceSaved(WorkspaceDocumentViewModel document)
        {
            WorkspaceDocumentTestFactory.MarkWorkspaceSaved(document);
        }
    }
}