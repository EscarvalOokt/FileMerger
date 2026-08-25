namespace FileMerger.Wpf.Features.Workspace;

public interface IWorkspaceDocumentDirtyStateService
{
    void RefreshPreviewDirtyState(WorkspaceDocumentViewModel document);

    void MarkPreviewApplied(WorkspaceDocumentViewModel document);

    void RefreshWorkspaceDirtyState(WorkspaceDocumentViewModel document);

    void MarkWorkspaceSaved(WorkspaceDocumentViewModel document);
}