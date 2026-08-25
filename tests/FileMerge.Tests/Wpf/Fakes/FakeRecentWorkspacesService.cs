using FileMerger.Wpf.Features.Workspace.Recent;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeRecentWorkspacesService : IRecentWorkspacesService
{
    public List<RecentWorkspaceEntry> Entries { get; } = [];

    public List<string> AddedPaths { get; } = [];

    public List<string> RemovedPaths { get; } = [];

    public int GetCalls { get; private set; }

    public int ClearCalls { get; private set; }

    public Task<IReadOnlyCollection<RecentWorkspaceEntry>> GetRecentWorkspacesAsync(
        CancellationToken cancellationToken = default)
    {
        GetCalls++;

        IReadOnlyCollection<RecentWorkspaceEntry> result = [.. Entries];
        return Task.FromResult(result);
    }

    public Task AddOrUpdateAsync(
        string workspaceFilePath,
        CancellationToken cancellationToken = default)
    {
        AddedPaths.Add(workspaceFilePath);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(
        string workspaceFilePath,
        CancellationToken cancellationToken = default)
    {
        RemovedPaths.Add(workspaceFilePath);

        Entries.RemoveAll(x =>
            string.Equals(x.FilePath, workspaceFilePath, StringComparison.OrdinalIgnoreCase));

        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        ClearCalls++;
        Entries.Clear();

        return Task.CompletedTask;
    }
}