namespace FileMerger.Wpf.Features.Workspace.Tabs.Dialogs;

public interface IWorkspaceTabRenameDialogService
{
    string? RequestRename(WorkspaceTabViewModel tab);
}