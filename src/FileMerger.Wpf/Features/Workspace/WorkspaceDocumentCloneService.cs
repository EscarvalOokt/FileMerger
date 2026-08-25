namespace FileMerger.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentCloneService : IWorkspaceDocumentCloneService
{
    private readonly IWorkspaceDocumentFactory _workspaceDocumentFactory;
    private readonly IWorkspaceDocumentDirtyStateService _workspaceDocumentDirtyStateService;

    public WorkspaceDocumentCloneService(
        IWorkspaceDocumentFactory workspaceDocumentFactory,
        IWorkspaceDocumentDirtyStateService workspaceDocumentDirtyStateService)
    {
        ArgumentNullException.ThrowIfNull(workspaceDocumentFactory);
        ArgumentNullException.ThrowIfNull(workspaceDocumentDirtyStateService);

        _workspaceDocumentFactory = workspaceDocumentFactory;
        _workspaceDocumentDirtyStateService = workspaceDocumentDirtyStateService;
    }

    public WorkspaceDocumentViewModel CloneAsDuplicate(
        WorkspaceDocumentViewModel sourceDocument,
        string duplicateSessionName)
    {
        ArgumentNullException.ThrowIfNull(sourceDocument);

        if (string.IsNullOrWhiteSpace(duplicateSessionName))
            throw new ArgumentException("Duplicate session name cannot be empty.", nameof(duplicateSessionName));

        WorkspaceDto dto = WorkspaceDocumentMapper.Capture(sourceDocument);
        WorkspaceDocumentViewModel duplicate = _workspaceDocumentFactory.CreateDefaultDocument();

        WorkspaceDocumentMapper.Apply(duplicate, dto);

        duplicate.ResetRuntimeState();
        duplicate.CurrentProfileEntryId = null;
        duplicate.WorkspaceFilePath = null;
        duplicate.SessionSettings.SessionName = duplicateSessionName.Trim();

        duplicate.WorkspaceDirtyTracker.Reset();
        _workspaceDocumentDirtyStateService.RefreshWorkspaceDirtyState(duplicate);

        return duplicate;
    }
}