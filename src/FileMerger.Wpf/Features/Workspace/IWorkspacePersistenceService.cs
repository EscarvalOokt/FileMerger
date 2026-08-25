namespace FileMerger.Wpf.Features.Workspace;

public interface IWorkspacePersistenceService
{
    Task SaveWorkspaceAsync(
        WorkspaceDto workspace,
        string filePath,
        CancellationToken cancellationToken = default);

    Task<WorkspaceDto> LoadWorkspaceAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}