using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Preview;
using FileMerger.Wpf.Features.Preview.ViewModels;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentViewModelPreviewSummaryTests
{
    [Fact]
    public void Document_Should_Start_With_Empty_Preview_Summary()
    {
        WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDefaultDocument();

        Assert.False(document.HasPreviewSummary);
        Assert.False(document.PreviewSummary.HasSummary);
        Assert.True(document.IsPreviewSummaryExpanded);
        Assert.False(document.IsPreviewSummaryCollapsed);
    }

    [Fact]
    public void SetPreviewSummary_Should_Update_Summary_And_HasPreviewSummary()
    {
        WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDefaultDocument();
        List<string?> changedProperties = [];

        document.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        PreviewGenerationSummaryViewModel summary = CreateSummary();

        document.SetPreviewSummary(summary);

        Assert.Same(summary, document.PreviewSummary);
        Assert.True(document.HasPreviewSummary);
        Assert.Contains(nameof(WorkspaceDocumentViewModel.PreviewSummary), changedProperties);
        Assert.Contains(nameof(WorkspaceDocumentViewModel.HasPreviewSummary), changedProperties);
    }

    [Fact]
    public void ResetPreviewSummary_Should_Clear_Summary()
    {
        WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDefaultDocument();

        document.SetPreviewSummary(CreateSummary());
        document.ResetPreviewSummary();

        Assert.False(document.HasPreviewSummary);
        Assert.False(document.PreviewSummary.HasSummary);
    }

    [Fact]
    public void ResetRuntimeState_Should_Clear_Preview_Summary()
    {
        WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDefaultDocument();

        document.SetPreviewSummary(CreateSummary());
        document.ResetRuntimeState();

        Assert.False(document.HasPreviewSummary);
        Assert.False(document.PreviewSummary.HasSummary);
    }

    [Fact]
    public void TogglePreviewSummary_Should_Toggle_Expanded_State()
    {
        WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDefaultDocument();

        Assert.True(document.IsPreviewSummaryExpanded);
        Assert.False(document.IsPreviewSummaryCollapsed);

        document.TogglePreviewSummary();

        Assert.False(document.IsPreviewSummaryExpanded);
        Assert.True(document.IsPreviewSummaryCollapsed);

        document.TogglePreviewSummary();

        Assert.True(document.IsPreviewSummaryExpanded);
        Assert.False(document.IsPreviewSummaryCollapsed);
    }

    private static PreviewGenerationSummaryViewModel CreateSummary()
    {
        MergeOutput output = new(
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

        PreviewTextFormatResult previewText = new(
            Text: "merged",
            TotalCharacters: 6,
            DisplayedCharacters: 6,
            WasTruncated: false,
            OmittedCharacters: 0);

        return PreviewGenerationSummaryViewModel.From(output, [], [], [], previewText);
    }
}