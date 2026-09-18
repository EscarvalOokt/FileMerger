using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Settings;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.Tabs;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shared.Status;
using FileMerger.Wpf.Shell.Main;

namespace FileMerger.Tests.Wpf.Shell.Main;

public sealed class MainViewModelWorkspaceTabRoutingTests
{
    [Fact]
    public void BuildPreviewCommand_Should_Use_ActiveTab_Document()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel firstDocument = context.ViewModel.CurrentDocument;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel secondDocument = secondTab.Document;

        context.WorkspaceTabs.SelectTab(secondTab);

        context.ViewModel.BuildPreviewCommand.Execute(null);

        Assert.Same(secondDocument, context.PreviewService.LastBuildPreviewDocument);
        Assert.NotSame(firstDocument, context.PreviewService.LastBuildPreviewDocument);
    }

    [Fact]
    public void SaveOutputCommand_Should_Use_ActiveTab_Document()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel firstDocument = context.ViewModel.CurrentDocument;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel secondDocument = secondTab.Document;

        secondDocument.SetLastOutput(CreateOutput());
        secondDocument.SessionSettings.OutputPath = @"D:\Output\second.txt";
        context.WorkspaceTabs.SelectTab(secondTab);

        context.ViewModel.SaveCommand.Execute(null);

        Assert.Same(secondDocument, context.OutputService.LastSaveOutputDocument);
        Assert.NotSame(firstDocument, context.OutputService.LastSaveOutputDocument);
    }

    [Fact]
    public void SaveWorkspaceCommand_Should_Use_ActiveTab_Document()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel firstDocument = context.ViewModel.CurrentDocument;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel secondDocument = secondTab.Document;

        context.WorkspaceTabs.SelectTab(secondTab);

        context.ViewModel.SaveWorkspaceCommand.Execute(null);

        Assert.Same(secondDocument, context.LifecycleService.LastSaveWorkspaceDocument);
        Assert.NotSame(firstDocument, context.LifecycleService.LastSaveWorkspaceDocument);
    }

    [Fact]
    public void LoadWorkspaceCommand_Should_Open_Workspace_In_New_Tab_When_Active_Tab_Is_Not_Empty()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel originalDocument = context.ViewModel.CurrentDocument;
        originalDocument.WorkspaceFilePath = @"D:\Workspaces\existing.filemerger.workspace.json";
        originalDocument.SessionSettings.SessionName = "Existing Workspace";

        context.LifecycleService.OnLoad = document =>
        {
            document.SessionSettings.SessionName = "Loaded Workspace";
            document.WorkspaceFilePath = @"D:\Workspaces\loaded.filemerger.workspace.json";
        };

        context.ViewModel.LoadWorkspaceCommand.Execute(null);

        Assert.Equal(2, context.WorkspaceTabs.Tabs.Count);
        Assert.NotSame(originalDocument, context.ViewModel.CurrentDocument);
        Assert.Same(context.ViewModel.CurrentDocument, context.LifecycleService.LastLoadWorkspaceDocument);

        Assert.Equal("Existing Workspace", originalDocument.SessionSettings.SessionName);
        Assert.Equal(@"D:\Workspaces\existing.filemerger.workspace.json", originalDocument.WorkspaceFilePath);

        Assert.Equal("Loaded Workspace", context.ViewModel.CurrentDocument.SessionSettings.SessionName);
        Assert.Equal(
            @"D:\Workspaces\loaded.filemerger.workspace.json",
            context.ViewModel.CurrentDocument.WorkspaceFilePath);
    }

    [Fact]
    public void LoadWorkspaceCommand_Should_Reuse_Empty_Clean_Active_Tab()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel originalDocument = context.ViewModel.CurrentDocument;

        context.LifecycleService.OnLoad = document =>
        {
            document.SessionSettings.SessionName = "Loaded Workspace";
            document.WorkspaceFilePath = @"D:\Workspaces\loaded.filemerger.workspace.json";
        };

        context.ViewModel.LoadWorkspaceCommand.Execute(null);

        Assert.Single(context.WorkspaceTabs.Tabs);
        Assert.Same(originalDocument, context.ViewModel.CurrentDocument);
        Assert.Same(originalDocument, context.LifecycleService.LastLoadWorkspaceDocument);
        Assert.Equal("Loaded Workspace", originalDocument.SessionSettings.SessionName);
        Assert.Equal(@"D:\Workspaces\loaded.filemerger.workspace.json", originalDocument.WorkspaceFilePath);
    }

    [Fact]
    public void LoadWorkspaceCommand_Should_Refresh_Current_Profile_Card_With_Loaded_Profile_Metadata()
    {
        TestContext context = CreateContext();

        context.LifecycleService.OnLoad = document =>
        {
            document.SessionSettings.SessionName = "Loaded Workspace";
            document.WorkspaceFilePath = @"D:\Workspaces\loaded.filemerger.workspace.json";
            document.ProfileEditor.WorkingProfileName = "Loaded Workspace Profile";
            document.CurrentProfileEntryId = "loaded-profile-id";
        };

        context.ViewModel.LoadWorkspaceCommand.Execute(null);

        Assert.Equal("Loaded Workspace Profile", context.ViewModel.CurrentProfileCard.ProfileName);
        Assert.Equal("loaded-profile-id", context.ViewModel.CurrentProfileCard.ProfileEntryId);
        Assert.True(context.ViewModel.CurrentProfileCard.IsLinkedToLibrary);
    }

    [Fact]
    public void WorkspaceConfigurationContext_Should_Follow_Active_Tab()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceDocumentViewModel firstDocument = firstTab.Document;
        firstDocument.SessionSettings.SessionName = "First Session";
        firstDocument.SessionSettings.OutputPath = @"D:\Output\first.txt";
        firstDocument.SourcesPane.LoadSources(
        [
            new MergeSource(@"D:\First", MergeSourceType.Directory, isRecursive: true, isEnabled: true)
        ]);
        firstDocument.ProfileEditor.WorkingProfileName = "First Profile";
        firstDocument.CurrentProfileEntryId = "first-profile";

        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel secondDocument = secondTab.Document;
        secondDocument.SessionSettings.SessionName = "Second Session";
        secondDocument.SessionSettings.OutputPath = @"D:\Output\second.txt";
        secondDocument.SourcesPane.LoadSources(
        [
            new MergeSource(@"D:\Second", MergeSourceType.File, isRecursive: false, isEnabled: true)
        ]);
        secondDocument.ProfileEditor.WorkingProfileName = "Second Profile";
        secondDocument.CurrentProfileEntryId = "second-profile";

        context.WorkspaceTabs.SelectTab(secondTab);

        Assert.Same(secondDocument.SessionSettings, context.ViewModel.SessionSettings);
        Assert.Same(secondDocument.SourcesPane, context.ViewModel.SourcesPane);
        Assert.Equal("Second Session", context.ViewModel.SessionSettings.SessionName);
        Assert.Single(context.ViewModel.SourcesPane.Sources);
        Assert.Equal("Second Profile", context.ViewModel.CurrentProfileCard.ProfileName);
        Assert.Equal("second-profile", context.ViewModel.CurrentProfileCard.ProfileEntryId);

        context.WorkspaceTabs.SelectTab(firstTab);

        Assert.Same(firstDocument.SessionSettings, context.ViewModel.SessionSettings);
        Assert.Same(firstDocument.SourcesPane, context.ViewModel.SourcesPane);
        Assert.Equal("First Session", context.ViewModel.SessionSettings.SessionName);
        Assert.Single(context.ViewModel.SourcesPane.Sources);
        Assert.Equal("First Profile", context.ViewModel.CurrentProfileCard.ProfileName);
        Assert.Equal("first-profile", context.ViewModel.CurrentProfileCard.ProfileEntryId);
    }

    [Fact]
    public void ApplyProfileToCurrentSession_Should_Update_ActiveTab_Only()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel firstDocument = context.ViewModel.CurrentDocument;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel secondDocument = secondTab.Document;

        firstDocument.ProfileEditor.WorkingProfileName = "First Profile";
        firstDocument.ProfileEditor.IncludeHeaderComment = false;

        secondDocument.ProfileEditor.WorkingProfileName = "Second Profile";
        secondDocument.ProfileEditor.IncludeHeaderComment = false;

        context.WorkspaceTabs.SelectTab(secondTab);

        context.ViewModel.ApplyProfileToCurrentSession(
            profileName: "Applied Profile",
            profile: CreateProfile(includeHeaderComment: true),
            profileEntryId: "profile-2");

        Assert.Equal("First Profile", firstDocument.ProfileEditor.WorkingProfileName);
        Assert.False(firstDocument.ProfileEditor.IncludeHeaderComment);
        Assert.Null(firstDocument.CurrentProfileEntryId);

        Assert.Equal("Applied Profile", secondDocument.ProfileEditor.WorkingProfileName);
        Assert.True(secondDocument.ProfileEditor.IncludeHeaderComment);
        Assert.Equal("profile-2", secondDocument.CurrentProfileEntryId);
        Assert.Equal("profile-2", secondDocument.ProfileOriginEntryId);
        Assert.Equal("Applied Profile", secondDocument.ProfileOriginDisplayName);
        Assert.Equal("From profile library", context.ViewModel.CurrentProfileCard.ProfileSourceText);
    }

    [Fact]
    public void ApplyProfileToCurrentSession_Without_EntryId_Should_Clear_Linkage_And_Origin()
    {
        TestContext context = CreateContext();
        WorkspaceDocumentViewModel document = context.ViewModel.CurrentDocument;

        document.CurrentProfileEntryId = "linked-profile";
        document.ProfileOriginEntryId = "linked-profile";
        document.ProfileOriginDisplayName = "Linked Profile";

        context.ViewModel.ApplyProfileToCurrentSession(
            profileName: "Custom Profile",
            profile: CreateProfile(includeHeaderComment: true),
            profileEntryId: null);

        Assert.Null(document.CurrentProfileEntryId);
        Assert.Null(document.ProfileOriginEntryId);
        Assert.Null(document.ProfileOriginDisplayName);
        Assert.Equal("Custom workspace profile", context.ViewModel.CurrentProfileCard.ProfileSourceText);
    }

    [Fact]
    public void CurrentProfileCard_Should_Show_Custom_Profile_Origin()
    {
        TestContext context = CreateContext();
        WorkspaceDocumentViewModel document = context.ViewModel.CurrentDocument;

        document.ProfileEditor.WorkingProfileName = "Customized Profile";
        document.CurrentProfileEntryId = null;
        document.ProfileOriginEntryId = "origin-profile";
        document.ProfileOriginDisplayName = "Origin Profile";

        Assert.Equal("Customized Profile", context.ViewModel.CurrentProfileCard.ProfileName);
        Assert.Null(context.ViewModel.CurrentProfileCard.ProfileEntryId);
        Assert.Equal("origin-profile", context.ViewModel.CurrentProfileCard.ProfileOriginEntryId);
        Assert.Equal("Origin Profile", context.ViewModel.CurrentProfileCard.ProfileOriginDisplayName);
        Assert.Equal(
            "Custom workspace profile based on 'Origin Profile'",
            context.ViewModel.CurrentProfileCard.ProfileSourceText);
    }

    [Fact]
    public void CaptureCurrentProfile_Should_Read_ActiveTab_Profile()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel firstDocument = context.ViewModel.CurrentDocument;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel secondDocument = secondTab.Document;

        firstDocument.ProfileEditor.IncludeHeaderComment = false;
        secondDocument.ProfileEditor.IncludeHeaderComment = true;

        context.WorkspaceTabs.SelectTab(secondTab);

        WorkspaceProfileDto captured = context.ViewModel.CaptureCurrentProfile();

        Assert.True(captured.IncludeHeaderComment);
    }

    [Fact]
    public void Skipped_File_Category_Change_Should_Refresh_Dirty_State_For_Active_Document()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel firstDocument = context.ViewModel.CurrentDocument;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel secondDocument = secondTab.Document;

        context.WorkspaceTabs.SelectTab(secondTab);

        int workspaceRefreshCount = context.DirtyStateService.WorkspaceRefreshedDocuments.Count;
        int previewRefreshCount = context.DirtyStateService.PreviewRefreshedDocuments.Count;

        secondDocument.ProfileEditor.IncludeUnsupportedFilesInSkippedMetadata = false;

        Assert.True(context.DirtyStateService.WorkspaceRefreshedDocuments.Count > workspaceRefreshCount);
        Assert.True(context.DirtyStateService.PreviewRefreshedDocuments.Count > previewRefreshCount);
        Assert.Same(secondDocument, context.DirtyStateService.WorkspaceRefreshedDocuments[^1]);
        Assert.Same(secondDocument, context.DirtyStateService.PreviewRefreshedDocuments[^1]);
        Assert.NotSame(firstDocument, secondDocument);
    }

    [Fact]
    public void SaveCommand_CanExecute_Should_Follow_ActiveTab_LastOutput_And_OutputPath()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();

        secondTab.Document.SetLastOutput(CreateOutput());
        secondTab.Document.SessionSettings.OutputPath = @"D:\Output\merged.txt";

        context.WorkspaceTabs.SelectTab(firstTab);
        Assert.False(context.ViewModel.SaveCommand.CanExecute(null));

        context.WorkspaceTabs.SelectTab(secondTab);
        Assert.True(context.ViewModel.SaveCommand.CanExecute(null));

        secondTab.Document.SessionSettings.OutputPath = string.Empty;
        Assert.False(context.ViewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void SaveOutputAction_Should_Describe_Fresh_Applied_Preview()
    {
        TestContext context = CreateContext();
        WorkspaceDocumentViewModel document = context.ViewModel.CurrentDocument;

        PrepareAppliedPreview(document);

        Assert.True(document.PreviewDirtyTracker.HasAppliedPreview);
        Assert.False(document.PreviewDirtyTracker.IsPreviewDirty);
        Assert.Equal("Save", context.ViewModel.SaveOutputActionText);
        Assert.Equal("Save the latest built output.", context.ViewModel.SaveOutputActionTooltip);
        Assert.True(context.ViewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void SaveOutputAction_Should_Explain_Stale_Applied_Preview()
    {
        TestContext context = CreateContext();
        WorkspaceDocumentViewModel document = context.ViewModel.CurrentDocument;
        PrepareAppliedPreview(document);

        List<string?> changedProperties = [];
        context.ViewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        MakePreviewStale(document);

        Assert.True(document.PreviewDirtyTracker.HasAppliedPreview);
        Assert.True(document.PreviewDirtyTracker.IsPreviewDirty);
        Assert.Equal("Save Last Build", context.ViewModel.SaveOutputActionText);
        Assert.Equal(
            "Save the last successfully built output. Changes made since that build are not included in its content; run Build Preview to rebuild it first.",
            context.ViewModel.SaveOutputActionTooltip);
        Assert.Contains(nameof(MainViewModel.SaveOutputActionText), changedProperties);
        Assert.Contains(nameof(MainViewModel.SaveOutputActionTooltip), changedProperties);
        Assert.True(context.ViewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void SaveOutputAction_Should_Return_To_Fresh_State_After_New_Applied_Preview()
    {
        TestContext context = CreateContext();
        WorkspaceDocumentViewModel document = context.ViewModel.CurrentDocument;
        PrepareAppliedPreview(document);
        MakePreviewStale(document);

        document.SetLastOutput(CreateOutput("rebuilt output"));
        ApplyPreviewBaseline(document);

        Assert.False(document.PreviewDirtyTracker.IsPreviewDirty);
        Assert.Equal("Save", context.ViewModel.SaveOutputActionText);
        Assert.Equal("Save the latest built output.", context.ViewModel.SaveOutputActionTooltip);
    }

    [Fact]
    public void SaveOutputAction_Should_Follow_ActiveTab_Stale_State()
    {
        TestContext context = CreateContext();
        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();

        PrepareAppliedPreview(firstTab.Document);
        PrepareAppliedPreview(secondTab.Document);
        MakePreviewStale(secondTab.Document);

        context.WorkspaceTabs.SelectTab(firstTab);

        Assert.Equal("Save", context.ViewModel.SaveOutputActionText);
        Assert.Equal("Save the latest built output.", context.ViewModel.SaveOutputActionTooltip);

        List<string?> changedProperties = [];
        context.ViewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        context.WorkspaceTabs.SelectTab(secondTab);

        Assert.Equal("Save Last Build", context.ViewModel.SaveOutputActionText);
        Assert.Equal(
            "Save the last successfully built output. Changes made since that build are not included in its content; run Build Preview to rebuild it first.",
            context.ViewModel.SaveOutputActionTooltip);
        Assert.Contains(nameof(MainViewModel.SaveOutputActionText), changedProperties);
        Assert.Contains(nameof(MainViewModel.SaveOutputActionTooltip), changedProperties);

        context.WorkspaceTabs.SelectTab(firstTab);

        Assert.Equal("Save", context.ViewModel.SaveOutputActionText);
    }

    [Fact]
    public void Commands_Should_Use_ActiveTab_BusyState()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();

        firstTab.Document.SetLastOutput(CreateOutput());
        firstTab.Document.SessionSettings.OutputPath = @"D:\Output\first.txt";

        secondTab.Document.SetLastOutput(CreateOutput());
        secondTab.Document.SessionSettings.OutputPath = @"D:\Output\second.txt";

        context.OutputService.CanOpenOutputFolderResult = true;

        firstTab.Document.OperationStatus.IsBusy = true;
        secondTab.Document.OperationStatus.IsBusy = false;

        context.WorkspaceTabs.SelectTab(firstTab);
        Assert.False(context.ViewModel.BuildPreviewCommand.CanExecute(null));
        Assert.False(context.ViewModel.SaveWorkspaceCommand.CanExecute(null));
        Assert.False(context.ViewModel.SaveWorkspaceAsCommand.CanExecute(null));
        Assert.False(context.ViewModel.LoadWorkspaceCommand.CanExecute(null));
        Assert.False(context.ViewModel.DuplicateWorkspaceTabCommand.CanExecute(null));
        Assert.False(context.ViewModel.SaveCommand.CanExecute(null));
        Assert.False(context.ViewModel.CopyPreviewCommand.CanExecute(null));
        Assert.False(context.ViewModel.OpenOutputFolderCommand.CanExecute(null));

        context.WorkspaceTabs.SelectTab(secondTab);
        Assert.True(context.ViewModel.BuildPreviewCommand.CanExecute(null));
        Assert.True(context.ViewModel.SaveWorkspaceCommand.CanExecute(null));
        Assert.True(context.ViewModel.SaveWorkspaceAsCommand.CanExecute(null));
        Assert.True(context.ViewModel.LoadWorkspaceCommand.CanExecute(null));
        Assert.True(context.ViewModel.DuplicateWorkspaceTabCommand.CanExecute(null));
        Assert.True(context.ViewModel.SaveCommand.CanExecute(null));
        Assert.True(context.ViewModel.CopyPreviewCommand.CanExecute(null));
        Assert.True(context.ViewModel.OpenOutputFolderCommand.CanExecute(null));
    }

    [Fact]
    public void SwitchingActiveTab_Should_Show_Active_Document_Status()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();

        firstTab.Document.OperationStatus.SetStatus("First tab ready.", StatusSeverity.Info);
        secondTab.Document.OperationStatus.SetStatus("Second tab ready.", StatusSeverity.Success);

        context.WorkspaceTabs.SelectTab(firstTab);

        Assert.Contains("First tab ready.", context.ViewModel.FooterStatusMessage);

        context.WorkspaceTabs.SelectTab(secondTab);

        Assert.Contains("Second tab ready.", context.ViewModel.FooterStatusMessage);
    }

    [Fact]
    public void SwitchingActiveTab_Should_Show_Active_Document_Progress()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();

        firstTab.Document.OperationStatus.ShowDeterminateProgress("First tab progress.", 25);
        secondTab.Document.OperationStatus.ShowDeterminateProgress("Second tab progress.", 75);

        context.WorkspaceTabs.SelectTab(firstTab);

        Assert.Same(firstTab.Document.OperationStatus, context.ViewModel.OperationStatus);
        Assert.Equal("First tab progress.", context.ViewModel.OperationStatus.ProgressMessage);
        Assert.Equal(25, context.ViewModel.OperationStatus.ProgressValue);

        context.WorkspaceTabs.SelectTab(secondTab);

        Assert.Same(secondTab.Document.OperationStatus, context.ViewModel.OperationStatus);
        Assert.Equal("Second tab progress.", context.ViewModel.OperationStatus.ProgressMessage);
        Assert.Equal(75, context.ViewModel.OperationStatus.ProgressValue);
    }

    [Fact]
    public void Inactive_Document_Progress_Should_Not_Raise_Footer_Status_Change()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();

        firstTab.Document.OperationStatus.SetStatus("First active status.", StatusSeverity.Info);
        context.WorkspaceTabs.SelectTab(firstTab);

        List<string?> changedProperties = [];
        context.ViewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        secondTab.Document.OperationStatus.SetStatus("Second inactive status.", StatusSeverity.Success);
        secondTab.Document.OperationStatus.ShowDeterminateProgress("Second inactive progress.", 40);

        Assert.DoesNotContain(nameof(MainViewModel.FooterStatusMessage), changedProperties);
        Assert.DoesNotContain(nameof(MainViewModel.FooterSeverity), changedProperties);
        Assert.Contains("First active status.", context.ViewModel.FooterStatusMessage);

        context.WorkspaceTabs.SelectTab(secondTab);

        Assert.Contains("Second inactive status.", context.ViewModel.FooterStatusMessage);
        Assert.Equal("Second inactive progress.", context.ViewModel.OperationStatus.ProgressMessage);
    }

    [Fact]
    public void Completed_Inactive_Operation_Should_Update_Only_Its_Document_Status()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();

        firstTab.Document.OperationStatus.SetStatus("First tab ready.", StatusSeverity.Info);
        secondTab.Document.OperationStatus.IsBusy = true;
        secondTab.Document.OperationStatus.ShowIndeterminateProgress("Second tab running.");

        context.WorkspaceTabs.SelectTab(firstTab);

        secondTab.Document.OperationStatus.HideProgress();
        secondTab.Document.OperationStatus.IsBusy = false;
        secondTab.Document.OperationStatus.SetStatus("Second tab completed.", StatusSeverity.Success);

        Assert.Contains("First tab ready.", context.ViewModel.FooterStatusMessage);

        context.WorkspaceTabs.SelectTab(secondTab);

        Assert.Contains("Second tab completed.", context.ViewModel.FooterStatusMessage);
    }

    [Fact]
    public void CancelCommand_Should_Target_Active_Document()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();

        CancellationToken firstToken = firstTab.Document.BeginPreviewBuild();
        CancellationToken secondToken = secondTab.Document.BeginPreviewBuild();

        firstTab.Document.OperationStatus.IsCancelable = true;
        secondTab.Document.OperationStatus.IsCancelable = true;

        context.WorkspaceTabs.SelectTab(secondTab);

        context.ViewModel.OperationStatus.CancelCommand.Execute(null);

        Assert.False(firstToken.IsCancellationRequested);
        Assert.True(secondToken.IsCancellationRequested);

        firstTab.Document.CompletePreviewBuild();
        secondTab.Document.CompletePreviewBuild();
    }

    [Fact]
    public void SwitchingActiveTab_Should_Raise_CommandAvailability()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();

        context.WorkspaceTabs.SelectTab(firstTab);

        int buildPreviewChanges = 0;
        int saveChanges = 0;
        int saveWorkspaceChanges = 0;
        int copyPreviewChanges = 0;
        int openOutputFolderChanges = 0;

        context.ViewModel.BuildPreviewCommand.CanExecuteChanged += (_, _) => buildPreviewChanges++;
        context.ViewModel.SaveCommand.CanExecuteChanged += (_, _) => saveChanges++;
        context.ViewModel.SaveWorkspaceCommand.CanExecuteChanged += (_, _) => saveWorkspaceChanges++;
        context.ViewModel.CopyPreviewCommand.CanExecuteChanged += (_, _) => copyPreviewChanges++;
        context.ViewModel.OpenOutputFolderCommand.CanExecuteChanged += (_, _) => openOutputFolderChanges++;

        context.WorkspaceTabs.SelectTab(secondTab);

        Assert.True(buildPreviewChanges > 0);
        Assert.True(saveChanges > 0);
        Assert.True(saveWorkspaceChanges > 0);
        Assert.True(copyPreviewChanges > 0);
        Assert.True(openOutputFolderChanges > 0);
    }

    [Fact]
    public void BuildPreviewCommand_Should_Keep_Target_When_ActiveTab_Changes_During_Command()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel secondDocument = secondTab.Document;

        context.WorkspaceTabs.SelectTab(secondTab);

        context.PreviewService.OnBuildPreviewAsync = (_, _, _) =>
        {
            context.WorkspaceTabs.SelectTab(firstTab);
            return Task.CompletedTask;
        };

        context.ViewModel.BuildPreviewCommand.Execute(null);

        Assert.Same(secondDocument, context.PreviewService.LastBuildPreviewDocument);
        Assert.Same(firstTab.Document, context.ViewModel.CurrentDocument);
    }

    [Fact]
    public void SaveWorkspaceAsCommand_Should_Use_ActiveTab_Document()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel firstDocument = context.ViewModel.CurrentDocument;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel secondDocument = secondTab.Document;

        context.WorkspaceTabs.SelectTab(secondTab);

        context.ViewModel.SaveWorkspaceAsCommand.Execute(null);

        Assert.Same(secondDocument, context.LifecycleService.LastSaveWorkspaceAsDocument);
        Assert.NotSame(firstDocument, context.LifecycleService.LastSaveWorkspaceAsDocument);
    }

    [Fact]
    public void DuplicateWorkspaceTabCommand_Should_Duplicate_Active_Tab()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel sourceDocument = context.ViewModel.CurrentDocument;
        sourceDocument.SessionSettings.SessionName = "Source Workspace";
        sourceDocument.SessionSettings.OutputPath = @"D:\Output\source.txt";
        sourceDocument.WorkspaceFilePath = @"D:\Workspaces\source.filemerger.workspace.json";

        context.ViewModel.DuplicateWorkspaceTabCommand.Execute(null);

        Assert.Equal(2, context.WorkspaceTabs.Tabs.Count);
        Assert.NotSame(sourceDocument, context.ViewModel.CurrentDocument);
        Assert.Equal("Source Workspace Copy", context.ViewModel.CurrentDocument.SessionSettings.SessionName);
        Assert.Equal(@"D:\Output\source.txt", context.ViewModel.CurrentDocument.SessionSettings.OutputPath);
        Assert.Null(context.ViewModel.CurrentDocument.WorkspaceFilePath);
    }

    [Fact]
    public void DuplicateWorkspaceTabCommand_Should_Not_Modify_Source_Document()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel sourceDocument = context.ViewModel.CurrentDocument;
        sourceDocument.SessionSettings.SessionName = "Source Workspace";
        sourceDocument.SessionSettings.OutputPath = @"D:\Output\source.txt";
        sourceDocument.WorkspaceFilePath = @"D:\Workspaces\source.filemerger.workspace.json";

        context.ViewModel.DuplicateWorkspaceTabCommand.Execute(null);

        context.ViewModel.CurrentDocument.SessionSettings.OutputPath = @"D:\Output\duplicate.txt";

        Assert.Equal("Source Workspace", sourceDocument.SessionSettings.SessionName);
        Assert.Equal(@"D:\Output\source.txt", sourceDocument.SessionSettings.OutputPath);
        Assert.Equal(@"D:\Workspaces\source.filemerger.workspace.json", sourceDocument.WorkspaceFilePath);
    }

    [Fact]
    public void RenameCurrentWorkspaceTab_Should_Rename_Active_Tab()
    {
        TestContext context = CreateContext();

        bool result = context.ViewModel.RenameCurrentWorkspaceTab("Renamed Workspace");

        Assert.True(result);
        Assert.Equal("Renamed Workspace", context.ViewModel.CurrentDocument.SessionSettings.SessionName);
        Assert.Equal("Renamed Workspace", context.WorkspaceTabs.ActiveTab.Title);
    }

    [Fact]
    public void RenameCurrentWorkspaceTab_Should_Not_Change_WorkspaceFilePath()
    {
        TestContext context = CreateContext();

        context.ViewModel.CurrentDocument.WorkspaceFilePath = @"D:\Workspaces\source.filemerger.workspace.json";

        bool result = context.ViewModel.RenameCurrentWorkspaceTab("Renamed Workspace");

        Assert.True(result);
        Assert.Equal(
            @"D:\Workspaces\source.filemerger.workspace.json",
            context.ViewModel.CurrentDocument.WorkspaceFilePath);
    }

    [Fact]
    public void RenameCurrentWorkspaceTab_Should_Return_False_When_Name_Is_Empty()
    {
        TestContext context = CreateContext();

        string originalName = context.ViewModel.CurrentDocument.SessionSettings.SessionName;

        bool result = context.ViewModel.RenameCurrentWorkspaceTab(" ");

        Assert.False(result);
        Assert.Equal(originalName, context.ViewModel.CurrentDocument.SessionSettings.SessionName);
    }

    [Fact]
    public void RenameWorkspaceTabCommand_Should_Open_Rename_Dialog_For_Active_Tab()
    {
        TestContext context = CreateContext();

        context.RenameDialogService.NextName = "Renamed Workspace";

        context.ViewModel.RenameWorkspaceTabCommand.Execute(null);

        Assert.Same(context.WorkspaceTabs.ActiveTab, context.RenameDialogService.LastRequestedTab);
    }

    [Fact]
    public void RenameWorkspaceTabCommand_Should_Rename_Active_Tab_When_Dialog_Returns_Name()
    {
        TestContext context = CreateContext();

        context.RenameDialogService.NextName = "Renamed Workspace";

        context.ViewModel.RenameWorkspaceTabCommand.Execute(null);

        Assert.Equal("Renamed Workspace", context.ViewModel.CurrentDocument.SessionSettings.SessionName);
        Assert.Equal("Renamed Workspace", context.WorkspaceTabs.ActiveTab.Title);
    }

    [Fact]
    public void RenameWorkspaceTabCommand_Should_Trim_Dialog_Name()
    {
        TestContext context = CreateContext();

        context.RenameDialogService.NextName = "  Renamed Workspace  ";

        context.ViewModel.RenameWorkspaceTabCommand.Execute(null);

        Assert.Equal("Renamed Workspace", context.ViewModel.CurrentDocument.SessionSettings.SessionName);
        Assert.Equal("Renamed Workspace", context.WorkspaceTabs.ActiveTab.Title);
    }

    [Fact]
    public void RenameWorkspaceTabCommand_Should_Not_Rename_When_Dialog_Returns_Null()
    {
        TestContext context = CreateContext();

        string originalName = context.ViewModel.CurrentDocument.SessionSettings.SessionName;
        context.RenameDialogService.NextName = null;

        context.ViewModel.RenameWorkspaceTabCommand.Execute(null);

        Assert.Equal(originalName, context.ViewModel.CurrentDocument.SessionSettings.SessionName);
    }

    [Fact]
    public void RenameWorkspaceTabCommand_Should_Not_Rename_When_Dialog_Returns_Whitespace()
    {
        TestContext context = CreateContext();

        string originalName = context.ViewModel.CurrentDocument.SessionSettings.SessionName;
        context.RenameDialogService.NextName = "   ";

        context.ViewModel.RenameWorkspaceTabCommand.Execute(null);

        Assert.Equal(originalName, context.ViewModel.CurrentDocument.SessionSettings.SessionName);
    }

    [Fact]
    public void RenameWorkspaceTabCommand_Should_Not_Change_WorkspaceFilePath()
    {
        TestContext context = CreateContext();

        context.ViewModel.CurrentDocument.WorkspaceFilePath = @"D:\Workspaces\source.filemerger.workspace.json";

        context.RenameDialogService.NextName = "Renamed Workspace";

        context.ViewModel.RenameWorkspaceTabCommand.Execute(null);

        Assert.Equal(
            @"D:\Workspaces\source.filemerger.workspace.json",
            context.ViewModel.CurrentDocument.WorkspaceFilePath);
    }

    [Fact]
    public void RenameWorkspaceTabCommand_Should_Not_Affect_Other_Tabs()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel firstDocument = context.ViewModel.CurrentDocument;
        firstDocument.SessionSettings.SessionName = "First Workspace";

        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        secondTab.Document.SessionSettings.SessionName = "Second Workspace";

        context.WorkspaceTabs.SelectTab(secondTab);
        context.RenameDialogService.NextName = "Renamed Second Workspace";

        context.ViewModel.RenameWorkspaceTabCommand.Execute(null);

        Assert.Equal("First Workspace", firstDocument.SessionSettings.SessionName);
        Assert.Equal("Renamed Second Workspace", secondTab.Document.SessionSettings.SessionName);
    }

    [Fact]
    public void RenameWorkspaceTabCommand_Should_Not_Mark_Dirty_When_Name_Is_Unchanged()
    {
        TestContext context = CreateContext();

        context.ViewModel.CurrentDocument.SessionSettings.SessionName = "Current Workspace";
        context.DirtyStateService.MarkWorkspaceSaved(context.ViewModel.CurrentDocument);

        context.RenameDialogService.NextName = "Current Workspace";

        context.ViewModel.RenameWorkspaceTabCommand.Execute(null);

        Assert.False(context.ViewModel.CurrentDocument.WorkspaceDirtyTracker.IsWorkspaceDirty);
    }

    [Fact]
    public void RenameWorkspaceTabCommand_Should_Be_Disabled_When_Active_Document_Is_Busy()
    {
        TestContext context = CreateContext();

        context.ViewModel.CurrentDocument.OperationStatus.IsBusy = true;

        Assert.False(context.ViewModel.RenameWorkspaceTabCommand.CanExecute(null));
    }

    [Fact]
    public void SelectNextWorkspaceTabCommand_Should_Be_Disabled_When_Only_One_Tab_Exists()
    {
        TestContext context = CreateContext();

        Assert.False(context.ViewModel.SelectNextWorkspaceTabCommand.CanExecute(null));
    }

    [Fact]
    public void SelectNextWorkspaceTabCommand_Should_Be_Enabled_When_Multiple_Tabs_Exist()
    {
        TestContext context = CreateContext();

        context.WorkspaceTabs.CreateNewTab();

        Assert.True(context.ViewModel.SelectNextWorkspaceTabCommand.CanExecute(null));
    }

    [Fact]
    public void SelectNextWorkspaceTabCommand_Should_Select_Next_Tab()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceTabViewModel thirdTab = context.WorkspaceTabs.CreateNewTab();

        context.WorkspaceTabs.SelectTab(firstTab);

        context.ViewModel.SelectNextWorkspaceTabCommand.Execute(null);

        Assert.Equal(secondTab, context.WorkspaceTabs.ActiveTab);
        Assert.Same(secondTab.Document, context.ViewModel.CurrentDocument);
        Assert.False(thirdTab.IsActive);
    }

    [Fact]
    public void SelectNextWorkspaceTabCommand_Should_Wrap_To_First_Tab()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        context.WorkspaceTabs.CreateNewTab();
        WorkspaceTabViewModel thirdTab = context.WorkspaceTabs.CreateNewTab();

        context.WorkspaceTabs.SelectTab(thirdTab);

        context.ViewModel.SelectNextWorkspaceTabCommand.Execute(null);

        Assert.Equal(firstTab, context.WorkspaceTabs.ActiveTab);
        Assert.Same(firstTab.Document, context.ViewModel.CurrentDocument);
    }

    [Fact]
    public void SelectPreviousWorkspaceTabCommand_Should_Be_Disabled_When_Only_One_Tab_Exists()
    {
        TestContext context = CreateContext();

        Assert.False(context.ViewModel.SelectPreviousWorkspaceTabCommand.CanExecute(null));
    }

    [Fact]
    public void SelectPreviousWorkspaceTabCommand_Should_Be_Enabled_When_Multiple_Tabs_Exist()
    {
        TestContext context = CreateContext();

        context.WorkspaceTabs.CreateNewTab();

        Assert.True(context.ViewModel.SelectPreviousWorkspaceTabCommand.CanExecute(null));
    }

    [Fact]
    public void SelectPreviousWorkspaceTabCommand_Should_Select_Previous_Tab()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceTabViewModel thirdTab = context.WorkspaceTabs.CreateNewTab();

        context.WorkspaceTabs.SelectTab(thirdTab);

        context.ViewModel.SelectPreviousWorkspaceTabCommand.Execute(null);

        Assert.Equal(secondTab, context.WorkspaceTabs.ActiveTab);
        Assert.Same(secondTab.Document, context.ViewModel.CurrentDocument);
        Assert.False(firstTab.IsActive);
    }

    [Fact]
    public void SelectPreviousWorkspaceTabCommand_Should_Wrap_To_Last_Tab()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        context.WorkspaceTabs.CreateNewTab();
        WorkspaceTabViewModel thirdTab = context.WorkspaceTabs.CreateNewTab();

        context.WorkspaceTabs.SelectTab(firstTab);

        context.ViewModel.SelectPreviousWorkspaceTabCommand.Execute(null);

        Assert.Equal(thirdTab, context.WorkspaceTabs.ActiveTab);
        Assert.Same(thirdTab.Document, context.ViewModel.CurrentDocument);
    }

    [Fact]
    public void CloseOtherWorkspaceTabsCommand_Should_Be_Disabled_When_Only_One_Tab_Exists()
    {
        TestContext context = CreateContext();

        Assert.False(context.ViewModel.CloseOtherWorkspaceTabsCommand.CanExecute(null));
    }

    [Fact]
    public void CloseOtherWorkspaceTabsCommand_Should_Be_Enabled_When_Multiple_Tabs_Exist()
    {
        TestContext context = CreateContext();

        context.WorkspaceTabs.CreateNewTab();

        Assert.True(context.ViewModel.CloseOtherWorkspaceTabsCommand.CanExecute(null));
    }

    [Fact]
    public void CloseOtherWorkspaceTabsCommand_Should_Close_Inactive_Tabs()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceTabViewModel thirdTab = context.WorkspaceTabs.CreateNewTab();

        context.WorkspaceTabs.SelectTab(activeTab);

        context.ViewModel.CloseOtherWorkspaceTabsCommand.Execute(null);

        Assert.Single(context.WorkspaceTabs.Tabs);
        Assert.Contains(activeTab, context.WorkspaceTabs.Tabs);
        Assert.DoesNotContain(firstTab, context.WorkspaceTabs.Tabs);
        Assert.DoesNotContain(thirdTab, context.WorkspaceTabs.Tabs);
    }

    [Fact]
    public void CloseOtherWorkspaceTabsCommand_Should_Keep_Active_Document()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel firstDocument = context.ViewModel.CurrentDocument;
        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel activeDocument = activeTab.Document;

        context.WorkspaceTabs.CreateNewTab();
        context.WorkspaceTabs.SelectTab(activeTab);

        context.ViewModel.CloseOtherWorkspaceTabsCommand.Execute(null);

        Assert.Same(activeDocument, context.ViewModel.CurrentDocument);
        Assert.NotSame(firstDocument, context.ViewModel.CurrentDocument);
    }

    [Fact]
    public void CloseOtherWorkspaceTabsCommand_Should_Be_Disabled_When_Active_Document_Is_Busy()
    {
        TestContext context = CreateContext();

        context.WorkspaceTabs.CreateNewTab();
        context.ViewModel.CurrentDocument.OperationStatus.IsBusy = true;

        Assert.False(context.ViewModel.CloseOtherWorkspaceTabsCommand.CanExecute(null));
    }

    [Fact]
    public async Task CloseWorkspaceTabAsync_Should_Close_Clean_Tab_Without_Confirmation()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel cleanTab = context.WorkspaceTabs.CreateNewTab();

        context.WorkspaceTabs.SelectTab(activeTab);

        bool result = await context.ViewModel.CloseWorkspaceTabAsync(cleanTab);

        Assert.True(result);
        Assert.DoesNotContain(cleanTab, context.WorkspaceTabs.Tabs);
        Assert.Equal(0, context.PromptService.ConfirmUnsavedChangesCalls);
    }

    [Fact]
    public async Task CloseWorkspaceTabAsync_Should_Keep_Dirty_Tab_When_User_Cancels()
    {
        TestContext context = CreateContext();
        context.PromptService.UnsavedChangesDecision = UnsavedChangesDecision.Cancel;

        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel dirtyTab = context.WorkspaceTabs.CreateNewTab();

        MarkWorkspaceDirty(dirtyTab.Document);
        context.WorkspaceTabs.SelectTab(activeTab);

        bool result = await context.ViewModel.CloseWorkspaceTabAsync(dirtyTab);

        Assert.False(result);
        Assert.Contains(dirtyTab, context.WorkspaceTabs.Tabs);
        Assert.Equal(1, context.PromptService.ConfirmUnsavedChangesCalls);
    }

    [Fact]
    public async Task CloseWorkspaceTabAsync_Should_Close_Dirty_Tab_When_User_Discards()
    {
        TestContext context = CreateContext();
        context.PromptService.UnsavedChangesDecision = UnsavedChangesDecision.Discard;

        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel dirtyTab = context.WorkspaceTabs.CreateNewTab();

        MarkWorkspaceDirty(dirtyTab.Document);
        context.WorkspaceTabs.SelectTab(activeTab);

        bool result = await context.ViewModel.CloseWorkspaceTabAsync(dirtyTab);

        Assert.True(result);
        Assert.DoesNotContain(dirtyTab, context.WorkspaceTabs.Tabs);
        Assert.Equal(1, context.PromptService.ConfirmUnsavedChangesCalls);
        Assert.Null(context.LifecycleService.LastSaveWorkspaceDocument);
    }

    [Fact]
    public async Task CloseWorkspaceTabAsync_Should_Save_And_Close_Dirty_Tab_When_User_Saves()
    {
        TestContext context = CreateContext();
        context.PromptService.UnsavedChangesDecision = UnsavedChangesDecision.Save;

        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel dirtyTab = context.WorkspaceTabs.CreateNewTab();

        MarkWorkspaceDirty(dirtyTab.Document);

        context.LifecycleService.OnSave = WorkspaceDocumentTestFactory.MarkWorkspaceSaved;

        context.WorkspaceTabs.SelectTab(activeTab);

        bool result = await context.ViewModel.CloseWorkspaceTabAsync(dirtyTab);

        Assert.True(result);
        Assert.DoesNotContain(dirtyTab, context.WorkspaceTabs.Tabs);
        Assert.Same(dirtyTab.Document, context.LifecycleService.LastSaveWorkspaceDocument);
    }

    [Fact]
    public async Task CloseWorkspaceTabAsync_Should_Keep_Dirty_Tab_When_Save_Is_Canceled()
    {
        TestContext context = CreateContext();

        context.PromptService.UnsavedChangesDecision = UnsavedChangesDecision.Save;
        context.LifecycleService.SaveResult = false;

        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel dirtyTab = context.WorkspaceTabs.CreateNewTab();

        MarkWorkspaceDirty(dirtyTab.Document);
        context.WorkspaceTabs.SelectTab(activeTab);

        bool result = await context.ViewModel.CloseWorkspaceTabAsync(dirtyTab);

        Assert.False(result);
        Assert.Contains(dirtyTab, context.WorkspaceTabs.Tabs);
        Assert.Same(dirtyTab.Document, context.LifecycleService.LastSaveWorkspaceDocument);
    }

    [Fact]
    public async Task CloseWorkspaceTabAsync_Should_Show_Tab_Name_In_UnsavedChangesPrompt()
    {
        TestContext context = CreateContext();
        context.PromptService.UnsavedChangesDecision = UnsavedChangesDecision.Cancel;

        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel dirtyTab = context.WorkspaceTabs.CreateNewTab();

        dirtyTab.Document.SessionSettings.SessionName = "Dirty Routing Tab";
        MarkWorkspaceDirty(dirtyTab.Document);

        context.WorkspaceTabs.SelectTab(activeTab);

        bool result = await context.ViewModel.CloseWorkspaceTabAsync(dirtyTab);

        Assert.False(result);
        Assert.Equal("Unsaved workspace", context.PromptService.LastUnsavedChangesTitle);
        Assert.NotNull(context.PromptService.LastUnsavedChangesMessage);
        Assert.Contains("Dirty Routing Tab", context.PromptService.LastUnsavedChangesMessage);
    }

    [Fact]
    public void DuplicateWorkspaceTabCommand_Should_Be_Disabled_When_Active_Document_Is_Busy()
    {
        TestContext context = CreateContext();

        context.ViewModel.CurrentDocument.OperationStatus.IsBusy = true;

        Assert.False(context.ViewModel.DuplicateWorkspaceTabCommand.CanExecute(null));
    }

    [Fact]
    public void CloseActiveWorkspaceTabCommand_Should_Be_Disabled_When_Active_Document_Is_Busy()
    {
        TestContext context = CreateContext();

        context.WorkspaceTabs.CreateNewTab();
        context.ViewModel.CurrentDocument.OperationStatus.IsBusy = true;

        Assert.False(context.ViewModel.CloseActiveWorkspaceTabCommand.CanExecute(null));
    }

    [Fact]
    public void CloseOtherWorkspaceTabsCommand_Should_Be_Disabled_When_Inactive_Tab_Is_Busy()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel inactiveBusyTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.CreateNewTab();

        inactiveBusyTab.Document.OperationStatus.IsBusy = true;
        context.WorkspaceTabs.SelectTab(activeTab);

        Assert.False(context.ViewModel.CloseOtherWorkspaceTabsCommand.CanExecute(null));
    }

    [Fact]
    public async Task CloseWorkspaceTabAsync_Should_Not_Close_Busy_Inactive_Tab()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel busyTab = context.WorkspaceTabs.CreateNewTab();

        busyTab.Document.OperationStatus.IsBusy = true;
        context.WorkspaceTabs.SelectTab(activeTab);

        bool result = await context.ViewModel.CloseWorkspaceTabAsync(busyTab);

        Assert.False(result);
        Assert.Contains(busyTab, context.WorkspaceTabs.Tabs);
        Assert.Equal(0, context.PromptService.ConfirmUnsavedChangesCalls);
        Assert.Null(context.LifecycleService.LastSaveWorkspaceDocument);
    }

    [Fact]
    public void CloseOtherWorkspaceTabsCommand_Should_Not_Close_Tabs_When_Inactive_Tab_Is_Busy()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel busyInactiveTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.CreateNewTab();

        busyInactiveTab.Document.OperationStatus.IsBusy = true;
        context.WorkspaceTabs.SelectTab(activeTab);

        context.ViewModel.CloseOtherWorkspaceTabsCommand.Execute(null);

        Assert.Equal(3, context.WorkspaceTabs.Tabs.Count);
        Assert.Contains(firstTab, context.WorkspaceTabs.Tabs);
        Assert.Contains(busyInactiveTab, context.WorkspaceTabs.Tabs);
        Assert.Contains(activeTab, context.WorkspaceTabs.Tabs);
        Assert.Equal(activeTab, context.WorkspaceTabs.ActiveTab);
    }

    [Fact]
    public void CommandAvailability_Should_Update_When_Active_Document_Busy_State_Changes()
    {
        TestContext context = CreateContext();

        context.WorkspaceTabs.CreateNewTab();

        int closeActiveChanges = 0;
        int duplicateChanges = 0;
        int renameChanges = 0;

        context.ViewModel.CloseActiveWorkspaceTabCommand.CanExecuteChanged += (_, _) => closeActiveChanges++;
        context.ViewModel.DuplicateWorkspaceTabCommand.CanExecuteChanged += (_, _) => duplicateChanges++;
        context.ViewModel.RenameWorkspaceTabCommand.CanExecuteChanged += (_, _) => renameChanges++;

        context.ViewModel.CurrentDocument.OperationStatus.IsBusy = true;

        Assert.True(closeActiveChanges > 0);
        Assert.True(duplicateChanges > 0);
        Assert.True(renameChanges > 0);
        Assert.False(context.ViewModel.CloseActiveWorkspaceTabCommand.CanExecute(null));
        Assert.False(context.ViewModel.DuplicateWorkspaceTabCommand.CanExecute(null));
        Assert.False(context.ViewModel.RenameWorkspaceTabCommand.CanExecute(null));
    }

    [Fact]
    public void CommandAvailability_Should_Update_When_Inactive_Tab_Busy_State_Changes()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel inactiveTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel activeTab = context.WorkspaceTabs.CreateNewTab();

        context.WorkspaceTabs.SelectTab(activeTab);

        int closeOtherChanges = 0;
        context.ViewModel.CloseOtherWorkspaceTabsCommand.CanExecuteChanged += (_, _) => closeOtherChanges++;

        inactiveTab.Document.OperationStatus.IsBusy = true;

        Assert.True(closeOtherChanges > 0);
        Assert.False(context.ViewModel.CloseOtherWorkspaceTabsCommand.CanExecute(null));
    }

    [Fact]
    public void CopyPreviewCommand_Should_Copy_ActiveTab_LastOutput_Content()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel firstDocument = context.ViewModel.CurrentDocument;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel secondDocument = secondTab.Document;

        firstDocument.SetLastOutput(CreateOutput("first output"));
        secondDocument.SetLastOutput(CreateOutput("second output"));

        context.WorkspaceTabs.SelectTab(secondTab);

        context.ViewModel.CopyPreviewCommand.Execute(null);

        Assert.Equal("second output", context.ClipboardService.LastText);
        Assert.Equal(1, context.ClipboardService.SetTextCallCount);
    }

    [Fact]
    public void CopyPreviewCommand_Should_Copy_Full_Output_Not_Displayed_PreviewContent()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel document = context.ViewModel.CurrentDocument;

        document.SetLastOutput(CreateOutput("full output"));
        document.PreviewContent = "truncated preview";

        context.ViewModel.CopyPreviewCommand.Execute(null);

        Assert.Equal("full output", context.ClipboardService.LastText);
    }

    [Fact]
    public void CopyPreviewCommand_CanExecute_Should_Require_LastOutput_Content_And_NotBusy()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel document = context.ViewModel.CurrentDocument;

        Assert.False(context.ViewModel.CopyPreviewCommand.CanExecute(null));

        document.SetLastOutput(CreateOutput());

        Assert.True(context.ViewModel.CopyPreviewCommand.CanExecute(null));

        document.OperationStatus.IsBusy = true;

        Assert.False(context.ViewModel.CopyPreviewCommand.CanExecute(null));
    }

    [Fact]
    public void OpenOutputFolderCommand_Should_Use_ActiveTab_Document()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel firstDocument = context.ViewModel.CurrentDocument;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel secondDocument = secondTab.Document;

        context.WorkspaceTabs.SelectTab(secondTab);

        context.ViewModel.OpenOutputFolderCommand.Execute(null);

        Assert.Same(secondDocument, context.OutputService.LastOpenOutputFolderDocument);
        Assert.NotSame(firstDocument, context.OutputService.LastOpenOutputFolderDocument);
        Assert.Equal(1, context.OutputService.OpenOutputFolderCalls);
    }

    [Fact]
    public void OpenOutputFolderCommand_CanExecute_Should_Use_OutputService_And_BusyState()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel document = context.ViewModel.CurrentDocument;

        context.OutputService.CanOpenOutputFolderResult = false;
        Assert.False(context.ViewModel.OpenOutputFolderCommand.CanExecute(null));

        context.OutputService.CanOpenOutputFolderResult = true;
        Assert.True(context.ViewModel.OpenOutputFolderCommand.CanExecute(null));

        document.OperationStatus.IsBusy = true;
        Assert.False(context.ViewModel.OpenOutputFolderCommand.CanExecute(null));
    }

    [Fact]
    public void TogglePreviewSummaryCommand_Should_Toggle_ActiveTab_Summary_State()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();

        Assert.True(firstTab.Document.IsPreviewSummaryExpanded);
        Assert.True(secondTab.Document.IsPreviewSummaryExpanded);

        context.WorkspaceTabs.SelectTab(secondTab);

        context.ViewModel.TogglePreviewSummaryCommand.Execute(null);

        Assert.True(firstTab.Document.IsPreviewSummaryExpanded);
        Assert.False(secondTab.Document.IsPreviewSummaryExpanded);

        context.WorkspaceTabs.SelectTab(firstTab);

        Assert.True(context.ViewModel.IsPreviewSummaryExpanded);
        Assert.False(context.ViewModel.IsPreviewSummaryCollapsed);
    }

    [Fact]
    public void IsPreviewLineWrapEnabled_Should_Update_ActiveTab_Document()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();

        context.WorkspaceTabs.SelectTab(firstTab);

        Assert.False(firstTab.Document.IsPreviewLineWrapEnabled);
        Assert.False(secondTab.Document.IsPreviewLineWrapEnabled);
        Assert.Same(firstTab.Document, context.ViewModel.CurrentDocument);

        context.ViewModel.IsPreviewLineWrapEnabled = true;

        Assert.True(firstTab.Document.IsPreviewLineWrapEnabled);
        Assert.False(secondTab.Document.IsPreviewLineWrapEnabled);

        context.WorkspaceTabs.SelectTab(secondTab);

        Assert.False(context.ViewModel.IsPreviewLineWrapEnabled);

        context.ViewModel.IsPreviewLineWrapEnabled = true;

        Assert.True(firstTab.Document.IsPreviewLineWrapEnabled);
        Assert.True(secondTab.Document.IsPreviewLineWrapEnabled);
    }

    [Fact]
    public void IsPreviewLineWrapEnabled_Setter_Should_Save_ApplicationPreference()
    {
        TestContext context = CreateContext();
        ApplicationPreferences original = new(
            isPreviewLineWrapEnabledByDefault: false,
            previewDisplayCharacterLimit: 20_000,
            crashLogRetentionLimit: 50);
        context.ApplicationPreferencesStore.SetCurrent(original);
        bool wasWorkspaceDirty = context.ViewModel.CurrentDocument.IsWorkspaceDirty;
        bool wasPreviewDirty = context.ViewModel.CurrentDocument.PreviewDirtyTracker.IsPreviewDirty;

        context.ViewModel.IsPreviewLineWrapEnabled = true;

        Assert.True(context.ViewModel.CurrentDocument.IsPreviewLineWrapEnabled);
        Assert.True(context.ApplicationPreferencesStore.Current.IsPreviewLineWrapEnabledByDefault);
        Assert.Equal(
            original.PreviewDisplayCharacterLimit,
            context.ApplicationPreferencesStore.Current.PreviewDisplayCharacterLimit);
        Assert.Equal(
            original.CrashLogRetentionLimit,
            context.ApplicationPreferencesStore.Current.CrashLogRetentionLimit);
        Assert.Equal(wasWorkspaceDirty, context.ViewModel.CurrentDocument.IsWorkspaceDirty);
        Assert.Equal(wasPreviewDirty, context.ViewModel.CurrentDocument.PreviewDirtyTracker.IsPreviewDirty);
    }

    [Fact]
    public void SwitchingActiveTab_Should_Refresh_Preview_Line_Wrap_Binding()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();

        firstTab.Document.IsPreviewLineWrapEnabled = true;
        secondTab.Document.IsPreviewLineWrapEnabled = false;

        context.WorkspaceTabs.SelectTab(firstTab);

        List<string?> changedProperties = [];
        context.ViewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        context.WorkspaceTabs.SelectTab(secondTab);

        Assert.False(context.ViewModel.IsPreviewLineWrapEnabled);
        Assert.Contains(nameof(MainViewModel.IsPreviewLineWrapEnabled), changedProperties);
    }

    [Fact]
    public void OpenKeyboardShortcutsCommand_Should_Show_Dialog()
    {
        TestContext context = CreateContext();

        Assert.True(context.ViewModel.OpenKeyboardShortcutsCommand.CanExecute(null));

        context.ViewModel.OpenKeyboardShortcutsCommand.Execute(null);

        Assert.Equal(1, context.KeyboardShortcutsDialogService.ShowDialogCalls);
    }

    [Fact]
    public void OpenUpdateCheckCommand_Should_Show_ApplicationLevel_Dialog()
    {
        TestContext context = CreateContext();
        WorkspaceDocumentViewModel document = context.ViewModel.CurrentDocument;

        Assert.True(context.ViewModel.OpenUpdateCheckCommand.CanExecute(null));

        context.ViewModel.OpenUpdateCheckCommand.Execute(null);

        Assert.Equal(1, context.UpdateCheckDialogService.ShowDialogCalls);
        Assert.Same(document, context.ViewModel.CurrentDocument);
    }

    [Fact]
    public void OpenWorkspaceConfigurationCommand_Should_Use_ActiveTab_Document()
    {
        TestContext context = CreateContext();

        WorkspaceDocumentViewModel firstDocument = context.ViewModel.CurrentDocument;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();
        WorkspaceDocumentViewModel secondDocument = secondTab.Document;

        context.WorkspaceTabs.SelectTab(secondTab);

        context.ViewModel.OpenWorkspaceConfigurationCommand.Execute(null);

        Assert.Equal(1, context.WorkspaceConfigurationDialogService.ShowDialogCalls);
        Assert.Same(secondDocument, context.WorkspaceConfigurationDialogService.LastDocument);
        Assert.NotSame(firstDocument, context.WorkspaceConfigurationDialogService.LastDocument);
    }

    [Fact]
    public void OpenWorkspaceConfigurationCommand_Should_Be_Disabled_When_Active_Document_Is_Busy()
    {
        TestContext context = CreateContext();
        context.ViewModel.CurrentDocument.OperationStatus.IsBusy = true;

        Assert.False(context.ViewModel.OpenWorkspaceConfigurationCommand.CanExecute(null));

        context.ViewModel.OpenWorkspaceConfigurationCommand.Execute(null);

        Assert.Equal(0, context.WorkspaceConfigurationDialogService.ShowDialogCalls);
    }

    [Fact]
    public void OpenWorkspaceConfigurationCommand_Should_Refresh_Current_Profile_Card_After_Apply()
    {
        TestContext context = CreateContext();
        context.WorkspaceConfigurationDialogService.OnShowDialog = document =>
        {
            document.ProfileEditor.WorkingProfileName = "Configured Profile";
            document.CurrentProfileEntryId = null;
            document.ProfileOriginEntryId = "origin-profile";
            document.ProfileOriginDisplayName = "Origin Profile";
            return true;
        };

        context.ViewModel.OpenWorkspaceConfigurationCommand.Execute(null);

        Assert.Equal("Configured Profile", context.ViewModel.CurrentProfileCard.ProfileName);
        Assert.Null(context.ViewModel.CurrentProfileCard.ProfileEntryId);
        Assert.Equal("origin-profile", context.ViewModel.CurrentProfileCard.ProfileOriginEntryId);
        Assert.Equal("Origin Profile", context.ViewModel.CurrentProfileCard.ProfileOriginDisplayName);
        Assert.Equal(
            "Custom workspace profile based on 'Origin Profile'",
            context.ViewModel.CurrentProfileCard.ProfileSourceText);
    }

    [Fact]
    public void OpenWorkspaceConfigurationCommand_Should_Not_Refresh_Dirty_State_After_Cancel()
    {
        TestContext context = CreateContext();
        context.WorkspaceConfigurationDialogService.Result = false;
        int workspaceRefreshCount = context.DirtyStateService.WorkspaceRefreshedDocuments.Count;
        int previewRefreshCount = context.DirtyStateService.PreviewRefreshedDocuments.Count;

        context.ViewModel.OpenWorkspaceConfigurationCommand.Execute(null);

        Assert.Equal(workspaceRefreshCount, context.DirtyStateService.WorkspaceRefreshedDocuments.Count);
        Assert.Equal(previewRefreshCount, context.DirtyStateService.PreviewRefreshedDocuments.Count);
    }

    [Fact]
    public void OpenWorkspaceConfigurationCommand_Should_Keep_Original_Target_When_Active_Tab_Changes()
    {
        TestContext context = CreateContext();

        WorkspaceTabViewModel firstTab = context.WorkspaceTabs.ActiveTab;
        WorkspaceTabViewModel secondTab = context.WorkspaceTabs.CreateNewTab();

        context.WorkspaceTabs.SelectTab(firstTab);
        context.WorkspaceConfigurationDialogService.OnShowDialog = _ =>
        {
            context.WorkspaceTabs.SelectTab(secondTab);
            return true;
        };

        context.ViewModel.OpenWorkspaceConfigurationCommand.Execute(null);

        Assert.Same(firstTab.Document, context.WorkspaceConfigurationDialogService.LastDocument);
        Assert.Same(secondTab.Document, context.ViewModel.CurrentDocument);
    }

    [Fact]
    public void OpenPreferencesCommand_Should_Open_Preferences_Dialog()
    {
        TestContext context = CreateContext();

        context.ViewModel.OpenPreferencesCommand.Execute(null);

        Assert.Equal(1, context.PreferencesDialogService.ShowDialogCalls);
    }

    [Fact]
    public void OpenPreferencesCommand_Should_Show_Status_When_Preferences_Are_Saved()
    {
        TestContext context = CreateContext();
        context.PreferencesDialogService.Result = true;

        context.ViewModel.OpenPreferencesCommand.Execute(null);

        Assert.Equal("Preferences saved.", context.ViewModel.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Success, context.ViewModel.OperationStatus.StatusSeverity);
    }

    [Fact]
    public void OpenPreferencesCommand_Should_Not_Change_WorkspaceDirtyState()
    {
        TestContext context = CreateContext();
        context.PreferencesDialogService.Result = true;
        bool wasWorkspaceDirty = context.ViewModel.CurrentDocument.IsWorkspaceDirty;

        context.ViewModel.OpenPreferencesCommand.Execute(null);

        Assert.Equal(wasWorkspaceDirty, context.ViewModel.CurrentDocument.IsWorkspaceDirty);
    }

    [Fact]
    public void OpenPreferencesCommand_Should_Not_Change_PreviewDirtyState()
    {
        TestContext context = CreateContext();
        context.PreferencesDialogService.Result = true;
        bool wasPreviewDirty = context.ViewModel.CurrentDocument.PreviewDirtyTracker.IsPreviewDirty;

        context.ViewModel.OpenPreferencesCommand.Execute(null);

        Assert.Equal(wasPreviewDirty, context.ViewModel.CurrentDocument.PreviewDirtyTracker.IsPreviewDirty);
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

        FakeWorkspaceDocumentPreviewService previewService = new();
        FakeWorkspaceDocumentOutputService outputService = new();
        FakeProfileManagerWindowService profileManagerWindowService = new();
        FakePreferencesDialogService preferencesDialogService = new();
        FakeWorkspaceConfigurationDialogService workspaceConfigurationDialogService = new();
        FakeWorkspaceTabRenameDialogService renameDialogService = new();
        FakeClipboardService clipboardService = new();
        FakeKeyboardShortcutsDialogService keyboardShortcutsDialogService = new();
        FakeUpdateCheckDialogService updateCheckDialogService = new();
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
            updateCheckDialogService,
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
            dirtyStateService,
            promptService,
            clipboardService,
            preferencesDialogService,
            workspaceConfigurationDialogService,
            renameDialogService,
            keyboardShortcutsDialogService,
            updateCheckDialogService,
            applicationPreferencesStore);
    }

    private static WorkspaceProfileDto CreateProfile(bool includeHeaderComment)
    {
        return new WorkspaceProfileDto(
            IncludeHeaderComment: includeHeaderComment,
            IncludeFileSeparators: true,
            IncludeRelativePathInSeparator: true,
            TrimTrailingEmptyLines: true,
            FileTypes: [],
            LineEndingMode: LineEndingMode.Preserve,
            SortMode: SortMode.ByRelativePathAscending,
            InputEncodingMode: InputEncodingMode.Auto,
            PreferredInputEncodingName: null,
            FallbackInputEncodingName: "windows-1251");
    }

    private static void PrepareAppliedPreview(WorkspaceDocumentViewModel document)
    {
        document.SessionSettings.OutputPath = WorkspaceDocumentTestFactory.DefaultOutputPath;
        document.SetLastOutput(CreateOutput());
        ApplyPreviewBaseline(document);
    }

    private static void MakePreviewStale(WorkspaceDocumentViewModel document)
    {
        document.SessionSettings.SessionName += " edited";

        MainStateFactory stateFactory = new();
        document.PreviewDirtyTracker.Refresh(stateFactory.BuildPreviewState(document));
    }

    private static void ApplyPreviewBaseline(WorkspaceDocumentViewModel document)
    {
        MainStateFactory stateFactory = new();
        document.PreviewDirtyTracker.MarkPreviewApplied(stateFactory.BuildPreviewState(document));
    }

    private static MergeOutput CreateOutput(string content = "merged")
    {
        return new MergeOutput(
            content: content,
            sections: [],
            statistics: new MergeStatistics(
                filesScanned: 1,
                filesIncluded: 1,
                filesSkipped: 0,
                totalCharacters: content.Length,
                duration: TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));
    }

    private static void MarkWorkspaceDirty(WorkspaceDocumentViewModel document)
    {
        document.SessionSettings.OutputPath = $@"D:\Output\{Guid.NewGuid():N}.txt";
        WorkspaceDocumentTestFactory.RefreshWorkspaceDirtyState(document);
    }

    private sealed record TestContext(
        MainViewModel ViewModel,
        WorkspaceTabManagerViewModel WorkspaceTabs,
        FakeWorkspaceDocumentPreviewService PreviewService,
        FakeWorkspaceDocumentOutputService OutputService,
        FakeWorkspaceDocumentLifecycleService LifecycleService,
        FakeWorkspaceDocumentDirtyStateService DirtyStateService,
        FakeUserPromptService PromptService,
        FakeClipboardService ClipboardService,
        FakePreferencesDialogService PreferencesDialogService,
        FakeWorkspaceConfigurationDialogService WorkspaceConfigurationDialogService,
        FakeWorkspaceTabRenameDialogService RenameDialogService,
        FakeKeyboardShortcutsDialogService KeyboardShortcutsDialogService,
        FakeUpdateCheckDialogService UpdateCheckDialogService,
        FakeApplicationPreferencesStore ApplicationPreferencesStore);

    private sealed class FakeWorkspaceDocumentLifecycleService : IWorkspaceDocumentLifecycleService
    {
        public WorkspaceDocumentViewModel? LastSaveWorkspaceDocument { get; private set; }

        public WorkspaceDocumentViewModel? LastSaveWorkspaceAsDocument { get; private set; }

        public WorkspaceDocumentViewModel? LastLoadWorkspaceDocument { get; private set; }

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
        public List<WorkspaceDocumentViewModel> PreviewRefreshedDocuments { get; } = [];

        public List<WorkspaceDocumentViewModel> WorkspaceRefreshedDocuments { get; } = [];

        public void RefreshPreviewDirtyState(WorkspaceDocumentViewModel document)
        {
            PreviewRefreshedDocuments.Add(document);
        }

        public void MarkPreviewApplied(WorkspaceDocumentViewModel document)
        {
        }

        public void RefreshWorkspaceDirtyState(WorkspaceDocumentViewModel document)
        {
            WorkspaceRefreshedDocuments.Add(document);
        }

        public void MarkWorkspaceSaved(WorkspaceDocumentViewModel document)
        {
            WorkspaceDocumentTestFactory.MarkWorkspaceSaved(document);
        }
    }
}