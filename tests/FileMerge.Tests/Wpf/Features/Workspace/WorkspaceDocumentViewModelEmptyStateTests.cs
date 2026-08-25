using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.State;

namespace FileMerger.Tests.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentViewModelEmptyStateTests
{
    [Fact]
    public void IsEmptyWorkspace_Should_Follow_Output_And_RuntimeReset()
    {
        WorkspaceDocumentViewModel document = CreateDocument();

        Assert.True(document.IsEmptyWorkspace);

        document.SetLastOutput(CreateEmptyOutput());

        Assert.False(document.IsEmptyWorkspace);

        document.ResetRuntimeState();

        Assert.True(document.IsEmptyWorkspace);
    }

    [Fact]
    public void IsEmptyWorkspace_Should_Follow_DiscoveredFiles()
    {
        WorkspaceDocumentViewModel document = CreateDocument();
        var file = new InputFile(
            fullPath: @"D:\Project\File.cs",
            relativePath: "File.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        document.FilesPane.ApplyFiles([file], [file]);

        Assert.False(document.IsEmptyWorkspace);

        document.FilesPane.LoadFiles([]);

        Assert.True(document.IsEmptyWorkspace);
    }

    [Fact]
    public void DiscoveredFilesEmptyState_WithoutSources_Should_ExplainHowToAddSources()
    {
        WorkspaceDocumentViewModel document = CreateDocument();

        Assert.True(document.ShowDiscoveredFilesEmptyState);
        Assert.Equal("No files discovered yet", document.DiscoveredFilesEmptyTitle);
        Assert.Contains("Add a folder or individual file source", document.DiscoveredFilesEmptyDescription);
    }

    [Fact]
    public void DiscoveredFilesEmptyState_WithSourcesBeforeFirstBuild_Should_PromptBuild()
    {
        WorkspaceDocumentViewModel document = CreateDocumentWithSource();

        Assert.Equal("No files discovered yet", document.DiscoveredFilesEmptyTitle);
        Assert.Contains("Build preview", document.DiscoveredFilesEmptyDescription);
    }

    [Fact]
    public void DiscoveredFilesEmptyState_WithValidationErrors_Should_ExplainBlockedDiscovery()
    {
        WorkspaceDocumentViewModel document = CreateDocument();
        AddValidationError(document);

        Assert.Equal("Preview could not discover files", document.DiscoveredFilesEmptyTitle);
        Assert.Contains("Fix validation errors", document.DiscoveredFilesEmptyDescription);
    }

    [Fact]
    public void DiscoveredFilesEmptyState_AfterAppliedPreview_Should_ExplainNoFilesWereFound()
    {
        WorkspaceDocumentViewModel document = CreateDocumentWithSource();
        document.PreviewDirtyTracker.MarkPreviewApplied(
            WorkspaceDocumentStateSnapshotFactory.Capture(document).PreviewState);

        Assert.Equal("No files were discovered", document.DiscoveredFilesEmptyTitle);
        Assert.Contains("Check source paths", document.DiscoveredFilesEmptyDescription);
    }

    [Fact]
    public void PreviewEmptyState_WithoutSources_Should_ExplainHowToAddSources()
    {
        WorkspaceDocumentViewModel document = CreateDocument();

        Assert.True(document.ShowPreviewEmptyState);
        Assert.Equal("Preview will appear here", document.PreviewEmptyTitle);
        Assert.Contains("Add a folder or file source", document.PreviewEmptyDescription);
    }

    [Fact]
    public void PreviewEmptyState_WithValidationErrorsAndNoOutput_Should_ExplainBlockedBuild()
    {
        WorkspaceDocumentViewModel document = CreateDocumentWithSource();
        AddValidationError(document);

        Assert.Equal("Preview build is blocked by validation errors", document.PreviewEmptyTitle);
        Assert.Contains("Fix validation errors", document.PreviewEmptyDescription);
    }

    [Fact]
    public void PreviewEmptyState_BeforeFirstBuild_Should_PromptBuild()
    {
        WorkspaceDocumentViewModel document = CreateDocumentWithSource();

        Assert.Equal("Preview will appear here after build", document.PreviewEmptyTitle);
        Assert.Contains("Build preview", document.PreviewEmptyDescription);
    }

    [Fact]
    public void PreviewEmptyState_WithSuccessfulEmptyOutput_Should_ExplainEmptyResult()
    {
        WorkspaceDocumentViewModel document = CreateDocumentWithSource();
        document.SetLastOutput(CreateEmptyOutput());

        Assert.Equal("Preview is empty", document.PreviewEmptyTitle);
        Assert.Contains("no file content was included", document.PreviewEmptyDescription);
    }

    [Fact]
    public void PreviewEmptyState_WithContent_Should_BeHidden()
    {
        WorkspaceDocumentViewModel document = CreateDocument();
        document.PreviewContent = "merged";

        Assert.False(document.ShowPreviewEmptyState);
    }

    private static WorkspaceDocumentViewModel CreateDocument()
    {
        return WorkspaceDocumentTestFactory.CreateDefaultDocument();
    }

    private static WorkspaceDocumentViewModel CreateDocumentWithSource()
    {
        WorkspaceDocumentViewModel document = CreateDocument();
        document.SourcesPane.LoadSources(
        [
            new MergeSource(
                @"D:\Project",
                MergeSourceType.Directory,
                isRecursive: true,
                isEnabled: true)
        ]);

        return document;
    }

    private static void AddValidationError(WorkspaceDocumentViewModel document)
    {
        document.ValidationPane.Load(
        [
            new ValidationIssue(
                ValidationSeverity.Error,
                "test.error",
                "Test validation error.")
        ]);
    }

    private static MergeOutput CreateEmptyOutput()
    {
        return new MergeOutput(
            content: string.Empty,
            sections: [],
            statistics: new MergeStatistics(
                filesScanned: 0,
                filesIncluded: 0,
                filesSkipped: 0,
                totalCharacters: 0,
                duration: TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));
    }
}