using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeWorkspaceDocumentPreviewService : IWorkspaceDocumentPreviewService
{
    public WorkspaceDocumentViewModel? LastBuildPreviewDocument { get; private set; }

    public int BuildPreviewCalls { get; private set; }

    public Func<WorkspaceDocumentViewModel, Action?, CancellationToken, Task>? OnBuildPreviewAsync { get; set; }

    public Task BuildPreviewAsync(
        WorkspaceDocumentViewModel document,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default)
    {
        LastBuildPreviewDocument = document;
        BuildPreviewCalls++;

        return OnBuildPreviewAsync?.Invoke(document, stateChanged, cancellationToken) ?? Task.CompletedTask;
    }
}