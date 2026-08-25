namespace FileMerger.Wpf.Features.Workspace;

public interface IWorkspaceDocumentLifecycleService
{
    Task<bool> SaveWorkspaceAsync(
        WorkspaceDocumentViewModel document,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default);

    Task<bool> SaveWorkspaceAsAsync(
        WorkspaceDocumentViewModel document,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default);

    Task<bool> LoadWorkspaceAsync(
        WorkspaceDocumentViewModel document,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default);

    Task<bool> LoadWorkspaceFromPathAsync(
        WorkspaceDocumentViewModel document,
        string workspaceFilePath,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default);
}