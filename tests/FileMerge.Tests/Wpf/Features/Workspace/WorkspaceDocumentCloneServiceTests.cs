using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentCloneServiceTests
{
    [Fact]
    public void CloneAsDuplicate_Should_Create_Independent_Document_Copy()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        WorkspaceDocumentCloneService service = new(factory, dirtyStateService);

        WorkspaceDocumentViewModel source = CreateDocument("Source Workspace");
        source.SessionSettings.OutputPath = @"D:\Output\source.txt";
        source.WorkspaceFilePath = @"D:\Workspaces\source.filemerger.workspace.json";

        WorkspaceDocumentViewModel duplicate = service.CloneAsDuplicate(source, "Source Workspace Copy");

        Assert.NotSame(source, duplicate);
        Assert.Equal("Source Workspace Copy", duplicate.SessionSettings.SessionName);
        Assert.Equal(@"D:\Output\source.txt", duplicate.SessionSettings.OutputPath);
        Assert.Null(duplicate.WorkspaceFilePath);

        duplicate.SessionSettings.OutputPath = @"D:\Output\duplicate.txt";

        Assert.Equal(@"D:\Output\source.txt", source.SessionSettings.OutputPath);
        Assert.Equal(@"D:\Output\duplicate.txt", duplicate.SessionSettings.OutputPath);
    }

    [Fact]
    public void CloneAsDuplicate_Should_Not_Copy_Runtime_State()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        WorkspaceDocumentCloneService service = new(factory, dirtyStateService);

        WorkspaceDocumentViewModel source = CreateDocument("Source Workspace");
        source.SetLastOutput(CreateOutput());
        source.PreviewContent = "preview";
        source.PreviewNotice = "notice";
        source.ValidationPane.Load(
        [
            new ValidationIssue(ValidationSeverity.Warning, "test.warning", "Test warning.")
        ]);

        WorkspaceDocumentViewModel duplicate = service.CloneAsDuplicate(source, "Source Workspace Copy");

        Assert.Null(duplicate.LastOutput);
        Assert.Equal(string.Empty, duplicate.PreviewContent);
        Assert.Equal(string.Empty, duplicate.PreviewNotice);
        Assert.Empty(duplicate.ValidationPane.Issues);
    }

    [Fact]
    public void CloneAsDuplicate_Should_Mark_Duplicate_As_WorkspaceDirty()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        WorkspaceDocumentCloneService service = new(factory, dirtyStateService);

        WorkspaceDocumentViewModel source = CreateDocument("Source Workspace");

        WorkspaceDocumentViewModel duplicate = service.CloneAsDuplicate(source, "Source Workspace Copy");

        Assert.Same(duplicate, dirtyStateService.LastWorkspaceRefreshedDocument);
        Assert.True(duplicate.IsWorkspaceDirty);
    }

    [Fact]
    public void CloneAsDuplicate_Should_Clear_Active_Profile_Linkage_And_Preserve_Origin()
    {
        FakeWorkspaceDocumentFactory factory = new();
        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        WorkspaceDocumentCloneService service = new(factory, dirtyStateService);

        WorkspaceDocumentViewModel source = CreateDocument("Source Workspace");
        source.ProfileEditor.WorkingProfileName = "Repository Profile";
        source.CurrentProfileEntryId = "profile-repository";
        source.ProfileOriginEntryId = "profile-repository";
        source.ProfileOriginDisplayName = "Repository Profile";

        WorkspaceDocumentViewModel duplicate = service.CloneAsDuplicate(source, "Source Workspace Copy");

        Assert.Null(duplicate.CurrentProfileEntryId);
        Assert.Equal("profile-repository", duplicate.ProfileOriginEntryId);
        Assert.Equal("Repository Profile", duplicate.ProfileOriginDisplayName);
        Assert.Equal("Repository Profile", duplicate.CurrentProfileName);
    }

    [Fact]
    public void CloneAsDuplicate_Should_Throw_When_SourceDocument_Is_Null()
    {
        WorkspaceDocumentCloneService service = new(
            new FakeWorkspaceDocumentFactory(),
            new FakeWorkspaceDocumentDirtyStateService());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            service.CloneAsDuplicate(null!, "Duplicate"));

        Assert.Equal("sourceDocument", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CloneAsDuplicate_Should_Throw_When_DuplicateSessionName_Is_Empty(string duplicateSessionName)
    {
        WorkspaceDocumentCloneService service = new(
            new FakeWorkspaceDocumentFactory(),
            new FakeWorkspaceDocumentDirtyStateService());

        WorkspaceDocumentViewModel source = CreateDocument("Source Workspace");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            service.CloneAsDuplicate(source, duplicateSessionName));

        Assert.Equal("duplicateSessionName", ex.ParamName);
    }

    private static WorkspaceDocumentViewModel CreateDocument(string sessionName)
    {
        return WorkspaceDocumentTestFactory.CreateSavedDocument(sessionName: sessionName, outputPath: string.Empty);
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

    private sealed class FakeWorkspaceDocumentDirtyStateService : IWorkspaceDocumentDirtyStateService
    {
        public WorkspaceDocumentViewModel? LastWorkspaceRefreshedDocument { get; private set; }

        public void RefreshPreviewDirtyState(WorkspaceDocumentViewModel document)
        {
        }

        public void MarkPreviewApplied(WorkspaceDocumentViewModel document)
        {
        }

        public void RefreshWorkspaceDirtyState(WorkspaceDocumentViewModel document)
        {
            LastWorkspaceRefreshedDocument = document;

            WorkspaceDocumentTestFactory.RefreshWorkspaceDirtyState(document);
        }

        public void MarkWorkspaceSaved(WorkspaceDocumentViewModel document)
        {
            WorkspaceDocumentTestFactory.MarkWorkspaceSaved(document);
        }
    }
}