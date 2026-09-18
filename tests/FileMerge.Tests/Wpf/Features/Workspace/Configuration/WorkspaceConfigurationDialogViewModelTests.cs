using System.IO;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Files.ViewModels;
using FileMerger.Wpf.Features.Preview;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.Configuration.Dialogs;
using FileMerger.Wpf.Features.Workspace.Configuration.ViewModels;
using FileMerger.Wpf.Features.Workspace.State;
using FileMerger.Wpf.Shell.Main;

namespace FileMerger.Tests.Wpf.Features.Workspace.Configuration;

public sealed class WorkspaceConfigurationDialogViewModelTests
{
    [Fact]
    public void Constructor_Should_Create_Detached_Working_Copy()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        IWorkspaceDocumentFactory factory = WorkspaceDocumentTestFactory.CreateFactory();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target, factory);

        Assert.NotSame(target, viewModel.WorkingDocument);
        Assert.NotSame(target.SessionSettings, viewModel.WorkingDocument.SessionSettings);
        Assert.NotSame(target.SourcesPane, viewModel.WorkingDocument.SourcesPane);
        Assert.NotSame(target.ProfileEditor, viewModel.WorkingDocument.ProfileEditor);

        Assert.Equal(target.SessionSettings.SessionName, viewModel.WorkingDocument.SessionSettings.SessionName);
        Assert.Equal(target.SessionSettings.OutputPath, viewModel.WorkingDocument.SessionSettings.OutputPath);
        Assert.Equal(target.CurrentProfileName, viewModel.WorkingDocument.CurrentProfileName);
        Assert.Equal(target.CurrentProfileEntryId, viewModel.WorkingDocument.CurrentProfileEntryId);
        Assert.Equal(target.ProfileOriginEntryId, viewModel.WorkingDocument.ProfileOriginEntryId);
        Assert.Equal(target.ProfileOriginDisplayName, viewModel.WorkingDocument.ProfileOriginDisplayName);
        Assert.Equal(
            target.ProfileEditor.IncludeHeaderComment,
            viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment);

        Assert.Equal(target.SourcesPane.Sources.Count, viewModel.WorkingDocument.SourcesPane.Sources.Count);
        Assert.NotSame(target.SourcesPane.Sources[0], viewModel.WorkingDocument.SourcesPane.Sources[0]);
    }

    [Fact]
    public void BrowseOutputCommand_Should_Use_Working_Document()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        FakeWorkspaceDocumentOutputService outputService = new();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target, outputService: outputService);

        viewModel.BrowseOutputCommand.Execute(null);

        Assert.Same(viewModel.WorkingDocument, outputService.LastBrowseOutputDocument);
        Assert.NotSame(target, outputService.LastBrowseOutputDocument);
    }

    [Fact]
    public void Editing_Working_Copy_Should_Not_Mutate_Target_Before_Ok()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target);

        viewModel.WorkingDocument.SessionSettings.SessionName = "Staged Session";
        viewModel.WorkingDocument.SessionSettings.OutputPath = @"D:\Output\staged.txt";
        viewModel.WorkingDocument.ProfileEditor.WorkingProfileName = "Staged Profile";
        viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment = false;
        viewModel.WorkingDocument.SourcesPane.LoadSources(
        [
            new MergeSource(@"D:\Staged", MergeSourceType.Directory, isRecursive: false, isEnabled: true)
        ]);

        Assert.Equal("Live Session", target.SessionSettings.SessionName);
        Assert.Equal(@"D:\Output\live.txt", target.SessionSettings.OutputPath);
        Assert.Equal("Live Profile", target.CurrentProfileName);
        Assert.True(target.ProfileEditor.IncludeHeaderComment);
        Assert.Equal(@"D:\Live", Assert.Single(target.SourcesPane.Sources).Path);
    }

    [Fact]
    public void Editing_Working_Copy_Should_Not_Mutate_Target_Dirty_State()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        WorkspaceDocumentDirtyStateService dirtyStateService = CreateDirtyStateService();
        dirtyStateService.MarkWorkspaceSaved(target);
        dirtyStateService.MarkPreviewApplied(target);

        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target, dirtyStateService: dirtyStateService);

        viewModel.WorkingDocument.SessionSettings.SessionName = "Staged Session";
        viewModel.WorkingDocument.SourcesPane.Sources[0].IsEnabled = false;
        viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment = false;

        Assert.False(target.IsWorkspaceDirty);
        Assert.False(target.PreviewDirtyTracker.IsPreviewDirty);
    }

    [Fact]
    public void Cancel_Should_Discard_Staged_Changes_And_Preserve_Dirty_State()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget(filterRulePattern: "bin");
        WorkspaceDocumentDirtyStateService dirtyStateService = CreateDirtyStateService();
        dirtyStateService.MarkWorkspaceSaved(target);
        dirtyStateService.MarkPreviewApplied(target);

        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target, dirtyStateService: dirtyStateService);
        bool? closeResult = null;
        viewModel.RequestClose += (_, result) => closeResult = result;

        viewModel.WorkingDocument.SessionSettings.SessionName = "Cancelled Session";
        viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment = false;
        viewModel.WorkingDocument.SourcesPane.Sources[0].IsEnabled = false;
        viewModel.WorkingDocument.ProfileEditor.FilterRules[0].Pattern = string.Empty;

        Assert.False(viewModel.OkCommand.CanExecute(null));
        Assert.True(viewModel.CancelCommand.CanExecute(null));

        viewModel.CancelCommand.Execute(null);

        Assert.Equal(false, closeResult);
        Assert.Equal("bin", Assert.Single(target.ProfileEditor.FilterRules).Pattern);
        Assert.Equal("Live Session", target.SessionSettings.SessionName);
        Assert.Equal("live-profile", target.CurrentProfileEntryId);
        Assert.Equal("live-profile", target.ProfileOriginEntryId);
        Assert.Equal("Live Profile", target.ProfileOriginDisplayName);
        Assert.True(target.ProfileEditor.IncludeHeaderComment);
        Assert.True(target.SourcesPane.Sources[0].IsEnabled);
        Assert.False(target.IsWorkspaceDirty);
        Assert.False(target.PreviewDirtyTracker.IsPreviewDirty);
    }

    [Fact]
    public void Ok_Should_Apply_Staged_Configuration_And_Close_With_True()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target);
        bool? closeResult = null;
        viewModel.RequestClose += (_, result) => closeResult = result;

        viewModel.WorkingDocument.SessionSettings.SessionName = "Configured Session";
        viewModel.WorkingDocument.SessionSettings.OutputPath = @"D:\Output\configured.txt";
        viewModel.WorkingDocument.ProfileEditor.WorkingProfileName = "Configured Profile";
        viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment = false;
        viewModel.WorkingDocument.SourcesPane.LoadSources(
        [
            new MergeSource(@"D:\Configured", MergeSourceType.Directory, isRecursive: false, isEnabled: true)
        ]);

        viewModel.OkCommand.Execute(null);

        Assert.Equal(true, closeResult);
        Assert.Equal("Configured Session", target.SessionSettings.SessionName);
        Assert.Equal(@"D:\Output\configured.txt", target.SessionSettings.OutputPath);
        Assert.Equal("Configured Profile", target.CurrentProfileName);
        Assert.Null(target.CurrentProfileEntryId);
        Assert.Equal("live-profile", target.ProfileOriginEntryId);
        Assert.Equal("Live Profile", target.ProfileOriginDisplayName);
        Assert.False(target.ProfileEditor.IncludeHeaderComment);

        MergeSource source = Assert.Single(target.SourcesPane.BuildSources());
        Assert.Equal(@"D:\Configured", source.Path);
        Assert.False(source.IsRecursive);
    }

    [Fact]
    public void Ok_Without_Profile_Changes_Should_Preserve_Linkage_And_Origin()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target);

        viewModel.OkCommand.Execute(null);

        Assert.Equal("live-profile", target.CurrentProfileEntryId);
        Assert.Equal("live-profile", target.ProfileOriginEntryId);
        Assert.Equal("Live Profile", target.ProfileOriginDisplayName);
    }

    [Fact]
    public void Ok_Should_Clear_Linkage_When_Linked_Profile_Settings_Diverge()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target);

        viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment = false;

        viewModel.OkCommand.Execute(null);

        Assert.Null(target.CurrentProfileEntryId);
        Assert.Equal("live-profile", target.ProfileOriginEntryId);
        Assert.Equal("Live Profile", target.ProfileOriginDisplayName);
    }

    [Fact]
    public void Ok_Should_Clear_Linkage_When_Linked_Profile_Name_Diverges()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target);

        viewModel.WorkingDocument.ProfileEditor.WorkingProfileName = "Renamed Profile";

        viewModel.OkCommand.Execute(null);

        Assert.Null(target.CurrentProfileEntryId);
        Assert.Equal("live-profile", target.ProfileOriginEntryId);
        Assert.Equal("Live Profile", target.ProfileOriginDisplayName);
    }

    [Fact]
    public void Ok_Should_Preserve_Linkage_When_Profile_Changes_Are_Reverted()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target);

        viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment = false;
        viewModel.WorkingDocument.ProfileEditor.WorkingProfileName = "Renamed Profile";
        viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment = true;
        viewModel.WorkingDocument.ProfileEditor.WorkingProfileName = "Live Profile";

        viewModel.OkCommand.Execute(null);

        Assert.Equal("live-profile", target.CurrentProfileEntryId);
        Assert.Equal("live-profile", target.ProfileOriginEntryId);
        Assert.Equal("Live Profile", target.ProfileOriginDisplayName);
    }

    [Fact]
    public void Ok_Should_Not_Relink_Custom_Profile_When_It_Matches_Origin()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        target.CurrentProfileEntryId = null;
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target);

        viewModel.OkCommand.Execute(null);

        Assert.Null(target.CurrentProfileEntryId);
        Assert.Equal("live-profile", target.ProfileOriginEntryId);
        Assert.Equal("Live Profile", target.ProfileOriginDisplayName);
    }

    [Fact]
    public void Ok_Should_Refresh_Existing_Workspace_And_Preview_Dirty_State()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        WorkspaceDocumentDirtyStateService dirtyStateService = CreateDirtyStateService();
        dirtyStateService.MarkWorkspaceSaved(target);
        dirtyStateService.MarkPreviewApplied(target);

        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target, dirtyStateService: dirtyStateService);
        viewModel.WorkingDocument.SessionSettings.SessionName = "Changed Session";

        viewModel.OkCommand.Execute(null);

        Assert.True(target.IsWorkspaceDirty);
        Assert.True(target.PreviewDirtyTracker.IsPreviewDirty);
        Assert.True(target.PreviewDirtyTracker.PreviewDirtyReason.HasFlag(PreviewDirtyReason.SessionSettingsChanged));
    }

    [Fact]
    public void Ok_Without_Changes_Should_Preserve_Clean_State()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        WorkspaceDocumentDirtyStateService dirtyStateService = CreateDirtyStateService();
        dirtyStateService.MarkWorkspaceSaved(target);
        dirtyStateService.MarkPreviewApplied(target);

        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target, dirtyStateService: dirtyStateService);

        viewModel.OkCommand.Execute(null);

        Assert.False(target.IsWorkspaceDirty);
        Assert.False(target.PreviewDirtyTracker.IsPreviewDirty);
        Assert.Equal(PreviewDirtyReason.None, target.PreviewDirtyTracker.PreviewDirtyReason);
        Assert.Equal("live-profile", target.CurrentProfileEntryId);
    }

    [Fact]
    public void Ok_Should_Not_Apply_NonConfiguration_Working_State()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        target.WorkspaceFilePath = @"D:\Workspaces\live.filemerger.workspace.json";
        target.PreviewContent = "live preview";
        target.SetLastOutput(CreateOutput(@"D:\Output\live.txt"));

        InputFileItemViewModel targetFile = CreateFile("Live.cs");
        target.FilesPane.LoadFiles([targetFile]);
        target.FilesPane.ApplyOverridesDictionary(
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                [targetFile.FullPath] = false
            });

        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target);
        viewModel.WorkingDocument.WorkspaceFilePath = @"D:\Workspaces\working.filemerger.workspace.json";
        viewModel.WorkingDocument.PreviewContent = "working preview";
        viewModel.WorkingDocument.SetLastOutput(CreateOutput(@"D:\Output\working.txt"));

        InputFileItemViewModel workingFile = CreateFile("Working.cs");
        viewModel.WorkingDocument.FilesPane.LoadFiles([workingFile]);
        viewModel.WorkingDocument.FilesPane.ApplyOverridesDictionary(
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                [workingFile.FullPath] = false
            });

        viewModel.WorkingDocument.SessionSettings.SessionName = "Applied Configuration";
        viewModel.OkCommand.Execute(null);

        Assert.Equal("Applied Configuration", target.SessionSettings.SessionName);
        Assert.Equal(@"D:\Workspaces\live.filemerger.workspace.json", target.WorkspaceFilePath);
        Assert.Equal("live preview", target.PreviewContent);
        Assert.Equal(@"D:\Output\live.txt", target.LastOutput?.OutputTarget?.Path);
        Assert.Same(targetFile, Assert.Single(target.FilesPane.Files));
        Assert.False(target.FilesPane.CaptureOverridesDictionary()[targetFile.FullPath]);
    }

    [Fact]
    public void Ok_Should_Update_Only_Target_Workspace()
    {
        WorkspaceDocumentViewModel first = CreateConfiguredTarget();
        WorkspaceDocumentViewModel second = WorkspaceDocumentTestFactory.CreateDocument(
            sessionName: "Second Session",
            outputPath: @"D:\Output\second.txt");

        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(first);
        viewModel.WorkingDocument.SessionSettings.SessionName = "First Updated";

        viewModel.OkCommand.Execute(null);

        Assert.Equal("First Updated", first.SessionSettings.SessionName);
        Assert.Equal("Second Session", second.SessionSettings.SessionName);
    }

    [Fact]
    public void OkCommand_Should_Update_Availability_When_Invalid_Rule_Is_Fixed_Or_Removed()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget(filterRulePattern: "bin");
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target);
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.WorkingDocument.ProfileEditor.FilterRules);
        List<bool> canExecuteStates = [];
        viewModel.OkCommand.CanExecuteChanged += (_, _) => canExecuteStates.Add(viewModel.OkCommand.CanExecute(null));

        Assert.True(viewModel.OkCommand.CanExecute(null));

        rule.Pattern = string.Empty;

        Assert.False(viewModel.OkCommand.CanExecute(null));
        Assert.Contains(false, canExecuteStates);

        canExecuteStates.Clear();
        rule.Pattern = "obj";

        Assert.True(viewModel.OkCommand.CanExecute(null));
        Assert.Contains(true, canExecuteStates);

        rule.Pattern = string.Empty;
        canExecuteStates.Clear();
        viewModel.WorkingDocument.ProfileEditor.FilterRules.Remove(rule);

        Assert.True(viewModel.OkCommand.CanExecute(null));
        Assert.Contains(true, canExecuteStates);
    }

    [Fact]
    public void OkCommand_Should_Be_Disabled_For_Initially_Invalid_Profile()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget(filterRulePattern: string.Empty);
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target);

        Assert.False(viewModel.OkCommand.CanExecute(null));
        Assert.True(viewModel.CancelCommand.CanExecute(null));
        Assert.Equal(string.Empty, Assert.Single(viewModel.WorkingDocument.ProfileEditor.FilterRules).Pattern);
        Assert.Equal(
            "Filter Rules, rule 1: Pattern cannot be empty.",
            viewModel.WorkingDocument.ProfileEditor.DraftValidationMessage);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Ok_Should_Reject_Invalid_Draft_Without_Mutating_Target(bool workspaceDirty, bool previewDirty)
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget(filterRulePattern: "bin");
        target.WorkspaceFilePath = @"D:\Workspaces\live.filemerger.workspace.json";
        target.PreviewContent = "live preview";
        MergeOutput output = CreateOutput(@"D:\Output\live.txt");
        target.SetLastOutput(output);

        InputFileItemViewModel targetFile = CreateFile("Live.cs");
        target.FilesPane.LoadFiles([targetFile]);
        target.FilesPane.ApplyOverridesDictionary(
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                [targetFile.FullPath] = false
            });

        WorkspaceDocumentDirtyStateService dirtyStateService = CreateDirtyStateService();
        dirtyStateService.MarkWorkspaceSaved(target);
        dirtyStateService.MarkPreviewApplied(target);

        target.SessionSettings.SessionName = "Updated Live Session";
        dirtyStateService.RefreshWorkspaceDirtyState(target);
        dirtyStateService.RefreshPreviewDirtyState(target);

        if (!workspaceDirty)
            dirtyStateService.MarkWorkspaceSaved(target);

        if (!previewDirty)
            dirtyStateService.MarkPreviewApplied(target);

        WorkspaceDirtyStateTracker unchangedTargetTracker = new();
        unchangedTargetTracker.MarkWorkspaceSaved(WorkspaceDocumentStateSnapshotFactory.Capture(target));
        PreviewDirtyReason previousPreviewDirtyReason = target.PreviewDirtyTracker.PreviewDirtyReason;
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target, dirtyStateService: dirtyStateService);
        int closeRequests = 0;
        viewModel.RequestClose += (_, _) => closeRequests++;

        viewModel.WorkingDocument.SessionSettings.SessionName = "Rejected Session";
        viewModel.WorkingDocument.SessionSettings.OutputPath = @"D:\Output\rejected.txt";
        viewModel.WorkingDocument.ProfileEditor.WorkingProfileName = "Rejected Profile";
        viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment = false;
        viewModel.WorkingDocument.SourcesPane.Sources[0].IsEnabled = false;
        ProfileFilterRuleItemViewModel invalidRule = Assert.Single(viewModel.WorkingDocument.ProfileEditor.FilterRules);
        invalidRule.Pattern = string.Empty;

        Assert.False(viewModel.OkCommand.CanExecute(null));

        viewModel.OkCommand.Execute(null);

        Assert.Equal(0, closeRequests);
        unchangedTargetTracker.Refresh(WorkspaceDocumentStateSnapshotFactory.Capture(target));
        Assert.False(unchangedTargetTracker.IsWorkspaceDirty);
        Assert.Equal("bin", Assert.Single(target.ProfileEditor.FilterRules).Pattern);
        Assert.Equal(workspaceDirty, target.IsWorkspaceDirty);
        Assert.Equal(previewDirty, target.PreviewDirtyTracker.IsPreviewDirty);
        Assert.Equal(previousPreviewDirtyReason, target.PreviewDirtyTracker.PreviewDirtyReason);
        Assert.Equal(@"D:\Workspaces\live.filemerger.workspace.json", target.WorkspaceFilePath);
        Assert.Equal("live preview", target.PreviewContent);
        Assert.Same(output, target.LastOutput);
        Assert.Same(targetFile, Assert.Single(target.FilesPane.Files));
        Assert.False(target.FilesPane.CaptureOverridesDictionary()[targetFile.FullPath]);
        Assert.Same(invalidRule, Assert.Single(viewModel.WorkingDocument.ProfileEditor.FilterRules));
        Assert.Equal(string.Empty, invalidRule.Pattern);
        Assert.True(invalidRule.HasValidationError);
        Assert.Equal("Rejected Session", viewModel.WorkingDocument.SessionSettings.SessionName);
        Assert.Equal("Rejected Profile", viewModel.WorkingDocument.ProfileEditor.WorkingProfileName);

        dirtyStateService.RefreshWorkspaceDirtyState(target);
        dirtyStateService.RefreshPreviewDirtyState(target);

        Assert.Equal(workspaceDirty, target.IsWorkspaceDirty);
        Assert.Equal(previewDirty, target.PreviewDirtyTracker.IsPreviewDirty);
        Assert.Equal(previousPreviewDirtyReason, target.PreviewDirtyTracker.PreviewDirtyReason);
    }

    [Fact]
    public void Ok_Should_Apply_Corrected_Draft_After_Rejected_Attempt()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget(filterRulePattern: "bin");
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target);
        List<bool?> closeResults = [];
        viewModel.RequestClose += (_, result) => closeResults.Add(result);
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.WorkingDocument.ProfileEditor.FilterRules);

        viewModel.WorkingDocument.SessionSettings.SessionName = "Corrected Session";
        rule.Pattern = string.Empty;

        viewModel.OkCommand.Execute(null);

        Assert.Empty(closeResults);
        Assert.Equal("Live Session", target.SessionSettings.SessionName);
        Assert.Equal("bin", Assert.Single(target.ProfileEditor.FilterRules).Pattern);

        rule.Pattern = "obj";

        Assert.True(viewModel.OkCommand.CanExecute(null));

        viewModel.OkCommand.Execute(null);

        Assert.Equal(true, Assert.Single(closeResults));
        Assert.Equal("Corrected Session", target.SessionSettings.SessionName);
        Assert.Equal("obj", Assert.Single(target.ProfileEditor.FilterRules).Pattern);
        Assert.Null(target.CurrentProfileEntryId);
        Assert.Equal("live-profile", target.ProfileOriginEntryId);
        Assert.Equal("Live Profile", target.ProfileOriginDisplayName);
    }

    [Fact]
    public void CurrentProfileCard_Should_Reflect_Initial_Working_Profile()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(target);

        Assert.Equal("Live Profile", viewModel.CurrentProfileCard.ProfileName);
        Assert.Equal("live-profile", viewModel.CurrentProfileCard.ProfileEntryId);
        Assert.Equal("live-profile", viewModel.CurrentProfileCard.ProfileOriginEntryId);
        Assert.Equal("Live Profile", viewModel.CurrentProfileCard.ProfileOriginDisplayName);
        Assert.Equal("From profile library", viewModel.CurrentProfileCard.ProfileSourceText);
    }

    [Fact]
    public void EditLocalProfile_Cancel_Should_Not_Change_Working_Copy_Or_Target()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        FakeWorkspaceLocalProfileDialogService dialogService = new();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            workspaceLocalProfileDialogService: dialogService);

        dialogService.Result = null;

        viewModel.EditLocalProfileCommand.Execute(null);

        Assert.Equal(1, dialogService.ShowDialogCalls);
        Assert.Equal("Live Profile", dialogService.LastProfileName);
        Assert.NotNull(dialogService.LastProfile);
        Assert.True(dialogService.LastProfile.IncludeHeaderComment);
        Assert.Equal("Live Profile", viewModel.WorkingDocument.CurrentProfileName);
        Assert.True(viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment);
        Assert.Equal("live-profile", viewModel.WorkingDocument.CurrentProfileEntryId);
        Assert.Equal("Live Profile", target.CurrentProfileName);
        Assert.True(target.ProfileEditor.IncludeHeaderComment);
        Assert.Equal("live-profile", target.CurrentProfileEntryId);
    }

    [Fact]
    public void EditLocalProfile_Ok_Should_Update_Working_Copy_Only_And_Profile_Card()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        FakeWorkspaceLocalProfileDialogService dialogService = new();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            workspaceLocalProfileDialogService: dialogService);

        WorkspaceProfileDto edited = viewModel.WorkingDocument.ProfileEditor.CaptureProfile() with
        {
            IncludeHeaderComment = false
        };

        dialogService.Result = new WorkspaceLocalProfileEditResult("Edited Local Profile", edited);

        viewModel.EditLocalProfileCommand.Execute(null);

        Assert.Equal("Edited Local Profile", viewModel.WorkingDocument.CurrentProfileName);
        Assert.False(viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment);
        Assert.Null(viewModel.WorkingDocument.CurrentProfileEntryId);
        Assert.Equal("live-profile", viewModel.WorkingDocument.ProfileOriginEntryId);
        Assert.Equal("Live Profile", viewModel.WorkingDocument.ProfileOriginDisplayName);
        Assert.Equal("Edited Local Profile", viewModel.CurrentProfileCard.ProfileName);
        Assert.Equal(
            "Custom workspace profile based on 'Live Profile'",
            viewModel.CurrentProfileCard.ProfileSourceText);

        Assert.Equal("Live Profile", target.CurrentProfileName);
        Assert.True(target.ProfileEditor.IncludeHeaderComment);
        Assert.Equal("live-profile", target.CurrentProfileEntryId);
    }

    [Fact]
    public void EditLocalProfile_Should_Preserve_Linkage_When_Result_Matches_Linked_Profile()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        FakeWorkspaceLocalProfileDialogService dialogService = new();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            workspaceLocalProfileDialogService: dialogService);

        dialogService.Result = new WorkspaceLocalProfileEditResult(
            viewModel.WorkingDocument.CurrentProfileName,
            viewModel.WorkingDocument.ProfileEditor.CaptureProfile());

        viewModel.EditLocalProfileCommand.Execute(null);

        Assert.Equal("live-profile", viewModel.WorkingDocument.CurrentProfileEntryId);
        Assert.Equal("live-profile", viewModel.WorkingDocument.ProfileOriginEntryId);
        Assert.Equal("Live Profile", viewModel.WorkingDocument.ProfileOriginDisplayName);
        Assert.Equal("From profile library", viewModel.CurrentProfileCard.ProfileSourceText);
    }

    [Fact]
    public void EditLocalProfile_Should_Not_Relink_Custom_Profile_When_Result_Matches_Origin()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        target.CurrentProfileEntryId = null;

        FakeWorkspaceLocalProfileDialogService dialogService = new();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            workspaceLocalProfileDialogService: dialogService);

        dialogService.Result = new WorkspaceLocalProfileEditResult(
            "Live Profile",
            viewModel.WorkingDocument.ProfileEditor.CaptureProfile());

        viewModel.EditLocalProfileCommand.Execute(null);

        Assert.Null(viewModel.WorkingDocument.CurrentProfileEntryId);
        Assert.Equal("live-profile", viewModel.WorkingDocument.ProfileOriginEntryId);
        Assert.Equal("Live Profile", viewModel.WorkingDocument.ProfileOriginDisplayName);
        Assert.Equal(
            "Custom workspace profile based on 'Live Profile'",
            viewModel.CurrentProfileCard.ProfileSourceText);
    }

    [Fact]
    public void Cancel_After_Local_Profile_Edit_Should_Leave_Target_Untouched()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        FakeWorkspaceLocalProfileDialogService dialogService = new();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            workspaceLocalProfileDialogService: dialogService);

        dialogService.Result = new WorkspaceLocalProfileEditResult(
            "Edited Local Profile",
            viewModel.WorkingDocument.ProfileEditor.CaptureProfile() with { IncludeHeaderComment = false });

        viewModel.EditLocalProfileCommand.Execute(null);
        viewModel.CancelCommand.Execute(null);

        Assert.Equal("Live Profile", target.CurrentProfileName);
        Assert.True(target.ProfileEditor.IncludeHeaderComment);
        Assert.Equal("live-profile", target.CurrentProfileEntryId);
        Assert.Equal("live-profile", target.ProfileOriginEntryId);
        Assert.Equal("Live Profile", target.ProfileOriginDisplayName);
    }

    [Fact]
    public void EditLocalProfile_Should_Allow_Correcting_Initially_Invalid_Profile()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget(filterRulePattern: string.Empty);
        FakeWorkspaceLocalProfileDialogService dialogService = new();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            workspaceLocalProfileDialogService: dialogService);

        Assert.False(viewModel.OkCommand.CanExecute(null));

        WorkspaceProfileDto corrected = viewModel.WorkingDocument.ProfileEditor.CaptureProfile() with
        {
            FilterRules =
            [
                new WorkspaceFileFilterRuleDto(
                    Mode: FilterMode.Exclude,
                    Target: FilterTarget.DirectorySegment,
                    PatternType: RulePatternType.Exact,
                    Pattern: "obj",
                    IsEnabled: true,
                    Description: "Exclude build output",
                    IsUserEditable: true)
            ]
        };
        dialogService.Result = new WorkspaceLocalProfileEditResult("Corrected Profile", corrected);

        viewModel.EditLocalProfileCommand.Execute(null);

        Assert.True(viewModel.OkCommand.CanExecute(null));
        Assert.Equal("obj", Assert.Single(viewModel.WorkingDocument.ProfileEditor.FilterRules).Pattern);
    }

    [Fact]
    public void ChooseProfile_Should_Use_Staged_Host_And_Not_Mutate_Target()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        FakeProfileManagerWindowService profileManagerWindowService = new();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            profileManagerWindowService: profileManagerWindowService);

        WorkspaceProfileDto selectedProfile = viewModel.WorkingDocument.ProfileEditor.CaptureProfile() with
        {
            IncludeHeaderComment = false
        };

        profileManagerWindowService.OnShowDialogWithHostAsync = host =>
        {
            host.ApplyProfileToCurrentSession("Library Profile B", selectedProfile, "profile-b");
            return Task.CompletedTask;
        };

        viewModel.ChooseProfileCommand.Execute(null);

        Assert.Equal(1, profileManagerWindowService.ShowDialogWithHostCalls);
        Assert.Same(viewModel, profileManagerWindowService.LastProfileHost);
        Assert.Equal(ProfileManagerContext.WorkspaceConfiguration, profileManagerWindowService.LastContext);
        Assert.Equal("Library Profile B", viewModel.WorkingDocument.CurrentProfileName);
        Assert.Equal("profile-b", viewModel.WorkingDocument.CurrentProfileEntryId);
        Assert.Equal("profile-b", viewModel.WorkingDocument.ProfileOriginEntryId);
        Assert.Equal("Library Profile B", viewModel.WorkingDocument.ProfileOriginDisplayName);
        Assert.False(viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment);
        Assert.Equal("Library Profile B", viewModel.CurrentProfileCard.ProfileName);
        Assert.Equal("From profile library", viewModel.CurrentProfileCard.ProfileSourceText);

        Assert.Equal("Live Profile", target.CurrentProfileName);
        Assert.Equal("live-profile", target.CurrentProfileEntryId);
        Assert.True(target.ProfileEditor.IncludeHeaderComment);
    }

    [Fact]
    public void ChooseProfile_Should_Clear_Linkage_And_Origin_For_Unsaved_Draft()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        FakeProfileManagerWindowService profileManagerWindowService = new();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            profileManagerWindowService: profileManagerWindowService);

        WorkspaceProfileDto customProfile = viewModel.WorkingDocument.ProfileEditor.CaptureProfile() with
        {
            IncludeHeaderComment = false
        };

        profileManagerWindowService.OnShowDialogWithHostAsync = host =>
        {
            host.ApplyProfileToCurrentSession("Unsaved Draft", customProfile, profileEntryId: null);
            return Task.CompletedTask;
        };

        viewModel.ChooseProfileCommand.Execute(null);

        Assert.Null(viewModel.WorkingDocument.CurrentProfileEntryId);
        Assert.Null(viewModel.WorkingDocument.ProfileOriginEntryId);
        Assert.Null(viewModel.WorkingDocument.ProfileOriginDisplayName);
        Assert.Equal("Custom workspace profile", viewModel.CurrentProfileCard.ProfileSourceText);
        Assert.Equal("live-profile", target.CurrentProfileEntryId);
    }

    [Fact]
    public void Cancel_After_ChooseProfile_Should_Leave_Target_Untouched()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        FakeProfileManagerWindowService profileManagerWindowService = new();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            profileManagerWindowService: profileManagerWindowService);

        WorkspaceProfileDto selectedProfile = viewModel.WorkingDocument.ProfileEditor.CaptureProfile() with
        {
            IncludeHeaderComment = false
        };

        profileManagerWindowService.OnShowDialogWithHostAsync = host =>
        {
            host.ApplyProfileToCurrentSession("Library Profile B", selectedProfile, "profile-b");
            return Task.CompletedTask;
        };

        viewModel.ChooseProfileCommand.Execute(null);
        viewModel.CancelCommand.Execute(null);

        Assert.Equal("Live Profile", target.CurrentProfileName);
        Assert.Equal("live-profile", target.CurrentProfileEntryId);
        Assert.Equal("live-profile", target.ProfileOriginEntryId);
        Assert.Equal("Live Profile", target.ProfileOriginDisplayName);
        Assert.True(target.ProfileEditor.IncludeHeaderComment);
    }

    [Fact]
    public void ChooseProfile_Then_EditLocalProfile_Should_Detach_Selected_Link_And_Preserve_Selected_Origin()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        FakeProfileManagerWindowService profileManagerWindowService = new();
        FakeWorkspaceLocalProfileDialogService localProfileDialogService = new();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            workspaceLocalProfileDialogService: localProfileDialogService,
            profileManagerWindowService: profileManagerWindowService);

        WorkspaceProfileDto selectedProfile = viewModel.WorkingDocument.ProfileEditor.CaptureProfile() with
        {
            IncludeHeaderComment = false
        };

        profileManagerWindowService.OnShowDialogWithHostAsync = host =>
        {
            host.ApplyProfileToCurrentSession("Library Profile B", selectedProfile, "profile-b");
            return Task.CompletedTask;
        };

        viewModel.ChooseProfileCommand.Execute(null);

        localProfileDialogService.Result = new WorkspaceLocalProfileEditResult(
            "Library Profile B",
            selectedProfile with { TrimTrailingEmptyLines = !selectedProfile.TrimTrailingEmptyLines });

        viewModel.EditLocalProfileCommand.Execute(null);

        Assert.Null(viewModel.WorkingDocument.CurrentProfileEntryId);
        Assert.Equal("profile-b", viewModel.WorkingDocument.ProfileOriginEntryId);
        Assert.Equal("Library Profile B", viewModel.WorkingDocument.ProfileOriginDisplayName);
        Assert.Equal(
            "Custom workspace profile based on 'Library Profile B'",
            viewModel.CurrentProfileCard.ProfileSourceText);
    }

    [Fact]
    public void Ok_After_ChooseProfile_Should_Apply_Selected_Profile_Linkage_And_Origin()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        FakeProfileManagerWindowService profileManagerWindowService = new();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            profileManagerWindowService: profileManagerWindowService);

        WorkspaceProfileDto selectedProfile = viewModel.WorkingDocument.ProfileEditor.CaptureProfile() with
        {
            IncludeHeaderComment = false
        };

        profileManagerWindowService.OnShowDialogWithHostAsync = host =>
        {
            host.ApplyProfileToCurrentSession("Library Profile B", selectedProfile, "profile-b");
            return Task.CompletedTask;
        };

        viewModel.ChooseProfileCommand.Execute(null);
        viewModel.OkCommand.Execute(null);

        Assert.Equal("Library Profile B", target.CurrentProfileName);
        Assert.Equal("profile-b", target.CurrentProfileEntryId);
        Assert.Equal("profile-b", target.ProfileOriginEntryId);
        Assert.Equal("Library Profile B", target.ProfileOriginDisplayName);
        Assert.False(target.ProfileEditor.IncludeHeaderComment);
    }

    private static WorkspaceConfigurationDialogViewModel CreateViewModel(
        WorkspaceDocumentViewModel target,
        IWorkspaceDocumentFactory? factory = null,
        IWorkspaceDocumentDirtyStateService? dirtyStateService = null,
        IWorkspaceDocumentOutputService? outputService = null,
        IWorkspaceLocalProfileDialogService? workspaceLocalProfileDialogService = null,
        IProfileManagerWindowService? profileManagerWindowService = null)
    {
        return new WorkspaceConfigurationDialogViewModel(
            target,
            factory ?? WorkspaceDocumentTestFactory.CreateFactory(),
            dirtyStateService ?? CreateDirtyStateService(),
            outputService ?? new FakeWorkspaceDocumentOutputService(),
            workspaceLocalProfileDialogService ?? new FakeWorkspaceLocalProfileDialogService(),
            profileManagerWindowService ?? new FakeProfileManagerWindowService());
    }

    private static WorkspaceDocumentViewModel CreateConfiguredTarget(string? filterRulePattern = null)
    {
        WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDocument(
            sessionName: "Live Session",
            outputPath: @"D:\Output\live.txt");

        document.SourcesPane.LoadSources(
        [
            new MergeSource(@"D:\Live", MergeSourceType.Directory, isRecursive: true, isEnabled: true)
        ]);

        document.ProfileEditor.WorkingProfileName = "Live Profile";
        document.ProfileEditor.IncludeHeaderComment = true;
        document.CurrentProfileEntryId = "live-profile";
        document.ProfileOriginEntryId = "live-profile";
        document.ProfileOriginDisplayName = "Live Profile";

        if (filterRulePattern is not null)
        {
            document.ProfileEditor.FilterRules.Clear();
            document.ProfileEditor.FilterRules.Add(
                new ProfileFilterRuleItemViewModel(
                    new WorkspaceFileFilterRuleDto(
                        Mode: FilterMode.Exclude,
                        Target: FilterTarget.DirectorySegment,
                        PatternType: RulePatternType.Exact,
                        Pattern: filterRulePattern,
                        IsEnabled: true,
                        Description: "Exclude build output",
                        IsUserEditable: true)));
        }

        return document;
    }

    private static WorkspaceDocumentDirtyStateService CreateDirtyStateService()
    {
        return new WorkspaceDocumentDirtyStateService(new MainStateFactory());
    }

    private static InputFileItemViewModel CreateFile(string relativePath)
    {
        var model = new InputFile(
            fullPath: $@"D:\Project\{relativePath}",
            relativePath: relativePath,
            extension: Path.GetExtension(relativePath),
            kind: FileKind.CSharp,
            isIncluded: true);

        return new InputFileItemViewModel(model, automaticIncluded: true, currentIncluded: true, appliedIncluded: true);
    }

    private static MergeOutput CreateOutput(string outputPath)
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
}