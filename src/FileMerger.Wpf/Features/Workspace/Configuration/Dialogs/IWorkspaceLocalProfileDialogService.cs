namespace FileMerger.Wpf.Features.Workspace.Configuration.Dialogs;

public interface IWorkspaceLocalProfileDialogService
{
    Task<WorkspaceLocalProfileEditResult?> ShowDialogAsync(string profileName, WorkspaceProfileDto profile);
}