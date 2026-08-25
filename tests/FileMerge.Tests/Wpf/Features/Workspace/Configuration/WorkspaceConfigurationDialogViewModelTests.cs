using System.IO;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Files.ViewModels;
using FileMerger.Wpf.Features.Preview;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.Configuration.ViewModels;
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
        Assert.Equal(target.ProfileEditor.IncludeHeaderComment, viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment);

        Assert.Equal(target.SourcesPane.Sources.Count, viewModel.WorkingDocument.SourcesPane.Sources.Count);
        Assert.NotSame(target.SourcesPane.Sources[0], viewModel.WorkingDocument.SourcesPane.Sources[0]);
    }

    [Fact]
    public void BrowseOutputCommand_Should_Use_Working_Document()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        FakeWorkspaceDocumentOutputService outputService = new();
        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            outputService: outputService);

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
            new MergeSource(
                @"D:\Staged",
                MergeSourceType.Directory,
                isRecursive: false,
                isEnabled: true)
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

        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            dirtyStateService: dirtyStateService);

        viewModel.WorkingDocument.SessionSettings.SessionName = "Staged Session";
        viewModel.WorkingDocument.SourcesPane.Sources[0].IsEnabled = false;
        viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment = false;

        Assert.False(target.IsWorkspaceDirty);
        Assert.False(target.PreviewDirtyTracker.IsPreviewDirty);
    }

    [Fact]
    public void Cancel_Should_Discard_Staged_Changes_And_Preserve_Dirty_State()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        WorkspaceDocumentDirtyStateService dirtyStateService = CreateDirtyStateService();
        dirtyStateService.MarkWorkspaceSaved(target);
        dirtyStateService.MarkPreviewApplied(target);

        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            dirtyStateService: dirtyStateService);
        bool? closeResult = null;
        viewModel.RequestClose += (_, result) => closeResult = result;

        viewModel.WorkingDocument.SessionSettings.SessionName = "Cancelled Session";
        viewModel.WorkingDocument.ProfileEditor.IncludeHeaderComment = false;
        viewModel.WorkingDocument.SourcesPane.Sources[0].IsEnabled = false;

        viewModel.CancelCommand.Execute(null);

        Assert.Equal(false, closeResult);
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
            new MergeSource(
                @"D:\Configured",
                MergeSourceType.Directory,
                isRecursive: false,
                isEnabled: true)
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

        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            dirtyStateService: dirtyStateService);
        viewModel.WorkingDocument.SessionSettings.SessionName = "Changed Session";

        viewModel.OkCommand.Execute(null);

        Assert.True(target.IsWorkspaceDirty);
        Assert.True(target.PreviewDirtyTracker.IsPreviewDirty);
        Assert.True(target.PreviewDirtyTracker.PreviewDirtyReason.HasFlag(
            PreviewDirtyReason.SessionSettingsChanged));
    }

    [Fact]
    public void Ok_Without_Changes_Should_Preserve_Clean_State()
    {
        WorkspaceDocumentViewModel target = CreateConfiguredTarget();
        WorkspaceDocumentDirtyStateService dirtyStateService = CreateDirtyStateService();
        dirtyStateService.MarkWorkspaceSaved(target);
        dirtyStateService.MarkPreviewApplied(target);

        WorkspaceConfigurationDialogViewModel viewModel = CreateViewModel(
            target,
            dirtyStateService: dirtyStateService);

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

    private static WorkspaceConfigurationDialogViewModel CreateViewModel(
        WorkspaceDocumentViewModel target,
        IWorkspaceDocumentFactory? factory = null,
        IWorkspaceDocumentDirtyStateService? dirtyStateService = null,
        IWorkspaceDocumentOutputService? outputService = null)
    {
        return new WorkspaceConfigurationDialogViewModel(
            target,
            factory ?? WorkspaceDocumentTestFactory.CreateFactory(),
            dirtyStateService ?? CreateDirtyStateService(),
            outputService ?? new FakeWorkspaceDocumentOutputService());
    }

    private static WorkspaceDocumentViewModel CreateConfiguredTarget()
    {
        WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDocument(
            sessionName: "Live Session",
            outputPath: @"D:\Output\live.txt");

        document.SourcesPane.LoadSources(
        [
            new MergeSource(
                @"D:\Live",
                MergeSourceType.Directory,
                isRecursive: true,
                isEnabled: true)
        ]);

        document.ProfileEditor.WorkingProfileName = "Live Profile";
        document.ProfileEditor.IncludeHeaderComment = true;
        document.CurrentProfileEntryId = "live-profile";
        document.ProfileOriginEntryId = "live-profile";
        document.ProfileOriginDisplayName = "Live Profile";

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

        return new InputFileItemViewModel(
            model,
            automaticIncluded: true,
            currentIncluded: true,
            appliedIncluded: true);
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