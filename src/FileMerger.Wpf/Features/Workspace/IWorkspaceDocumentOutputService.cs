namespace FileMerger.Wpf.Features.Workspace;

public interface IWorkspaceDocumentOutputService
{
    Task SaveOutputAsync(
        WorkspaceDocumentViewModel document,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default);

    void BrowseOutputPath(WorkspaceDocumentViewModel document);

    bool CanOpenOutputFolder(WorkspaceDocumentViewModel document);

    void OpenOutputFolder(WorkspaceDocumentViewModel document);
}