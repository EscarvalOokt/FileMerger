using FileMerger.Wpf.Features.Workspace.Recent;
using FileMerger.Wpf.Shared.Status;

namespace FileMerger.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentLifecycleService : IWorkspaceDocumentLifecycleService
{
    private readonly IRecentWorkspacesService _recentWorkspacesService;
    private readonly IWorkspaceCoordinator _workspaceCoordinator;
    private readonly IWorkspaceDocumentDirtyStateService _workspaceDocumentDirtyStateService;

    public WorkspaceDocumentLifecycleService(
        IWorkspaceCoordinator workspaceCoordinator,
        IWorkspaceDocumentDirtyStateService workspaceDocumentDirtyStateService,
        IRecentWorkspacesService recentWorkspacesService)
    {
        ArgumentNullException.ThrowIfNull(workspaceCoordinator);
        ArgumentNullException.ThrowIfNull(workspaceDocumentDirtyStateService);
        ArgumentNullException.ThrowIfNull(recentWorkspacesService);

        _workspaceCoordinator = workspaceCoordinator;
        _workspaceDocumentDirtyStateService = workspaceDocumentDirtyStateService;
        _recentWorkspacesService = recentWorkspacesService;
    }

    public Task<bool> SaveWorkspaceAsync(
        WorkspaceDocumentViewModel document,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        return SaveWorkspaceCoreAsync(
            document,
            token => _workspaceCoordinator.SaveWorkspaceAsync(document, token),
            stateChanged,
            cancellationToken);
    }

    public Task<bool> SaveWorkspaceAsAsync(
        WorkspaceDocumentViewModel document,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        return SaveWorkspaceCoreAsync(
            document,
            token => _workspaceCoordinator.SaveWorkspaceAsAsync(document, token),
            stateChanged,
            cancellationToken);
    }

    public Task<bool> LoadWorkspaceAsync(
        WorkspaceDocumentViewModel document,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        return LoadWorkspaceCoreAsync(
            document,
            token => _workspaceCoordinator.LoadWorkspaceAsync(document, token),
            stateChanged,
            cancellationToken);
    }

    public Task<bool> LoadWorkspaceFromPathAsync(
        WorkspaceDocumentViewModel document,
        string workspaceFilePath,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrWhiteSpace(workspaceFilePath))
            throw new ArgumentException("Workspace file path cannot be empty.", nameof(workspaceFilePath));

        return LoadWorkspaceCoreAsync(
            document,
            token => _workspaceCoordinator.LoadWorkspaceFromPathAsync(document, workspaceFilePath, token),
            stateChanged,
            cancellationToken);
    }

    private async Task<bool> LoadWorkspaceCoreAsync(
        WorkspaceDocumentViewModel document,
        Func<CancellationToken, Task<bool>> loadAsync,
        Action? stateChanged,
        CancellationToken cancellationToken)
    {
        try
        {
            document.OperationStatus.IsBusy = true;
            document.OperationStatus.ShowIndeterminateProgress("Loading workspace.");
            stateChanged?.Invoke();

            bool loaded = await loadAsync(cancellationToken);

            if (loaded)
            {
                document.ResetRuntimeState();

                _workspaceDocumentDirtyStateService.MarkWorkspaceSaved(document);
                document.OperationStatus.SetStatus("Workspace loaded.", StatusSeverity.Success);

                await TryAddRecentWorkspaceAsync(document);
            }
            else
            {
                document.OperationStatus.SetStatus("Workspace load canceled.", StatusSeverity.Warning);
            }

            return loaded;
        }
        catch (OperationCanceledException)
        {
            document.OperationStatus.SetStatus("Workspace load canceled.", StatusSeverity.Warning);
            return false;
        }
        catch (Exception ex)
        {
            document.OperationStatus.SetStatus($"Failed to load workspace: {ex.Message}", StatusSeverity.Error);
            return false;
        }
        finally
        {
            document.OperationStatus.HideProgress();
            document.OperationStatus.IsBusy = false;
            stateChanged?.Invoke();
        }
    }

    private async Task<bool> SaveWorkspaceCoreAsync(
        WorkspaceDocumentViewModel document,
        Func<CancellationToken, Task<bool>> saveAsync,
        Action? stateChanged,
        CancellationToken cancellationToken)
    {
        try
        {
            document.OperationStatus.IsBusy = true;
            document.OperationStatus.ShowIndeterminateProgress("Saving workspace.");
            stateChanged?.Invoke();

            bool saved = await saveAsync(cancellationToken);

            if (saved)
            {
                _workspaceDocumentDirtyStateService.MarkWorkspaceSaved(document);
                document.OperationStatus.SetStatus("Workspace saved.", StatusSeverity.Success);

                await TryAddRecentWorkspaceAsync(document);
            }
            else
            {
                document.OperationStatus.SetStatus("Workspace save canceled.", StatusSeverity.Warning);
            }

            return saved;
        }
        catch (OperationCanceledException)
        {
            document.OperationStatus.SetStatus("Workspace save canceled.", StatusSeverity.Warning);
            return false;
        }
        catch (Exception ex)
        {
            document.OperationStatus.SetStatus($"Failed to save workspace: {ex.Message}", StatusSeverity.Error);
            return false;
        }
        finally
        {
            document.OperationStatus.HideProgress();
            document.OperationStatus.IsBusy = false;
            stateChanged?.Invoke();
        }
    }

    private async Task TryAddRecentWorkspaceAsync(WorkspaceDocumentViewModel document)
    {
        if (string.IsNullOrWhiteSpace(document.WorkspaceFilePath))
            return;

        try
        {
            await _recentWorkspacesService.AddOrUpdateAsync(document.WorkspaceFilePath);
        }
        catch
        {
            // Recent workspaces are secondary workflow state.
            // A failed recent-list update must not turn a successful open/save into a failed operation.
        }
    }
}