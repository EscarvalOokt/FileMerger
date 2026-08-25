using FileMerger.Wpf.Features.Workspace.Tabs;
using FileMerger.Wpf.Features.Workspace.Tabs.Dialogs;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeWorkspaceTabRenameDialogService : IWorkspaceTabRenameDialogService
{
    public WorkspaceTabViewModel? LastRequestedTab { get; private set; }

    public string? NextName { get; set; }

    public int RequestRenameCalls { get; private set; }

    public string? RequestRename(WorkspaceTabViewModel tab)
    {
        RequestRenameCalls++;
        LastRequestedTab = tab;

        return NextName;
    }
}