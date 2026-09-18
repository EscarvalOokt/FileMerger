using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.Configuration.Dialogs;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeWorkspaceLocalProfileDialogService : IWorkspaceLocalProfileDialogService
{
    public int ShowDialogCalls { get; private set; }

    public string? LastProfileName { get; private set; }

    public WorkspaceProfileDto? LastProfile { get; private set; }

    public WorkspaceLocalProfileEditResult? Result { get; set; }

    public Task<WorkspaceLocalProfileEditResult?> ShowDialogAsync(string profileName, WorkspaceProfileDto profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        ShowDialogCalls++;
        LastProfileName = profileName;
        LastProfile = profile;

        return Task.FromResult(Result);
    }
}