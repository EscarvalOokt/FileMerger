namespace FileMerger.Wpf.Features.Workspace;

public interface IWorkspaceDocumentCloneService
{
    WorkspaceDocumentViewModel CloneAsDuplicate(WorkspaceDocumentViewModel sourceDocument, string duplicateSessionName);
}