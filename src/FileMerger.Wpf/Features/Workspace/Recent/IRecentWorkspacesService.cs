namespace FileMerger.Wpf.Features.Workspace.Recent;

public interface IRecentWorkspacesService
{
    Task<IReadOnlyCollection<RecentWorkspaceEntry>> GetRecentWorkspacesAsync(
        CancellationToken cancellationToken = default);

    Task AddOrUpdateAsync(string workspaceFilePath, CancellationToken cancellationToken = default);

    Task RemoveAsync(string workspaceFilePath, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}