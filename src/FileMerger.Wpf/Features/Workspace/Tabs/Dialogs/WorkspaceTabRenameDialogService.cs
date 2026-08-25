using System.Windows;
using FileMerger.Wpf.Shell.Windows;

namespace FileMerger.Wpf.Features.Workspace.Tabs.Dialogs;

public sealed class WorkspaceTabRenameDialogService : IWorkspaceTabRenameDialogService
{
    private readonly IWindowOwnerResolver _ownerResolver;

    public WorkspaceTabRenameDialogService(IWindowOwnerResolver ownerResolver)
    {
        ArgumentNullException.ThrowIfNull(ownerResolver);
        _ownerResolver = ownerResolver;
    }

    public string? RequestRename(WorkspaceTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);

        WorkspaceTabRenameViewModel viewModel = new(tab.Title);
        WorkspaceTabRenameWindow window = new()
        {
            DataContext = viewModel
        };

        Window? owner = _ownerResolver.ResolveOwner(window);
        if (owner is not null)
            window.Owner = owner;

        bool? result = window.ShowDialog();
        if (result != true)
            return null;

        return viewModel.NormalizedName;
    }
}