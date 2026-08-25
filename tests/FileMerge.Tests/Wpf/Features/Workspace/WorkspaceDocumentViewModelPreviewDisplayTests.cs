using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentViewModelPreviewDisplayTests
{
    [Fact]
    public void Document_Should_Start_With_Line_Wrap_Disabled()
    {
        WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDefaultDocument();

        Assert.False(document.IsPreviewLineWrapEnabled);
    }

    [Fact]
    public void IsPreviewLineWrapEnabled_Should_Raise_PropertyChanged()
    {
        WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDefaultDocument();
        List<string?> changedProperties = [];

        document.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        document.IsPreviewLineWrapEnabled = true;

        Assert.True(document.IsPreviewLineWrapEnabled);
        Assert.Contains(nameof(WorkspaceDocumentViewModel.IsPreviewLineWrapEnabled), changedProperties);
    }

    [Fact]
    public void ResetRuntimeState_Should_Not_Reset_Line_Wrap()
    {
        WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDefaultDocument();

        document.IsPreviewLineWrapEnabled = true;
        document.ResetRuntimeState();

        Assert.True(document.IsPreviewLineWrapEnabled);
    }
}