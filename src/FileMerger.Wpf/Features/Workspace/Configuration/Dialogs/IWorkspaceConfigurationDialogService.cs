namespace FileMerger.Wpf.Features.Workspace.Configuration.Dialogs;

public interface IWorkspaceConfigurationDialogService
{
    Task<bool> ShowDialogAsync(WorkspaceDocumentViewModel document);
}