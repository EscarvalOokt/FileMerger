using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.State;
using FileMerger.Wpf.Features.Workspace.Tabs;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shared.Status;

namespace FileMerger.Tests.Wpf.Features.Workspace;

public sealed class WorkspaceTabManagerViewModelTests
{
    [Fact]
    public void Constructor_Should_Create_Initial_Tab()
    {
        FakeWorkspaceDocumentFactory factory = new();

        WorkspaceTabManagerViewModel manager = new(factory);

        WorkspaceTabViewModel tab = Assert.Single(manager.Tabs);

        Assert.Equal(tab, manager.ActiveTab);
        Assert.Equal(tab.Document, manager.ActiveDocument);
        Assert.True(tab.IsActive);
        Assert.Single(factory.CreatedDocuments);
    }

    [Fact]
    public void CreateNewTab_Should_Create_Additional_Tab_And_Select_It()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;

        WorkspaceTabViewModel secondTab = manager.CreateNewTab();

        Assert.Equal(2, manager.Tabs.Count);
        Assert.Equal(secondTab, manager.ActiveTab);
        Assert.Equal(secondTab.Document, manager.ActiveDocument);

        Assert.False(firstTab.IsActive);
        Assert.True(secondTab.IsActive);
    }

    [Fact]
    public void SelectTab_Should_Select_Existing_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel secondTab = manager.CreateNewTab();

        manager.SelectTab(firstTab);

        Assert.Equal(firstTab, manager.ActiveTab);
        Assert.True(firstTab.IsActive);
        Assert.False(secondTab.IsActive);
    }

    [Fact]
    public void SelectTab_Should_Throw_When_Tab_Is_Null()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => manager.SelectTab(null!));

        Assert.Equal("tab", ex.ParamName);
    }

    [Fact]
    public void SelectTab_Should_Throw_When_Tab_Does_Not_Belong_To_Manager()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();
        WorkspaceTabViewModel foreignTab = new(CreateDocument("Foreign Workspace"));

        Assert.Throws<InvalidOperationException>(() => manager.SelectTab(foreignTab));
    }

    [Fact]
    public void CanSelectNextTab_Should_Return_False_When_Only_One_Tab_Exists()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        Assert.False(manager.CanSelectNextTab());
    }

    [Fact]
    public void CanSelectNextTab_Should_Return_True_When_Multiple_Tabs_Exist()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        manager.CreateNewTab();

        Assert.True(manager.CanSelectNextTab());
    }

    [Fact]
    public void CanSelectPreviousTab_Should_Return_False_When_Only_One_Tab_Exists()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        Assert.False(manager.CanSelectPreviousTab());
    }

    [Fact]
    public void CanSelectPreviousTab_Should_Return_True_When_Multiple_Tabs_Exist()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        manager.CreateNewTab();

        Assert.True(manager.CanSelectPreviousTab());
    }

    [Fact]
    public void SelectNextTab_Should_Select_Next_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel secondTab = manager.CreateNewTab();
        WorkspaceTabViewModel thirdTab = manager.CreateNewTab();

        manager.SelectTab(firstTab);

        bool result = manager.SelectNextTab();

        Assert.True(result);
        Assert.Equal(secondTab, manager.ActiveTab);
        Assert.False(firstTab.IsActive);
        Assert.True(secondTab.IsActive);
        Assert.False(thirdTab.IsActive);
    }

    [Fact]
    public void SelectNextTab_Should_Wrap_To_First_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        manager.CreateNewTab();
        WorkspaceTabViewModel thirdTab = manager.CreateNewTab();

        manager.SelectTab(thirdTab);

        bool result = manager.SelectNextTab();

        Assert.True(result);
        Assert.Equal(firstTab, manager.ActiveTab);
        Assert.True(firstTab.IsActive);
        Assert.False(thirdTab.IsActive);
    }

    [Fact]
    public void SelectNextTab_Should_Return_False_When_Only_One_Tab_Exists()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel onlyTab = manager.ActiveTab;

        bool result = manager.SelectNextTab();

        Assert.False(result);
        Assert.Equal(onlyTab, manager.ActiveTab);
        Assert.True(onlyTab.IsActive);
    }

    [Fact]
    public void SelectPreviousTab_Should_Select_Previous_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel secondTab = manager.CreateNewTab();
        WorkspaceTabViewModel thirdTab = manager.CreateNewTab();

        manager.SelectTab(thirdTab);

        bool result = manager.SelectPreviousTab();

        Assert.True(result);
        Assert.Equal(secondTab, manager.ActiveTab);
        Assert.False(firstTab.IsActive);
        Assert.True(secondTab.IsActive);
        Assert.False(thirdTab.IsActive);
    }

    [Fact]
    public void SelectPreviousTab_Should_Wrap_To_Last_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        manager.CreateNewTab();
        WorkspaceTabViewModel thirdTab = manager.CreateNewTab();

        manager.SelectTab(firstTab);

        bool result = manager.SelectPreviousTab();

        Assert.True(result);
        Assert.Equal(thirdTab, manager.ActiveTab);
        Assert.False(firstTab.IsActive);
        Assert.True(thirdTab.IsActive);
    }

    [Fact]
    public void SelectPreviousTab_Should_Return_False_When_Only_One_Tab_Exists()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel onlyTab = manager.ActiveTab;

        bool result = manager.SelectPreviousTab();

        Assert.False(result);
        Assert.Equal(onlyTab, manager.ActiveTab);
        Assert.True(onlyTab.IsActive);
    }

    [Fact]
    public void CloseTab_Should_Close_Inactive_Tab_And_Keep_Active_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel secondTab = manager.CreateNewTab();

        manager.SelectTab(firstTab);

        bool result = manager.CloseTab(secondTab);

        Assert.True(result);
        Assert.Single(manager.Tabs);
        Assert.Equal(firstTab, manager.ActiveTab);
        Assert.True(firstTab.IsActive);
        Assert.False(secondTab.IsActive);
    }

    [Fact]
    public void CloseTab_Should_Close_Active_Tab_And_Select_Next_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel secondTab = manager.CreateNewTab();
        WorkspaceTabViewModel thirdTab = manager.CreateNewTab();

        manager.SelectTab(secondTab);

        bool result = manager.CloseTab(secondTab);

        Assert.True(result);
        Assert.Equal(2, manager.Tabs.Count);
        Assert.DoesNotContain(secondTab, manager.Tabs);

        Assert.Equal(thirdTab, manager.ActiveTab);
        Assert.False(firstTab.IsActive);
        Assert.False(secondTab.IsActive);
        Assert.True(thirdTab.IsActive);
    }

    [Fact]
    public void CloseTab_Should_Close_LastActiveTab_And_Select_Previous_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel secondTab = manager.CreateNewTab();

        bool result = manager.CloseTab(secondTab);

        Assert.True(result);
        Assert.Single(manager.Tabs);
        Assert.Equal(firstTab, manager.ActiveTab);
        Assert.True(firstTab.IsActive);
        Assert.False(secondTab.IsActive);
    }

    [Fact]
    public void CloseTab_Should_Not_Close_Last_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel onlyTab = manager.ActiveTab;

        bool result = manager.CloseTab(onlyTab);

        Assert.False(result);
        Assert.Single(manager.Tabs);
        Assert.Equal(onlyTab, manager.ActiveTab);
        Assert.True(onlyTab.IsActive);
    }

    [Fact]
    public void CloseTab_Should_Throw_When_Tab_Is_Null()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => manager.CloseTab(null!));

        Assert.Equal("tab", ex.ParamName);
    }

    [Fact]
    public void CloseTab_Should_Throw_When_Tab_Does_Not_Belong_To_Manager()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();
        WorkspaceTabViewModel foreignTab = new(CreateDocument("Foreign Workspace"));

        Assert.Throws<InvalidOperationException>(() => manager.CloseTab(foreignTab));
    }

    [Fact]
    public void CanCloseTab_Should_Return_False_For_Last_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        Assert.False(manager.CanCloseTab(manager.ActiveTab));
    }

    [Fact]
    public void CanCloseTab_Should_Return_True_When_Manager_Has_Multiple_Tabs()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        manager.CreateNewTab();

        Assert.True(manager.CanCloseTab(firstTab));
    }

    [Fact]
    public void CanCloseTab_Should_Return_False_For_Null_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        Assert.False(manager.CanCloseTab(null));
    }

    [Fact]
    public void CanCloseTab_Should_Return_False_For_Foreign_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();
        WorkspaceTabViewModel foreignTab = new(CreateDocument("Foreign Workspace"));

        manager.CreateNewTab();

        Assert.False(manager.CanCloseTab(foreignTab));
    }

    [Fact]
    public void CanCloseTab_Should_Return_False_When_Tab_Is_Busy()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel busyTab = manager.CreateNewTab();

        busyTab.Document.OperationStatus.IsBusy = true;

        Assert.False(manager.CanCloseTab(busyTab));
        Assert.True(manager.CanCloseTab(firstTab));
    }

    [Fact]
    public void CloseTab_Should_Not_Close_Busy_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel busyTab = manager.CreateNewTab();

        busyTab.Document.OperationStatus.IsBusy = true;

        bool result = manager.CloseTab(busyTab);

        Assert.False(result);
        Assert.Contains(busyTab, manager.Tabs);
        Assert.Contains(firstTab, manager.Tabs);
    }

    [Fact]
    public void CanCloseOtherTabs_Should_Return_False_When_Only_One_Tab_Exists()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        Assert.False(manager.CanCloseOtherTabs());
    }

    [Fact]
    public void CanCloseOtherTabs_Should_Return_True_When_Inactive_Tabs_Exist()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        manager.CreateNewTab();

        Assert.True(manager.CanCloseOtherTabs());
    }

    [Fact]
    public void CanCloseOtherTabs_Should_Return_False_When_Inactive_Tab_Is_Busy()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel busyInactiveTab = manager.ActiveTab;
        WorkspaceTabViewModel activeTab = manager.CreateNewTab();

        busyInactiveTab.Document.OperationStatus.IsBusy = true;
        manager.SelectTab(activeTab);

        Assert.False(manager.CanCloseOtherTabs());
    }

    [Fact]
    public void ActiveDocument_Should_Return_ActiveTab_Document()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel secondTab = manager.CreateNewTab();

        Assert.Equal(secondTab.Document, manager.ActiveDocument);
    }

    [Fact]
    public void SelectTab_Should_Raise_ActiveTab_And_ActiveDocument_PropertyChanged()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        manager.CreateNewTab();

        List<string?> changedProperties = [];
        manager.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        manager.SelectTab(firstTab);

        Assert.Contains(nameof(WorkspaceTabManagerViewModel.ActiveTab), changedProperties);
        Assert.Contains(nameof(WorkspaceTabManagerViewModel.ActiveDocument), changedProperties);
    }

    [Fact]
    public void Title_Should_Use_Document_SessionName()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Custom Workspace");

        WorkspaceTabViewModel tab = new(document);

        Assert.Equal("Custom Workspace", tab.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Title_Should_Fallback_To_UntitledWorkspace_When_SessionName_Is_Empty(string sessionName)
    {
        WorkspaceDocumentViewModel document = CreateDocument(sessionName);

        WorkspaceTabViewModel tab = new(document);

        Assert.Equal("Untitled Workspace", tab.Title);
    }

    [Fact]
    public void Title_Should_Update_When_Document_SessionName_Changes()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Initial Workspace");
        WorkspaceTabViewModel tab = new(document);

        List<string?> changedProperties = [];
        tab.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        document.SessionSettings.SessionName = "Renamed Workspace";

        Assert.Equal("Renamed Workspace", tab.Title);
        Assert.Contains(nameof(WorkspaceTabViewModel.Title), changedProperties);
    }

    [Fact]
    public void Title_Should_Fallback_To_Workspace_File_Name_When_SessionName_Is_Empty()
    {
        WorkspaceDocumentViewModel document = CreateDocument("");
        document.WorkspaceFilePath = @"D:\Workspaces\loaded.filemerger.workspace.json";

        WorkspaceTabViewModel tab = new(document);

        Assert.Equal("loaded", tab.Title);
    }

    [Fact]
    public void Title_Should_Update_When_WorkspaceFilePath_Changes()
    {
        WorkspaceDocumentViewModel document = CreateDocument("");
        WorkspaceTabViewModel tab = new(document);

        List<string?> changedProperties = [];
        tab.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        document.WorkspaceFilePath = @"D:\Workspaces\loaded.filemerger.workspace.json";

        Assert.Equal("loaded", tab.Title);
        Assert.Contains(nameof(WorkspaceTabViewModel.Title), changedProperties);
    }

    [Fact]
    public void HasDirtyIndicator_Should_Follow_WorkspaceDirty_State()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Dirty Workspace");
        WorkspaceTabViewModel tab = new(document);

        Assert.False(tab.HasDirtyIndicator);

        document.SessionSettings.OutputPath = @"D:\Output\changed.txt";
        WorkspaceDocumentTestFactory.RefreshWorkspaceDirtyState(document);

        Assert.True(tab.IsWorkspaceDirty);
        Assert.True(tab.HasDirtyIndicator);

        WorkspaceDocumentTestFactory.MarkWorkspaceSaved(document);

        Assert.False(tab.IsWorkspaceDirty);
        Assert.False(tab.HasDirtyIndicator);
    }

    [Fact]
    public void WorkspaceDirtyState_Should_Raise_HasDirtyIndicator_PropertyChanged()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Dirty Workspace");
        WorkspaceTabViewModel tab = new(document);

        List<string?> changedProperties = [];
        tab.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        document.SessionSettings.OutputPath = @"D:\Output\changed.txt";
        WorkspaceDocumentTestFactory.RefreshWorkspaceDirtyState(document);

        Assert.Contains(nameof(WorkspaceTabViewModel.HasDirtyIndicator), changedProperties);
    }

    [Fact]
    public void IsBusy_Should_Follow_Document_OperationStatus()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Busy Workspace");
        WorkspaceTabViewModel tab = new(document);

        Assert.False(tab.IsBusy);

        document.OperationStatus.IsBusy = true;

        Assert.True(tab.IsBusy);
    }

    [Fact]
    public void IsBusy_Should_Raise_PropertyChanged_When_Document_OperationStatus_Changes()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Busy Workspace");
        WorkspaceTabViewModel tab = new(document);

        List<string?> changedProperties = [];
        tab.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        document.OperationStatus.IsBusy = true;

        Assert.Contains(nameof(WorkspaceTabViewModel.IsBusy), changedProperties);
        Assert.Contains(nameof(WorkspaceTabViewModel.CanRequestClose), changedProperties);
        Assert.Contains(nameof(WorkspaceTabViewModel.HasActivityIndicator), changedProperties);
        Assert.Contains(nameof(WorkspaceTabViewModel.OperationTooltip), changedProperties);
        Assert.Contains(nameof(WorkspaceTabViewModel.ActivityIndicatorTooltip), changedProperties);
        Assert.Contains(nameof(WorkspaceTabViewModel.CloseTooltip), changedProperties);
    }

    [Fact]
    public void HasActivityIndicator_Should_Follow_Document_OperationStatus()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Busy Workspace");
        WorkspaceTabViewModel tab = new(document);

        Assert.False(tab.HasActivityIndicator);

        document.OperationStatus.IsBusy = true;

        Assert.True(tab.HasActivityIndicator);

        document.OperationStatus.IsBusy = false;

        Assert.False(tab.HasActivityIndicator);
    }

    [Fact]
    public void IsBusy_Should_Raise_OperationPresentation_PropertyChanged_When_Document_OperationStatus_Changes()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Busy Workspace");
        WorkspaceTabViewModel tab = new(document);

        List<string?> changedProperties = [];
        tab.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        document.OperationStatus.IsBusy = true;

        Assert.Contains(nameof(WorkspaceTabViewModel.HasActivityIndicator), changedProperties);
        Assert.Contains(nameof(WorkspaceTabViewModel.OperationTooltip), changedProperties);
        Assert.Contains(nameof(WorkspaceTabViewModel.ActivityIndicatorTooltip), changedProperties);
        Assert.Contains(nameof(WorkspaceTabViewModel.CloseTooltip), changedProperties);
    }

    [Fact]
    public void IsBusy_Should_Raise_ActivityIndicator_PropertyChanged_When_Document_OperationStatus_Changes()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Busy Workspace");
        WorkspaceTabViewModel tab = new(document);

        List<string?> changedProperties = [];
        tab.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        document.OperationStatus.IsBusy = true;

        Assert.Contains(nameof(WorkspaceTabViewModel.HasActivityIndicator), changedProperties);
        Assert.Contains(nameof(WorkspaceTabViewModel.ActivityIndicatorTooltip), changedProperties);
    }

    [Fact]
    public void OperationTooltip_Should_Be_Empty_When_Tab_Is_Not_Busy()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Idle Workspace");
        WorkspaceTabViewModel tab = new(document);

        document.OperationStatus.SetStatus("Saving workspace.", StatusSeverity.Info);
        document.OperationStatus.ShowDeterminateProgress("Saving workspace.", 50);

        Assert.Equal(string.Empty, tab.OperationTooltip);
    }

    [Fact]
    public void OperationTooltip_Should_Use_Progress_Message_When_Tab_Is_Busy()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Busy Workspace");
        WorkspaceTabViewModel tab = new(document);

        document.OperationStatus.IsBusy = true;
        document.OperationStatus.ShowDeterminateProgress("Reading files.", 50);

        Assert.Contains("Reading files.", tab.OperationTooltip);
    }

    [Fact]
    public void OperationTooltip_Should_Use_Status_Message_When_Busy_And_Progress_Is_Hidden()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Busy Workspace");
        WorkspaceTabViewModel tab = new(document);

        document.OperationStatus.SetStatus("Saving workspace.", StatusSeverity.Info);
        document.OperationStatus.IsBusy = true;

        Assert.Contains("Saving workspace.", tab.OperationTooltip);
    }

    [Fact]
    public void OperationTooltip_Should_Update_When_Progress_Message_Changes()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Busy Workspace");
        WorkspaceTabViewModel tab = new(document);

        document.OperationStatus.IsBusy = true;

        List<string?> changedProperties = [];
        tab.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        document.OperationStatus.ShowDeterminateProgress("Reading files.", 25);

        Assert.Contains(nameof(WorkspaceTabViewModel.OperationTooltip), changedProperties);
        Assert.Contains("Reading files.", tab.OperationTooltip);
    }

    [Fact]
    public void ActivityIndicatorTooltip_Should_Use_OperationTooltip()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Busy Workspace");
        WorkspaceTabViewModel tab = new(document);

        document.OperationStatus.IsBusy = true;
        document.OperationStatus.ShowDeterminateProgress("Reading files.", 25);

        Assert.Equal(tab.OperationTooltip, tab.ActivityIndicatorTooltip);
        Assert.Contains("Reading files.", tab.ActivityIndicatorTooltip);
    }

    [Fact]
    public void ActivityIndicatorTooltip_Should_Update_When_Progress_Message_Changes()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Busy Workspace");
        WorkspaceTabViewModel tab = new(document);

        document.OperationStatus.IsBusy = true;

        List<string?> changedProperties = [];
        tab.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        document.OperationStatus.ShowDeterminateProgress("Reading files.", 25);

        Assert.Contains(nameof(WorkspaceTabViewModel.ActivityIndicatorTooltip), changedProperties);
        Assert.Contains("Reading files.", tab.ActivityIndicatorTooltip);
    }

    [Fact]
    public void CloseTooltip_Should_Explain_When_Tab_Is_Busy()
    {
        WorkspaceDocumentViewModel document = CreateDocument("Busy Workspace");
        WorkspaceTabViewModel tab = new(document);

        Assert.Equal("Close workspace tab (Ctrl+W)", tab.CloseTooltip);

        document.OperationStatus.IsBusy = true;

        Assert.Contains("cannot be closed", tab.CloseTooltip);
    }

    [Fact]
    public async Task TryCloseTabAsync_Should_Close_Clean_Tab_Without_Prompt()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new();

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel secondTab = manager.CreateNewTab();

        manager.SelectTab(firstTab);

        bool result = await manager.TryCloseTabAsync(secondTab);

        Assert.True(result);
        Assert.DoesNotContain(secondTab, manager.Tabs);
        Assert.Equal(0, prompt.ConfirmUnsavedChangesCalls);
    }

    [Fact]
    public async Task TryCloseTabAsync_Should_Keep_Dirty_Tab_When_User_Cancels()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new()
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Cancel
        };

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);
        WorkspaceTabViewModel dirtyTab = manager.CreateNewTab();

        dirtyTab.Document.SessionSettings.OutputPath = @"D:\Output\changed.txt";
        dirtyTab.Document.WorkspaceDirtyTracker.Refresh(
            WorkspaceDocumentStateSnapshotFactory.Capture(dirtyTab.Document));

        bool result = await manager.TryCloseTabAsync(dirtyTab);

        Assert.False(result);
        Assert.Contains(dirtyTab, manager.Tabs);
        Assert.Equal(1, prompt.ConfirmUnsavedChangesCalls);
    }

    [Fact]
    public async Task TryCloseTabAsync_Should_Close_Dirty_Tab_When_User_Discards()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new()
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Discard
        };

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);
        WorkspaceTabViewModel dirtyTab = manager.CreateNewTab();

        dirtyTab.Document.SessionSettings.OutputPath = @"D:\Output\changed.txt";
        dirtyTab.Document.WorkspaceDirtyTracker.Refresh(
            WorkspaceDocumentStateSnapshotFactory.Capture(dirtyTab.Document));

        bool result = await manager.TryCloseTabAsync(dirtyTab);

        Assert.True(result);
        Assert.DoesNotContain(dirtyTab, manager.Tabs);
        Assert.Equal(1, prompt.ConfirmUnsavedChangesCalls);
        Assert.Null(lifecycle.LastSaveWorkspaceDocument);
    }

    [Fact]
    public async Task TryCloseTabAsync_Should_Save_And_Close_Dirty_Tab_When_User_Saves()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new()
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Save
        };

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);
        WorkspaceTabViewModel dirtyTab = manager.CreateNewTab();

        dirtyTab.Document.SessionSettings.OutputPath = @"D:\Output\changed.txt";
        dirtyTab.Document.WorkspaceDirtyTracker.Refresh(
            WorkspaceDocumentStateSnapshotFactory.Capture(dirtyTab.Document));

        lifecycle.OnSave = WorkspaceDocumentTestFactory.MarkWorkspaceSaved;

        bool result = await manager.TryCloseTabAsync(dirtyTab);

        Assert.True(result);
        Assert.DoesNotContain(dirtyTab, manager.Tabs);
        Assert.Same(dirtyTab.Document, lifecycle.LastSaveWorkspaceDocument);
    }

    [Fact]
    public async Task TryCloseTabAsync_Should_Keep_Tab_When_Save_Is_Canceled()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new()
        {
            SaveResult = false
        };

        FakeUserPromptService prompt = new()
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Save
        };

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);
        WorkspaceTabViewModel dirtyTab = manager.CreateNewTab();

        dirtyTab.Document.SessionSettings.OutputPath = @"D:\Output\changed.txt";
        dirtyTab.Document.WorkspaceDirtyTracker.Refresh(
            WorkspaceDocumentStateSnapshotFactory.Capture(dirtyTab.Document));

        bool result = await manager.TryCloseTabAsync(dirtyTab);

        Assert.False(result);
        Assert.Contains(dirtyTab, manager.Tabs);
        Assert.Same(dirtyTab.Document, lifecycle.LastSaveWorkspaceDocument);
    }

    [Fact]
    public async Task TryCloseTabAsync_Should_Show_Tab_Name_In_UnsavedChangesPrompt()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new()
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Cancel
        };

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);
        WorkspaceTabViewModel dirtyTab = manager.CreateNewTab();

        dirtyTab.Document.SessionSettings.SessionName = "Important Workspace";
        MarkWorkspaceDirty(dirtyTab.Document);

        bool result = await manager.TryCloseTabAsync(dirtyTab);

        Assert.False(result);
        Assert.Equal("Unsaved workspace", prompt.LastUnsavedChangesTitle);
        Assert.NotNull(prompt.LastUnsavedChangesMessage);
        Assert.Contains("Important Workspace", prompt.LastUnsavedChangesMessage);
        Assert.Contains("Save changes before closing this tab?", prompt.LastUnsavedChangesMessage);
    }

    [Fact]
    public async Task TryCloseTabAsync_Should_Not_Show_UnsavedChangesPrompt_For_Clean_Tab()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new();

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel cleanTab = manager.CreateNewTab();

        manager.SelectTab(firstTab);

        bool result = await manager.TryCloseTabAsync(cleanTab);

        Assert.True(result);
        Assert.Equal(0, prompt.ConfirmUnsavedChangesCalls);
        Assert.Null(prompt.LastUnsavedChangesTitle);
        Assert.Null(prompt.LastUnsavedChangesMessage);
    }

    [Fact]
    public async Task TryCloseTabAsync_Should_Not_Prompt_When_Tab_Is_Busy()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new();

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel busyTab = manager.CreateNewTab();

        MarkWorkspaceDirty(busyTab.Document);
        busyTab.Document.OperationStatus.IsBusy = true;

        bool result = await manager.TryCloseTabAsync(busyTab);

        Assert.False(result);
        Assert.Contains(busyTab, manager.Tabs);
        Assert.Equal(0, prompt.ConfirmUnsavedChangesCalls);
        Assert.Null(lifecycle.LastSaveWorkspaceDocument);
        Assert.Contains(firstTab, manager.Tabs);
    }

    [Fact]
    public async Task TryCloseOtherTabsAsync_Should_Show_Prompt_For_Each_Dirty_Inactive_Tab()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new();

        prompt.EnqueueUnsavedChangesDecision(UnsavedChangesDecision.Discard);
        prompt.EnqueueUnsavedChangesDecision(UnsavedChangesDecision.Discard);

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);

        WorkspaceTabViewModel firstDirtyTab = manager.ActiveTab;
        firstDirtyTab.Document.SessionSettings.SessionName = "First Dirty";
        MarkWorkspaceDirty(firstDirtyTab.Document);

        WorkspaceTabViewModel secondDirtyTab = manager.CreateNewTab();
        secondDirtyTab.Document.SessionSettings.SessionName = "Second Dirty";
        MarkWorkspaceDirty(secondDirtyTab.Document);

        WorkspaceTabViewModel activeTab = manager.CreateNewTab();
        manager.SelectTab(activeTab);

        bool result = await manager.TryCloseOtherTabsAsync();

        Assert.True(result);
        Assert.Equal(2, prompt.ConfirmUnsavedChangesCalls);
        Assert.Contains(prompt.UnsavedChangesMessages, x => x.Contains("First Dirty"));
        Assert.Contains(prompt.UnsavedChangesMessages, x => x.Contains("Second Dirty"));
    }

    [Fact]
    public async Task TryCloseOtherTabsAsync_Should_Not_Close_Last_Remaining_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel onlyTab = manager.ActiveTab;

        bool result = await manager.TryCloseOtherTabsAsync();

        Assert.False(result);
        Assert.Single(manager.Tabs);
        Assert.Equal(onlyTab, manager.ActiveTab);
    }

    [Fact]
    public async Task TryCloseOtherTabsAsync_Should_Close_Only_Inactive_Tabs()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new();

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel activeTab = manager.CreateNewTab();
        WorkspaceTabViewModel thirdTab = manager.CreateNewTab();

        manager.SelectTab(activeTab);

        bool result = await manager.TryCloseOtherTabsAsync();

        Assert.True(result);
        Assert.Single(manager.Tabs);
        Assert.Contains(activeTab, manager.Tabs);
        Assert.DoesNotContain(firstTab, manager.Tabs);
        Assert.DoesNotContain(thirdTab, manager.Tabs);
    }

    [Fact]
    public async Task TryCloseOtherTabsAsync_Should_Keep_Active_Tab_Active()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new();

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel activeTab = manager.CreateNewTab();
        WorkspaceTabViewModel thirdTab = manager.CreateNewTab();

        manager.SelectTab(activeTab);

        bool result = await manager.TryCloseOtherTabsAsync();

        Assert.True(result);
        Assert.Equal(activeTab, manager.ActiveTab);
        Assert.True(activeTab.IsActive);
        Assert.False(firstTab.IsActive);
        Assert.False(thirdTab.IsActive);
    }

    [Fact]
    public async Task TryCloseOtherTabsAsync_Should_Close_Clean_Inactive_Tabs_Without_Prompt()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new();

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);

        WorkspaceTabViewModel activeTab = manager.ActiveTab;
        WorkspaceTabViewModel secondTab = manager.CreateNewTab();
        WorkspaceTabViewModel thirdTab = manager.CreateNewTab();

        manager.SelectTab(activeTab);

        bool result = await manager.TryCloseOtherTabsAsync();

        Assert.True(result);
        Assert.Single(manager.Tabs);
        Assert.Contains(activeTab, manager.Tabs);
        Assert.DoesNotContain(secondTab, manager.Tabs);
        Assert.DoesNotContain(thirdTab, manager.Tabs);
        Assert.Equal(0, prompt.ConfirmUnsavedChangesCalls);
    }

    [Fact]
    public async Task TryCloseOtherTabsAsync_Should_Request_Confirmation_For_Dirty_Inactive_Tabs()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new()
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Discard
        };

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);

        WorkspaceTabViewModel dirtyTab = manager.ActiveTab;
        WorkspaceTabViewModel activeTab = manager.CreateNewTab();

        MarkWorkspaceDirty(dirtyTab.Document);

        manager.SelectTab(activeTab);

        bool result = await manager.TryCloseOtherTabsAsync();

        Assert.True(result);
        Assert.Single(manager.Tabs);
        Assert.Contains(activeTab, manager.Tabs);
        Assert.DoesNotContain(dirtyTab, manager.Tabs);
        Assert.Equal(1, prompt.ConfirmUnsavedChangesCalls);
    }

    [Fact]
    public async Task TryCloseOtherTabsAsync_Should_Stop_When_User_Cancels_Dirty_Tab()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new()
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Cancel
        };

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);

        WorkspaceTabViewModel firstCleanTab = manager.ActiveTab;
        WorkspaceTabViewModel dirtyTab = manager.CreateNewTab();
        WorkspaceTabViewModel remainingTab = manager.CreateNewTab();
        WorkspaceTabViewModel activeTab = manager.CreateNewTab();

        MarkWorkspaceDirty(dirtyTab.Document);

        manager.SelectTab(activeTab);

        bool result = await manager.TryCloseOtherTabsAsync();

        Assert.False(result);
        Assert.DoesNotContain(firstCleanTab, manager.Tabs);
        Assert.Contains(dirtyTab, manager.Tabs);
        Assert.Contains(remainingTab, manager.Tabs);
        Assert.Contains(activeTab, manager.Tabs);
        Assert.Equal(activeTab, manager.ActiveTab);
        Assert.Equal(1, prompt.ConfirmUnsavedChangesCalls);
    }

    [Fact]
    public async Task TryCloseOtherTabsAsync_Should_Stop_When_Save_Is_Canceled()
    {
        FakeWorkspaceDocumentFactory factory = new();

        FakeWorkspaceDocumentLifecycleService lifecycle = new()
        {
            SaveResult = false
        };

        FakeUserPromptService prompt = new()
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Save
        };

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);

        WorkspaceTabViewModel dirtyTab = manager.ActiveTab;
        WorkspaceTabViewModel activeTab = manager.CreateNewTab();

        MarkWorkspaceDirty(dirtyTab.Document);

        manager.SelectTab(activeTab);

        bool result = await manager.TryCloseOtherTabsAsync();

        Assert.False(result);
        Assert.Contains(dirtyTab, manager.Tabs);
        Assert.Contains(activeTab, manager.Tabs);
        Assert.Equal(activeTab, manager.ActiveTab);
        Assert.Same(dirtyTab.Document, lifecycle.LastSaveWorkspaceDocument);
    }

    [Fact]
    public async Task TryCloseOtherTabsAsync_Should_Save_And_Close_Dirty_Inactive_Tab_When_User_Saves()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompt = new()
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Save
        };

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompt);

        WorkspaceTabViewModel dirtyTab = manager.ActiveTab;
        WorkspaceTabViewModel activeTab = manager.CreateNewTab();

        MarkWorkspaceDirty(dirtyTab.Document);

        lifecycle.OnSave = WorkspaceDocumentTestFactory.MarkWorkspaceSaved;

        manager.SelectTab(activeTab);

        bool result = await manager.TryCloseOtherTabsAsync();

        Assert.True(result);
        Assert.Single(manager.Tabs);
        Assert.Contains(activeTab, manager.Tabs);
        Assert.DoesNotContain(dirtyTab, manager.Tabs);
        Assert.Same(dirtyTab.Document, lifecycle.LastSaveWorkspaceDocument);
    }

    [Fact]
    public async Task TryCloseOtherTabsAsync_Should_Not_Close_Any_Tab_When_Inactive_Tab_Is_Busy()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        WorkspaceTabViewModel busyInactiveTab = manager.CreateNewTab();
        WorkspaceTabViewModel activeTab = manager.CreateNewTab();

        busyInactiveTab.Document.OperationStatus.IsBusy = true;
        manager.SelectTab(activeTab);

        bool result = await manager.TryCloseOtherTabsAsync();

        Assert.False(result);
        Assert.Equal(3, manager.Tabs.Count);
        Assert.Contains(firstTab, manager.Tabs);
        Assert.Contains(busyInactiveTab, manager.Tabs);
        Assert.Contains(activeTab, manager.Tabs);
        Assert.Equal(activeTab, manager.ActiveTab);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_Should_Load_Into_Current_Tab_When_Current_Tab_Is_Empty_Clean_And_Pathless()
    {
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        WorkspaceTabManagerViewModel manager = new(new FakeWorkspaceDocumentFactory(), lifecycle);

        WorkspaceTabViewModel originalTab = manager.ActiveTab;

        lifecycle.OnLoad = document =>
        {
            document.SessionSettings.SessionName = "Loaded Workspace";
            document.WorkspaceFilePath = @"D:\Workspaces\loaded.filemerger.workspace.json";
        };

        bool result = await manager.OpenWorkspaceAsync();

        Assert.True(result);
        Assert.Single(manager.Tabs);
        Assert.Same(originalTab, manager.ActiveTab);
        Assert.Same(originalTab.Document, lifecycle.LastLoadWorkspaceDocument);
        Assert.Equal("Loaded Workspace", originalTab.Document.SessionSettings.SessionName);
        Assert.Equal(@"D:\Workspaces\loaded.filemerger.workspace.json", originalTab.Document.WorkspaceFilePath);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_Should_Create_New_Tab_When_Current_Tab_Is_Not_Empty()
    {
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        WorkspaceTabManagerViewModel manager = new(new FakeWorkspaceDocumentFactory(), lifecycle);

        WorkspaceTabViewModel originalTab = manager.ActiveTab;
        originalTab.Document.WorkspaceFilePath = @"D:\Workspaces\existing.filemerger.workspace.json";

        lifecycle.OnLoad = document =>
        {
            document.SessionSettings.SessionName = "Loaded Workspace";
            document.WorkspaceFilePath = @"D:\Workspaces\loaded.filemerger.workspace.json";
        };

        bool result = await manager.OpenWorkspaceAsync();

        Assert.True(result);
        Assert.Equal(2, manager.Tabs.Count);
        Assert.NotSame(originalTab, manager.ActiveTab);
        Assert.Same(manager.ActiveDocument, lifecycle.LastLoadWorkspaceDocument);

        Assert.Equal(@"D:\Workspaces\existing.filemerger.workspace.json", originalTab.Document.WorkspaceFilePath);
        Assert.Equal("Loaded Workspace", manager.ActiveDocument.SessionSettings.SessionName);
        Assert.Equal(@"D:\Workspaces\loaded.filemerger.workspace.json", manager.ActiveDocument.WorkspaceFilePath);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_Should_Remove_Temporary_Tab_When_Load_Is_Canceled()
    {
        FakeWorkspaceDocumentLifecycleService lifecycle = new()
        {
            LoadResult = false
        };

        WorkspaceTabManagerViewModel manager = new(new FakeWorkspaceDocumentFactory(), lifecycle);

        WorkspaceTabViewModel originalTab = manager.ActiveTab;
        originalTab.Document.WorkspaceFilePath = @"D:\Workspaces\existing.filemerger.workspace.json";

        bool result = await manager.OpenWorkspaceAsync();

        Assert.False(result);
        Assert.Single(manager.Tabs);
        Assert.Same(originalTab, manager.ActiveTab);
        Assert.NotSame(originalTab.Document, lifecycle.LastLoadWorkspaceDocument);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_Should_Restore_Previous_Active_Tab_When_New_Tab_Load_Is_Canceled()
    {
        FakeWorkspaceDocumentLifecycleService lifecycle = new()
        {
            LoadResult = false
        };

        WorkspaceTabManagerViewModel manager = new(new FakeWorkspaceDocumentFactory(), lifecycle);

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        firstTab.Document.WorkspaceFilePath = @"D:\Workspaces\first.filemerger.workspace.json";

        WorkspaceTabViewModel secondTab = manager.CreateNewTab();
        secondTab.Document.WorkspaceFilePath = @"D:\Workspaces\second.filemerger.workspace.json";

        manager.SelectTab(firstTab);

        bool result = await manager.OpenWorkspaceAsync();

        Assert.False(result);
        Assert.Equal(2, manager.Tabs.Count);
        Assert.Same(firstTab, manager.ActiveTab);
        Assert.Contains(secondTab, manager.Tabs);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_Should_Throw_When_LifecycleService_Is_Missing()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.OpenWorkspaceAsync());
    }

    [Fact]
    public void DuplicateActiveTab_Should_Create_New_Active_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        WorkspaceTabViewModel sourceTab = manager.ActiveTab;
        sourceTab.Document.SessionSettings.SessionName = "Source Workspace";

        WorkspaceTabViewModel duplicateTab = manager.DuplicateActiveTab();

        Assert.Equal(2, manager.Tabs.Count);
        Assert.Same(duplicateTab, manager.ActiveTab);
        Assert.True(duplicateTab.IsActive);
        Assert.False(sourceTab.IsActive);
        Assert.Equal("Source Workspace Copy", duplicateTab.Document.SessionSettings.SessionName);
    }

    [Fact]
    public void DuplicateTab_Should_Insert_Copy_After_Source_Tab()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        WorkspaceTabViewModel firstTab = manager.ActiveTab;
        firstTab.Document.SessionSettings.SessionName = "First";

        WorkspaceTabViewModel secondTab = manager.CreateNewTab();
        secondTab.Document.SessionSettings.SessionName = "Second";

        manager.SelectTab(firstTab);

        WorkspaceTabViewModel duplicateTab = manager.DuplicateTab(firstTab);

        Assert.Equal(firstTab, manager.Tabs[0]);
        Assert.Equal(duplicateTab, manager.Tabs[1]);
        Assert.Equal(secondTab, manager.Tabs[2]);
    }

    [Fact]
    public void DuplicateTab_Should_Create_Independent_Document_Copy()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        WorkspaceTabViewModel sourceTab = manager.ActiveTab;
        sourceTab.Document.SessionSettings.SessionName = "Source Workspace";
        sourceTab.Document.SessionSettings.OutputPath = @"D:\Output\source.txt";
        sourceTab.Document.WorkspaceFilePath = @"D:\Workspaces\source.filemerger.workspace.json";

        WorkspaceTabViewModel duplicateTab = manager.DuplicateTab(sourceTab);

        Assert.NotSame(sourceTab.Document, duplicateTab.Document);
        Assert.Equal(@"D:\Output\source.txt", duplicateTab.Document.SessionSettings.OutputPath);
        Assert.Null(duplicateTab.Document.WorkspaceFilePath);

        duplicateTab.Document.SessionSettings.OutputPath = @"D:\Output\duplicate.txt";

        Assert.Equal(@"D:\Output\source.txt", sourceTab.Document.SessionSettings.OutputPath);
        Assert.Equal(@"D:\Output\duplicate.txt", duplicateTab.Document.SessionSettings.OutputPath);
    }

    [Fact]
    public void DuplicateTab_Should_Not_Copy_RuntimeState()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        WorkspaceTabViewModel sourceTab = manager.ActiveTab;
        sourceTab.Document.SetLastOutput(CreateOutput());
        sourceTab.Document.PreviewContent = "preview";
        sourceTab.Document.PreviewNotice = "notice";

        WorkspaceTabViewModel duplicateTab = manager.DuplicateTab(sourceTab);

        Assert.Null(duplicateTab.Document.LastOutput);
        Assert.Equal(string.Empty, duplicateTab.Document.PreviewContent);
        Assert.Equal(string.Empty, duplicateTab.Document.PreviewNotice);
    }

    [Fact]
    public void DuplicateTab_Should_Mark_Copy_As_WorkspaceDirty()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        WorkspaceTabViewModel duplicateTab = manager.DuplicateActiveTab();

        Assert.True(duplicateTab.Document.IsWorkspaceDirty);
    }

    [Fact]
    public void DuplicateTab_Should_Generate_Unique_Copy_Name()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        WorkspaceTabViewModel sourceTab = manager.ActiveTab;
        sourceTab.Document.SessionSettings.SessionName = "Source Workspace";

        WorkspaceTabViewModel firstCopy = manager.DuplicateTab(sourceTab);
        WorkspaceTabViewModel secondCopy = manager.DuplicateTab(sourceTab);

        Assert.Equal("Source Workspace Copy", firstCopy.Document.SessionSettings.SessionName);
        Assert.Equal("Source Workspace Copy 2", secondCopy.Document.SessionSettings.SessionName);
    }

    [Fact]
    public void DuplicateTab_Should_Throw_When_Tab_Is_Null()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => manager.DuplicateTab(null!));

        Assert.Equal("tab", ex.ParamName);
    }

    [Fact]
    public void DuplicateTab_Should_Throw_When_Tab_Does_Not_Belong_To_Manager()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();
        WorkspaceTabViewModel foreignTab = new(CreateDocument("Foreign Workspace"));

        Assert.Throws<InvalidOperationException>(() => manager.DuplicateTab(foreignTab));
    }

    [Fact]
    public void DuplicateTab_Should_Throw_When_CloneService_Is_Missing()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();

        Assert.Throws<InvalidOperationException>(manager.DuplicateActiveTab);
    }

    [Fact]
    public void DuplicateTab_Should_Throw_When_Tab_Is_Busy()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        WorkspaceTabViewModel tab = manager.ActiveTab;
        tab.Document.OperationStatus.IsBusy = true;

        Assert.Throws<InvalidOperationException>(() => manager.DuplicateTab(tab));
    }

    [Fact]
    public void CanDuplicateTab_Should_Return_False_When_Tab_Is_Busy()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        WorkspaceTabViewModel tab = manager.ActiveTab;
        tab.Document.OperationStatus.IsBusy = true;

        Assert.False(manager.CanDuplicateTab(tab));
    }

    [Fact]
    public void RenameActiveTab_Should_Update_Active_Tab_SessionName()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        bool result = manager.RenameActiveTab("Renamed Workspace");

        Assert.True(result);
        Assert.Equal("Renamed Workspace", manager.ActiveDocument.SessionSettings.SessionName);
        Assert.Equal("Renamed Workspace", manager.ActiveTab.Title);
    }

    [Fact]
    public void RenameTab_Should_Not_Change_WorkspaceFilePath()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        WorkspaceTabViewModel tab = manager.ActiveTab;
        tab.Document.WorkspaceFilePath = @"D:\Workspaces\source.filemerger.workspace.json";

        bool result = manager.RenameTab(tab, "Renamed Workspace");

        Assert.True(result);
        Assert.Equal(@"D:\Workspaces\source.filemerger.workspace.json", tab.Document.WorkspaceFilePath);
    }

    [Fact]
    public void RenameTab_Should_Mark_WorkspaceDirty()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        WorkspaceTabViewModel tab = manager.ActiveTab;

        bool result = manager.RenameTab(tab, "Renamed Workspace");

        Assert.True(result);
        Assert.True(tab.Document.IsWorkspaceDirty);
    }

    [Fact]
    public void RenameTab_Should_Return_False_When_Name_Is_Empty()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        WorkspaceTabViewModel tab = manager.ActiveTab;
        string originalName = tab.Document.SessionSettings.SessionName;

        bool result = manager.RenameTab(tab, " ");

        Assert.False(result);
        Assert.Equal(originalName, tab.Document.SessionSettings.SessionName);
    }

    [Fact]
    public void RenameTab_Should_Trim_Name()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        bool result = manager.RenameActiveTab("  Renamed Workspace  ");

        Assert.True(result);
        Assert.Equal("Renamed Workspace", manager.ActiveDocument.SessionSettings.SessionName);
    }

    [Fact]
    public void RenameTab_Should_Throw_When_Tab_Is_Null()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            manager.RenameTab(null!, "Renamed Workspace"));

        Assert.Equal("tab", ex.ParamName);
    }

    [Fact]
    public void RenameTab_Should_Throw_When_Tab_Does_Not_Belong_To_Manager()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();
        WorkspaceTabViewModel foreignTab = new(CreateDocument("Foreign Workspace"));

        Assert.Throws<InvalidOperationException>(() => manager.RenameTab(foreignTab, "Renamed Workspace"));
    }

    [Fact]
    public void RenameTab_Should_Return_False_When_Tab_Is_Busy()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        WorkspaceTabViewModel tab = manager.ActiveTab;
        tab.Document.SessionSettings.SessionName = "Original";
        tab.Document.OperationStatus.IsBusy = true;

        bool result = manager.RenameTab(tab, "Renamed");

        Assert.False(result);
        Assert.Equal("Original", tab.Document.SessionSettings.SessionName);
    }

    [Fact]
    public void CanRenameTab_Should_Return_False_When_Tab_Is_Busy()
    {
        WorkspaceTabManagerViewModel manager = CreateManagerWithClone();

        WorkspaceTabViewModel tab = manager.ActiveTab;
        tab.Document.OperationStatus.IsBusy = true;

        Assert.False(manager.CanRenameTab(tab));
    }

    [Fact]
    public async Task TryPrepareForApplicationShutdownAsync_Should_Accept_Clean_Workspaces_Without_Closing_Tabs()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();
        manager.CreateNewTab();
        int tabCount = manager.Tabs.Count;

        bool result = await manager.TryPrepareForApplicationShutdownAsync();

        Assert.True(result);
        Assert.Equal(tabCount, manager.Tabs.Count);
    }

    [Fact]
    public async Task TryPrepareForApplicationShutdownAsync_Should_Reject_When_Any_Tab_Is_Busy()
    {
        WorkspaceTabManagerViewModel manager = CreateManager();
        WorkspaceTabViewModel busyInactiveTab = manager.ActiveTab;
        WorkspaceTabViewModel activeTab = manager.CreateNewTab();
        busyInactiveTab.Document.OperationStatus.IsBusy = true;
        manager.SelectTab(activeTab);

        bool result = await manager.TryPrepareForApplicationShutdownAsync();

        Assert.False(result);
        Assert.True(manager.HasBusyTabs());
        Assert.Equal(2, manager.Tabs.Count);
    }

    [Fact]
    public async Task TryPrepareForApplicationShutdownAsync_Should_Save_Dirty_Workspace_And_Keep_Tab_Open()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new()
        {
            OnSave = WorkspaceDocumentTestFactory.MarkWorkspaceSaved
        };
        FakeUserPromptService prompts = new()
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Save
        };
        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompts);
        WorkspaceTabViewModel tab = manager.ActiveTab;
        MarkWorkspaceDirty(tab.Document);

        bool result = await manager.TryPrepareForApplicationShutdownAsync();

        Assert.True(result);
        Assert.Single(manager.Tabs);
        Assert.False(tab.IsWorkspaceDirty);
        Assert.Same(tab.Document, lifecycle.LastSaveWorkspaceDocument);
    }

    [Fact]
    public async Task TryPrepareForApplicationShutdownAsync_Should_Accept_Discard_Without_Clearing_Dirty_State()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompts = new()
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Discard
        };
        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompts);
        WorkspaceTabViewModel tab = manager.ActiveTab;
        MarkWorkspaceDirty(tab.Document);

        bool result = await manager.TryPrepareForApplicationShutdownAsync();

        Assert.True(result);
        Assert.Single(manager.Tabs);
        Assert.True(tab.IsWorkspaceDirty);
        Assert.Null(lifecycle.LastSaveWorkspaceDocument);
    }

    [Fact]
    public async Task TryPrepareForApplicationShutdownAsync_Should_Stop_On_Cancel()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new();
        FakeUserPromptService prompts = new()
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Cancel
        };
        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompts);
        WorkspaceTabViewModel tab = manager.ActiveTab;
        MarkWorkspaceDirty(tab.Document);

        bool result = await manager.TryPrepareForApplicationShutdownAsync();

        Assert.False(result);
        Assert.Single(manager.Tabs);
        Assert.True(tab.IsWorkspaceDirty);
    }

    [Fact]
    public async Task TryPrepareForApplicationShutdownAsync_Should_Stop_When_Save_Fails()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new()
        {
            SaveResult = false
        };
        FakeUserPromptService prompts = new()
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Save
        };
        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompts);
        WorkspaceTabViewModel tab = manager.ActiveTab;
        MarkWorkspaceDirty(tab.Document);

        bool result = await manager.TryPrepareForApplicationShutdownAsync();

        Assert.False(result);
        Assert.True(tab.IsWorkspaceDirty);
        Assert.Same(tab.Document, lifecycle.LastSaveWorkspaceDocument);
    }

    [Fact]
    public async Task TryPrepareForApplicationShutdownAsync_Should_Process_Multiple_Dirty_Tabs_Sequentially()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentLifecycleService lifecycle = new()
        {
            OnSave = WorkspaceDocumentTestFactory.MarkWorkspaceSaved
        };
        FakeUserPromptService prompts = new();
        prompts.EnqueueUnsavedChangesDecision(UnsavedChangesDecision.Save);
        prompts.EnqueueUnsavedChangesDecision(UnsavedChangesDecision.Discard);

        WorkspaceTabManagerViewModel manager = new(factory, lifecycle, prompts);
        WorkspaceTabViewModel first = manager.ActiveTab;
        WorkspaceTabViewModel second = manager.CreateNewTab();
        MarkWorkspaceDirty(first.Document);
        MarkWorkspaceDirty(second.Document);

        bool result = await manager.TryPrepareForApplicationShutdownAsync();

        Assert.True(result);
        Assert.Equal(2, manager.Tabs.Count);
        Assert.False(first.IsWorkspaceDirty);
        Assert.True(second.IsWorkspaceDirty);
        Assert.Equal(2, prompts.ConfirmUnsavedChangesCalls);
    }

    private static WorkspaceTabManagerViewModel CreateManager()
    {
        return new WorkspaceTabManagerViewModel(new FakeWorkspaceDocumentFactory());
    }

    private static void MarkWorkspaceDirty(WorkspaceDocumentViewModel document)
    {
        document.SessionSettings.OutputPath = $@"D:\Output\{Guid.NewGuid():N}.txt";
        WorkspaceDocumentTestFactory.RefreshWorkspaceDirtyState(document);
    }

    private static WorkspaceTabManagerViewModel CreateManagerWithClone()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();

        WorkspaceDocumentCloneService cloneService = new(factory, dirtyStateService);

        return new WorkspaceTabManagerViewModel(factory, workspaceDocumentCloneService: cloneService);
    }

    private static MergeOutput CreateOutput()
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
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));
    }

    private static WorkspaceDocumentViewModel CreateDocument(string sessionName)
    {
        return WorkspaceDocumentTestFactory.CreateSavedDocument(sessionName: sessionName, outputPath: string.Empty);
    }

    private sealed class FakeWorkspaceDocumentLifecycleService : IWorkspaceDocumentLifecycleService
    {
        public WorkspaceDocumentViewModel? LastSaveWorkspaceDocument { get; private set; }

        public WorkspaceDocumentViewModel? LastLoadWorkspaceDocument { get; private set; }

        public bool SaveResult { get; init; } = true;

        public bool SaveAsResult { get; set; } = true;

        public bool LoadResult { get; init; } = true;

        public Action<WorkspaceDocumentViewModel>? OnSave { get; set; }

        public Action<WorkspaceDocumentViewModel>? OnLoad { get; set; }

        public Task<bool> SaveWorkspaceAsync(
            WorkspaceDocumentViewModel document,
            Action? stateChanged = null,
            CancellationToken cancellationToken = default)
        {
            LastSaveWorkspaceDocument = document;
            OnSave?.Invoke(document);

            return Task.FromResult(SaveResult);
        }

        public Task<bool> SaveWorkspaceAsAsync(
            WorkspaceDocumentViewModel document,
            Action? stateChanged = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SaveAsResult);
        }

        public Task<bool> LoadWorkspaceAsync(
            WorkspaceDocumentViewModel document,
            Action? stateChanged = null,
            CancellationToken cancellationToken = default)
        {
            LastLoadWorkspaceDocument = document;
            OnLoad?.Invoke(document);

            return Task.FromResult(LoadResult);
        }

        public Task<bool> LoadWorkspaceFromPathAsync(
            WorkspaceDocumentViewModel document,
            string workspaceFilePath,
            Action? stateChanged = null,
            CancellationToken cancellationToken = default)
        {
            LastLoadWorkspaceDocument = document;
            OnLoad?.Invoke(document);

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
            WorkspaceDocumentTestFactory.RefreshWorkspaceDirtyState(document);
        }

        public void MarkWorkspaceSaved(WorkspaceDocumentViewModel document)
        {
            WorkspaceDocumentTestFactory.MarkWorkspaceSaved(document);
        }
    }
}