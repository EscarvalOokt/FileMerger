namespace FileMerger.Wpf.Features.Workspace;

public interface IWorkspaceCoordinator
{
    Task<bool> SaveWorkspaceAsync(WorkspaceDocumentViewModel document, CancellationToken cancellationToken = default);

    Task<bool> SaveWorkspaceAsAsync(WorkspaceDocumentViewModel document, CancellationToken cancellationToken = default);

    Task<bool> LoadWorkspaceAsync(WorkspaceDocumentViewModel document, CancellationToken cancellationToken = default);

    Task<bool> LoadWorkspaceFromPathAsync(
        WorkspaceDocumentViewModel document,
        string workspaceFilePath,
        CancellationToken cancellationToken = default);
}