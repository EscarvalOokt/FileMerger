using FileMerger.Wpf.Features.Workspace.State;
using FileMerger.Wpf.Shell.Main;

namespace FileMerger.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentDirtyStateService : IWorkspaceDocumentDirtyStateService
{
    private readonly IMainStateFactory _mainStateFactory;

    public WorkspaceDocumentDirtyStateService(IMainStateFactory mainStateFactory)
    {
        ArgumentNullException.ThrowIfNull(mainStateFactory);
        _mainStateFactory = mainStateFactory;
    }

    public void RefreshPreviewDirtyState(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.PreviewDirtyTracker.Refresh(_mainStateFactory.BuildPreviewState(document));
    }

    public void MarkPreviewApplied(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.PreviewDirtyTracker.MarkPreviewApplied(_mainStateFactory.BuildPreviewState(document));
    }

    public void RefreshWorkspaceDirtyState(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.WorkspaceDirtyTracker.Refresh(WorkspaceDocumentStateSnapshotFactory.Capture(document));
    }

    public void MarkWorkspaceSaved(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.WorkspaceDirtyTracker.MarkWorkspaceSaved(WorkspaceDocumentStateSnapshotFactory.Capture(document));
    }
}