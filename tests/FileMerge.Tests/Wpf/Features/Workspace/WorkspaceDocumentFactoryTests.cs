using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Settings;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentFactoryTests
{
    [Fact]
    public void CreateDefaultDocument_Should_Create_Document_With_Default_State()
    {
        WorkspaceDocumentFactory factory = CreateFactory();

        WorkspaceDocumentViewModel document = factory.CreateDefaultDocument();

        Assert.Equal("Default Session", document.SessionSettings.SessionName);
        Assert.Equal(string.Empty, document.SessionSettings.OutputPath);

        Assert.Equal("Default", document.ProfileEditor.WorkingProfileName);

        Assert.Empty(document.SourcesPane.Sources);
        Assert.Empty(document.FilesPane.Files);
        Assert.False(document.ValidationPane.HasIssues);

        Assert.True(document.PreviewDirtyTracker.IsPreviewDirty);
        Assert.Null(document.LastOutput);

        Assert.False(document.WorkspaceDirtyTracker.IsWorkspaceDirty);
        Assert.True(document.WorkspaceDirtyTracker.HasSavedState);
        Assert.False(document.IsWorkspaceDirty);

        Assert.Equal(string.Empty, document.PreviewContent);
        Assert.Equal(string.Empty, document.PreviewNotice);
        Assert.False(document.HasPreviewNotice);
        Assert.Equal("Characters: 0", document.PreviewCharacterCountText);

        Assert.Equal("Ready.", document.OperationStatus.StatusMessage);
        Assert.False(document.OperationStatus.IsBusy);
    }

    [Fact]
    public void CreateDefaultDocument_Should_Create_Independent_ViewModel_Instances()
    {
        WorkspaceDocumentFactory factory = CreateFactory();

        WorkspaceDocumentViewModel first = factory.CreateDefaultDocument();
        WorkspaceDocumentViewModel second = factory.CreateDefaultDocument();

        Assert.NotSame(first, second);
        Assert.NotSame(first.SessionSettings, second.SessionSettings);
        Assert.NotSame(first.ProfileEditor, second.ProfileEditor);
        Assert.NotSame(first.SourcesPane, second.SourcesPane);
        Assert.NotSame(first.FilesPane, second.FilesPane);
        Assert.NotSame(first.ValidationPane, second.ValidationPane);
        Assert.NotSame(first.PreviewDirtyTracker, second.PreviewDirtyTracker);
        Assert.NotSame(first.WorkspaceDirtyTracker, second.WorkspaceDirtyTracker);
        Assert.NotSame(first.AppliedPreviewFileStateStore, second.AppliedPreviewFileStateStore);
        Assert.NotSame(first.OperationStatus, second.OperationStatus);
    }

    [Fact]
    public void CreateDefaultDocument_Should_Apply_LineWrap_Default_From_ApplicationPreferences()
    {
        FakeApplicationPreferencesStore preferencesStore = new();
        preferencesStore.SetCurrent(new ApplicationPreferences(
            isPreviewLineWrapEnabledByDefault: true,
            previewDisplayCharacterLimit: 20_000,
            crashLogRetentionLimit: 50));

        WorkspaceDocumentFactory factory =
            WorkspaceDocumentTestFactory.CreateFactory(preferencesStore);

        WorkspaceDocumentViewModel document = factory.CreateDefaultDocument();

        Assert.True(document.IsPreviewLineWrapEnabled);
        Assert.False(document.IsWorkspaceDirty);
    }

    [Fact]
    public void ResetRuntimeState_Should_Clear_Runtime_State_Without_Replacing_Configuration_ViewModels()
    {
        WorkspaceDocumentFactory factory = CreateFactory();
        WorkspaceDocumentViewModel document = factory.CreateDefaultDocument();

        object sessionSettings = document.SessionSettings;
        object profileEditor = document.ProfileEditor;
        object sourcesPane = document.SourcesPane;
        object filesPane = document.FilesPane;

        document.SetLastOutput(CreateOutput());
        document.PreviewContent = "preview";
        document.PreviewNotice = "notice";
        document.SetPreviewCharacterCount(123);

        document.ValidationPane.Load(
        [
            new ValidationIssue(
                ValidationSeverity.Warning,
                "test.warning",
                "Test warning.")
        ]);

        document.AppliedPreviewFileStateStore.Set(
        [
            new InputFile(
                fullPath: @"D:\Project\Test.cs",
                relativePath: "Test.cs",
                extension: ".cs",
                kind: FileKind.CSharp)
        ]);

        document.ResetRuntimeState();

        Assert.Same(sessionSettings, document.SessionSettings);
        Assert.Same(profileEditor, document.ProfileEditor);
        Assert.Same(sourcesPane, document.SourcesPane);
        Assert.Same(filesPane, document.FilesPane);

        Assert.Null(document.LastOutput);
        Assert.Equal(string.Empty, document.PreviewContent);
        Assert.Equal(string.Empty, document.PreviewNotice);
        Assert.False(document.HasPreviewNotice);
        Assert.Equal("Characters: 0", document.PreviewCharacterCountText);

        Assert.False(document.ValidationPane.HasIssues);
        Assert.False(document.AppliedPreviewFileStateStore.HasAppliedState);
        Assert.True(document.PreviewDirtyTracker.IsPreviewDirty);
    }

    private static WorkspaceDocumentFactory CreateFactory()
    {
        return WorkspaceDocumentTestFactory.CreateFactory();
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
}