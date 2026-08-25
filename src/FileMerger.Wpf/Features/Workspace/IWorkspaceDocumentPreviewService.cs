namespace FileMerger.Wpf.Features.Workspace;

public interface IWorkspaceDocumentPreviewService
{
    Task BuildPreviewAsync(
        WorkspaceDocumentViewModel document,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default);
}